using SRA_Assembler;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace SRA_Simulator
{
    public struct OpcodeFunc
    {
        public uint Opcode;
        public uint Func;

        public OpcodeFunc(uint opcode, uint func)
        {
            Opcode = opcode; Func = func;
        }

        public class EqualityComparer : IEqualityComparer<OpcodeFunc>
        {
            public bool Equals(OpcodeFunc x, OpcodeFunc y)
            {
                return x.Opcode == y.Opcode && x.Func == y.Func;
            }

            public int GetHashCode([DisallowNull] OpcodeFunc obj)
            {
                return obj.Opcode.GetHashCode() * obj.Func.GetHashCode();
            }
        }
    }

    public class CPU
    {
        private Register registers = new Register();
        private Vector256<ulong>[] vRegisters = new Vector256<ulong>[32];
        private ulong entry;

        private VirtualMemory memory;
        private VirtualMemory.Segment heapSegment;
        private VirtualMemory.Segment stackSegment;
        private VirtualMemory.Segment textSegment;

        private Dictionary<long, FileStream> openFiles;
        private Queue<long> availableDescriptor;
        private long nextDescriptor;

        // If true, terminate the process on exit system call.
        // Else, do not terminate the process and set the exit code.
        public bool TerminateOnExit = true;
        private long? exitCode;

        private const ulong STDIN = 0UL;
        private const ulong STDOUT = 1UL;
        private const ulong STDERR = 2UL;

        private Dictionary<ulong, Func<ulong, ulong, ulong, ulong, ulong, ulong, ulong>> syscallTable;

        private Dictionary<uint, InstructionFormat> opcodeFormats;
        private Dictionary<OpcodeFunc, Action<int, int, int>> rFunctions;
        private Dictionary<uint, Action<int, int, ushort>> iFunctions;
        private Dictionary<uint, Action<uint>> jFunctions;
        private Dictionary<OpcodeFunc, Action<int, ushort>> eiFunctions;

        public const ulong WORD_BIT_MASK = 0xFFFF_FFFFU;
        public const ulong HALF_BIT_MASK = 0xFFFFU;
        public const ulong BYTE_BIT_MASK = 0xFFU;
        public const ulong JUMP_BIT_MASK = 0x7FF_FFFFU;
        public const ulong IMM15_SIGN_EXT = 0xFFFF_FFFF_FFFF_8000U;
        public const ulong IMM16_SIGN_EXT = 0xFFFF_FFFF_FFFF_0000U;

        public byte[] TextBinary => ((List<byte>)textSegment.Memory).ToArray();
        public ulong TextSegmentStart => textSegment.MinAddress;
        public ulong Entrypoint => entry;
        public Register Registers => registers;
        public ReadOnlySpan<Vector256<ulong>> VectorRegisters
        {
            get
            {
                return new ReadOnlySpan<Vector256<ulong>>(vRegisters);
            }
        }
        public long? ExitCode => exitCode;

        public CPU(string execFile, ulong stackSize)
        {
            LoadProgram(execFile, stackSize);
            BuildFileSystem();
            BuildSyscallTable();
            LinkInstructions();
        }

        public void LoadProgram(string execFile, ulong stackSize)
        {
            using (FileStream fin = new FileStream(execFile, FileMode.Open))
            {
                using (BinaryReader fread = new BinaryReader(fin))
                {
                    byte[] elfBin = fread.ReadBytes(Marshal.SizeOf<ELFHeader>());
                    ELFHeader header = HeaderUtils.FromBytes<ELFHeader>(elfBin);
                    CheckELFHeader(header);
                    List<ProgramHeader> segments = new List<ProgramHeader>(header.e_phnum + 2);
                    for (int i = 0; i < header.e_phnum; i++)
                    {
                        fin.Position = header.e_ehsize + header.e_phentsize * i;
                        byte[] phBin = fread.ReadBytes(header.e_phentsize);
                        segments.Add(HeaderUtils.FromBytes<ProgramHeader>(phBin));
                    }

                    segments.Sort((s1, s2) =>
                    {
                        if (s1.p_vaddr < s2.p_vaddr)
                        {
                            return -1;
                        }
                        else if (s1.p_vaddr == s2.p_vaddr)
                        {
                            return 0;
                        }
                        else
                        {
                            return 1;
                        }
                    });

                    ulong lastSegmentEnd = segments[segments.Count - 1].p_vaddr;
                    lastSegmentEnd += segments[segments.Count - 1].p_memsz;

                    ProgramHeader heapSegment = new ProgramHeader();
                    heapSegment.p_vaddr = lastSegmentEnd + (ulong)Random.Shared.NextInt64(0, 4096);
                    if (heapSegment.p_vaddr % 32 != 0)
                    {
                        heapSegment.p_vaddr = (heapSegment.p_vaddr / 32 + 1) * 32;
                    }
                    heapSegment.p_filesz = 0;
                    heapSegment.p_memsz = 0; // initial heap size is zero
                    heapSegment.p_flags = ProgramHeader.PF_R | ProgramHeader.PF_W;
                    heapSegment.p_align = 0;

                    ulong stackSegmentEnd = 0x007F_FFFF_FFFFUL;
                    ulong stackSegmentStart = stackSegmentEnd - stackSize + 1;

                    ProgramHeader stackSegment = new ProgramHeader();
                    stackSegment.p_vaddr = stackSegmentStart;
                    stackSegment.p_filesz = 0;
                    stackSegment.p_memsz = stackSize;
                    stackSegment.p_flags = ProgramHeader.PF_R | ProgramHeader.PF_W;
                    stackSegment.p_align = 0;

                    segments.Add(heapSegment);
                    segments.Add(stackSegment);

                    memory = new VirtualMemory(fread, segments.ToArray());
                    for (int i = 0; i < memory.segments.Length; i++)
                    {
                        if (this.heapSegment != null && this.stackSegment != null)
                        {
                            break;
                        }

                        if (memory.segments[i].MinAddress == heapSegment.p_vaddr)
                        {
                            this.heapSegment = memory.segments[i];
                            continue;
                        }

                        if (memory.segments[i].MinAddress == stackSegment.p_vaddr)
                        {
                            this.stackSegment = memory.segments[i];
                            continue;
                        }
                    }

                    if (this.heapSegment == null || this.stackSegment == null)
                    {
                        throw new Exception("No heap or stack allocated.");
                    }

                    this.stackSegment.SetCapacity((int)(uint)stackSize);

                    entry = header.e_entry;

                    Reset();

                    textSegment = memory.GetSegment(registers.PC);
                }
            }
        }

        public void Reset()
        {
            for (int i = 0; i < 32; i++)
            {
                registers[i] = 0;
                vRegisters[i] = Vector256.CreateScalar(0UL);
            }

            registers.Hi = 0;
            registers.Lo = 0;

            registers.PC = entry;
            registers[28] = 0x0080_0000_0000UL; // frame pointer
            registers[29] = 0x007F_FFFF_FFF0UL; // stack pointer
            registers[30] = 0x0010_0000_8000UL; // global pointer

            exitCode = null;

            BuildFileSystem();

            Console.Clear();
        }

        public unsafe void CheckELFHeader(in ELFHeader header)
        {
            if (!header.CheckMagicNumber())
            {
                throw new Exception("The input file is not ELF file.");
            }

            if (header.e_type != ELFHeader.ET_EXEC)
            {
                throw new Exception("The input file is not executable.");
            }

            if (header.e_ident[ELFHeader.EI_CLASS] != ELFHeader.ELFCLASS64)
            {
                throw new Exception("The input file is not 64-bit program.");
            }

            if (header.e_ident[ELFHeader.EI_DATA] != ELFHeader.ELFDATA2LSB)
            {
                throw new Exception("The input file is not in little-endian format.");
            }

            if (header.e_ident[ELFHeader.EI_VERSION] != 1 || header.e_version != 1)
            {
                throw new Exception("The input file has wrong ELF version.");
            }

            if (header.e_ehsize != (ushort)Marshal.SizeOf<ELFHeader>())
            {
                throw new Exception("The input file has wrong ELF header size.");
            }

            if (header.e_phentsize != (ushort)Marshal.SizeOf<ProgramHeader>())
            {
                throw new Exception("The input file has wrong program header size.");
            }
        }

        public void BuildFileSystem()
        {
            if (openFiles != null)
            {
                foreach (var file in openFiles)
                {
                    file.Value.Close();
                }

                openFiles.Clear();
                availableDescriptor.Clear();
                nextDescriptor = 3;

                return;
            }

            openFiles = new Dictionary<long, FileStream>();
            availableDescriptor = new Queue<long>();

            // 0: stdin
            // 1: stdout
            // 2: stderr
            nextDescriptor = 3;
        }

        public void BuildSyscallTable()
        {
            syscallTable = new Dictionary<ulong, Func<ulong, ulong, ulong, ulong, ulong, ulong, ulong>>();

            syscallTable[1] = PrintInteger;
            syscallTable[2] = PrintFloat;
            syscallTable[3] = PrintDouble;
            syscallTable[4] = PrintString;
            syscallTable[5] = ReadInteger;
            syscallTable[6] = ReadFloat;
            syscallTable[7] = ReadDouble;
            syscallTable[8] = ReadString;
            syscallTable[9] = Sbrk;
            syscallTable[10] = Exit;
            syscallTable[11] = PrintCharacter;
            syscallTable[12] = ReadCharacter;
            syscallTable[13] = OpenFile;
            syscallTable[14] = ReadFile;
            syscallTable[15] = WriteFile;
            syscallTable[16] = CloseFile;
            syscallTable[17] = Exit2;
        }

        public void LinkInstructions()
        {
            opcodeFormats = new Dictionary<uint, InstructionFormat>();
            rFunctions = new Dictionary<OpcodeFunc, Action<int, int, int>>(new OpcodeFunc.EqualityComparer());
            iFunctions = new Dictionary<uint, Action<int, int, ushort>>();
            jFunctions = new Dictionary<uint, Action<uint>>();
            eiFunctions = new Dictionary<OpcodeFunc, Action<int, ushort>>(new OpcodeFunc.EqualityComparer());

            foreach (var inst in InstData.instructions)
            {
                Instruction instruction = inst.Value;
                if (instruction.Format == InstructionFormat.Pseudo)
                    continue;

                string name = instruction.Name;
                InstructionFormat format = instruction.Format;
                uint opcode = instruction.Opcode;
                uint func = instruction.Func;

                if (!opcodeFormats.ContainsKey(opcode))
                {
                    opcodeFormats[opcode] = format;
                }

                name = name.Replace('.', '_');
                MethodInfo methodInfo = typeof(CPU).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
                if (methodInfo == null)
                {
                    throw new Exception($"No method for the instruction {name}");
                }

                switch (format)
                {
                    case InstructionFormat.R:
                        {
                            OpcodeFunc opfunc = new OpcodeFunc(opcode, func);
                            if (rFunctions.ContainsKey(opfunc))
                            {
                                throw new Exception("Opcode collision");
                            }

                            Action<int, int, int> fun = (Action<int, int, int>)Delegate.CreateDelegate(
                                typeof(Action<int, int, int>),
                                this,
                                methodInfo);
                            rFunctions[opfunc] = fun;
                        }
                        break;
                    case InstructionFormat.I:
                        {
                            if (iFunctions.ContainsKey(opcode))
                            {
                                throw new Exception("Opcode collision");
                            }

                            Action<int, int, ushort> fun = (Action<int, int, ushort>)Delegate.CreateDelegate(
                                typeof(Action<int, int, ushort>),
                                this,
                                methodInfo);
                            iFunctions[opcode] = fun;
                        }
                        break;
                    case InstructionFormat.J:
                        {
                            if (jFunctions.ContainsKey(opcode))
                            {
                                throw new Exception("Opcode collision");
                            }

                            Action<uint> fun = (Action<uint>)Delegate.CreateDelegate(
                                typeof(Action<uint>),
                                this,
                                methodInfo);
                            jFunctions[opcode] = fun;
                        }
                        break;
                    case InstructionFormat.EI:
                        {
                            OpcodeFunc opfunc = new OpcodeFunc(opcode, func);
                            if (eiFunctions.ContainsKey(opfunc))
                            {
                                throw new Exception("Opcode collision");
                            }

                            Action<int, ushort> fun = (Action<int, ushort>)Delegate.CreateDelegate(
                                typeof(Action<int, ushort>),
                                this,
                                methodInfo);
                            eiFunctions[opfunc] = fun;
                        }
                        break;
                    default:
                        throw new Exception("Unknown instruction format");
                }
            }
        }

        public void Run()
        {
            while (true)
            {
                Clock();
            }
        }

        public void Clock()
        {
            uint inst = memory.GetInstruction(registers.PC);
            registers.PC += 4;

            uint opcode = (inst >> 25) & 0b111_1111U;
            if (!opcodeFormats.ContainsKey(opcode))
            {
                throw new Exception("Unknown instruction");
            }

            InstructionFormat format = opcodeFormats[opcode];
            switch (format)
            {
                case InstructionFormat.R:
                    {
                        int rs = (int)((inst >> 20) & 0b1_1111U);
                        int rt = (int)((inst >> 15) & 0b1_1111U);
                        int rd = (int)((inst >> 10) & 0b1_1111U);
                        uint func = inst & 0b11_1111_1111U;

                        OpcodeFunc opfn = new OpcodeFunc(opcode, func);
                        if (!rFunctions.ContainsKey(opfn))
                        {
                            throw new Exception("Unknown instruction");
                        }

                        rFunctions[opfn](rs, rt, rd);
                    }
                    break;
                case InstructionFormat.I:
                    {
                        int rs = (int)((inst >> 20) & 0b1_1111U);
                        int rt = (int)((inst >> 15) & 0b1_1111U);
                        ushort imm = (ushort)(inst & 0b111_1111_1111_1111U);

                        if (!iFunctions.ContainsKey(opcode))
                        {
                            throw new Exception("Unknown instruction");
                        }

                        iFunctions[opcode](rs, rt, imm);
                    }
                    break;
                case InstructionFormat.J:
                    {
                        uint addr = inst & 0x1FFFFFFU;

                        if (!jFunctions.ContainsKey(opcode))
                        {
                            throw new Exception("Unknown instruction");
                        }

                        jFunctions[opcode](addr);
                    }
                    break;
                case InstructionFormat.EI:
                    {
                        int rd = (int)((inst >> 20) & 0b1_1111U);
                        uint func = (inst >> 16) & 0b1111U;
                        ushort imm = (ushort)(inst & 0xFFFFU);

                        OpcodeFunc opfn = new OpcodeFunc(opcode, func);
                        if (!eiFunctions.ContainsKey(opfn))
                        {
                            throw new Exception("Unknown instruction");
                        }

                        eiFunctions[opfn](rd, imm);
                    }
                    break;
                default:
                    throw new Exception("Unknown instruction");
            }
        }

        public string GetMemoryString(ulong startAddr, ulong row, ulong column)
        {
            return memory.GetMemoryString(startAddr, row, column);
        }

        private void Exit(long exitCode)
        {
            Console.WriteLine($"Program terminated with exit code {exitCode}.");
            if (TerminateOnExit)
            {
                Environment.Exit((int)(uint)(ulong)exitCode);
            }
            else
            {
                this.exitCode = exitCode;
            }
        }

        // syscall 1
        public ulong PrintInteger(ulong value, ulong a1, ulong a2, ulong a3, ulong a4, ulong a5)
        {
            Console.Write(value);
            return 0;
        }

        // syscall 2
        public ulong PrintFloat(ulong value, ulong a1, ulong a2, ulong a3, ulong a4, ulong a5)
        {
            float fvalue = BitConverter.Int32BitsToSingle((int)(uint)value);
            Console.Write(fvalue);
            return 0;
        }

        // syscall 3
        public ulong PrintDouble(ulong value, ulong a1, ulong a2, ulong a3, ulong a4, ulong a5)
        {
            double dvalue = BitConverter.Int64BitsToDouble((long)value);
            Console.Write(dvalue);
            return 0;
        }

        // syscall 4
        public ulong PrintString(ulong straddr, ulong a1, ulong a2, ulong a3, ulong a4, ulong a5)
        {
            while (true)
            {
                byte character = memory[straddr];
                if (character == 0) // null-terminated
                {
                    break;
                }

                Console.Write((char)character);
                straddr++;
            }
            return 0;
        }

        // syscall 5
        public ulong ReadInteger(ulong a0, ulong a1, ulong a2, ulong a3, ulong a4, ulong a5)
        {
            return Convert.ToUInt64(Console.ReadLine(), 10);
        }

        // syscall 6
        public unsafe ulong ReadFloat(ulong a0, ulong a1, ulong a2, ulong a3, ulong a4, ulong a5)
        {
            float result = Convert.ToSingle(Console.ReadLine());
            return *((uint*)&result);
        }

        // syscall 7
        public unsafe ulong ReadDouble(ulong a0, ulong a1, ulong a2, ulong a3, ulong a4, ulong a5)
        {
            double result = Convert.ToDouble(Console.ReadLine());
            return *((ulong*)&result);
        }

        // syscall 8
        public unsafe ulong ReadString(ulong buf, ulong maxLen, ulong a2, ulong a3, ulong a4, ulong a5)
        {
            if (maxLen < 1)
            {
                return 0;
            }

            if (maxLen == 1)
            {
                memory[buf] = 0;
                return 0;
            }

            ulong n = 0;
            while (n < maxLen - 1)
            {
                int ch = Console.Read();
                if (ch == -1)
                {
                    break;
                }

                memory[buf + n] = (byte)ch;
                n++;

                if (ch == '\n')
                {
                    break;
                }
            }

            memory[buf + n] = 0;

            return 0;
        }

        // syscall 9
        public unsafe ulong Sbrk(ulong size, ulong a1, ulong a2, ulong a3, ulong a4, ulong a5)
        {
            if (size == 0)
            {
                return heapSegment.MaxAddress + 1;
            }

            ulong newMax = heapSegment.MaxAddress + size;
            if (newMax >= stackSegment.MinAddress || newMax < heapSegment.MinAddress - 1)
            {
                return unchecked((ulong)-1L);
            }

            ulong oldBreak = heapSegment.MaxAddress + 1;
            heapSegment.MaxAddress = newMax;

            return oldBreak;
        }

        // syscall 10
        public unsafe ulong Exit(ulong a0, ulong a1, ulong a2, ulong a3, ulong a4, ulong a5)
        {
            Exit(0);

            return 0;
        }

        // syscall 11
        public unsafe ulong PrintCharacter(ulong ch, ulong a1, ulong a2, ulong a3, ulong a4, ulong a5)
        {
            Console.Write((char)ch);
            return 0;
        }

        // syscall 12
        public unsafe ulong ReadCharacter(ulong a0, ulong a1, ulong a2, ulong a3, ulong a4, ulong a5)
        {
            return (ulong)(long)Console.Read();
        }

        const ulong O_ACCMODE = 0x3UL;
        const ulong O_RDONLY = 0UL;
        const ulong O_WRONLY = 0x1UL;
        const ulong O_RDWR = 0x2UL;
        const ulong O_CREAT = 0x40UL;
        const ulong O_EXCL = 0x80UL;
        const ulong O_TRUNC = 0x200UL;
        const ulong O_APPEND = 0x400UL;

        // syscall 13
        public unsafe ulong OpenFile(ulong filename, ulong flags, ulong mode, ulong a3, ulong a4, ulong a5)
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                byte ch = memory[filename];
                if (ch == 0)
                    break;

                sb.Append((char)ch);
            }

            string url = sb.ToString();

            FileAccess access;
            ulong accessFlag = flags & O_ACCMODE;
            switch(accessFlag)
            {
                case O_RDONLY:
                    access = FileAccess.Read;
                    break;
                case O_WRONLY:
                    access = FileAccess.Write;
                    break;
                case O_RDWR:
                    access = FileAccess.ReadWrite;
                    break;
                default:
                    return unchecked((ulong)-1L);
            }

            FileMode filemode;
            if ((flags & O_CREAT) != 0)
            {
                if ((flags & O_EXCL) != 0)
                {
                    filemode = FileMode.CreateNew;
                }
                else if ((flags & O_APPEND) != 0)
                {
                    filemode = FileMode.Append;
                }
                else
                {
                    filemode = FileMode.Create;
                }
            }
            else if ((flags & O_TRUNC) != 0)
            {
                filemode = FileMode.Truncate;
            }
            else if ((flags & O_APPEND) != 0)
            {
                filemode = FileMode.Append;
                if (!File.Exists(url))
                {
                    return unchecked((ulong)-1L);
                }
            }
            else
            {
                filemode = FileMode.Open;
            }

            FileStream stream;
            try
            {
                stream = new FileStream(url, filemode, access);
            }
            catch (Exception)
            {
                return unchecked((ulong)-1L);
            }

            if (availableDescriptor.Count > 0)
            {
                long descriptor = availableDescriptor.Dequeue();
                openFiles[descriptor] = stream;
                return (ulong)descriptor;
            }
            else
            {
                long descriptor = nextDescriptor;
                nextDescriptor++;
                openFiles[descriptor] = stream;
                return (ulong)descriptor;
            }
        }

        // syscall 14
        public unsafe ulong ReadFile(ulong descriptor, ulong buf, ulong maxCnt, ulong a3, ulong a4, ulong a5)
        {
            if (descriptor == STDIN)
            {
                ulong cnt = 0;
                while (cnt < maxCnt)
                {
                    int ch = Console.In.Read();
                    if (ch == -1)
                        break;

                    memory[buf + cnt] = (byte)ch;
                    cnt++;
                }

                return cnt;
            }
            
            if (descriptor == STDOUT || descriptor == STDERR)
            {
                return unchecked((ulong)-1L);
            }

            if (!openFiles.ContainsKey((long)descriptor))
            {
                return unchecked((ulong)-1L);
            }

            FileStream stream = openFiles[(long)descriptor];

            try
            {
                ulong cnt = 0;
                while (cnt < maxCnt)
                {
                    int ch = stream.ReadByte();
                    if (ch == -1)
                        break;

                    memory[buf + cnt] = (byte)ch;
                    cnt++;
                }

                return cnt;
            }
            catch (Exception)
            {
                return unchecked((ulong)-1L);
            }
        }

        // syscall 15
        public unsafe ulong WriteFile(ulong descriptor, ulong buf, ulong cnt, ulong a3, ulong a4, ulong a5)
        {
            if (descriptor == STDIN)
            {
                return unchecked((ulong)-1L);
            }

            if (descriptor == STDOUT || descriptor == STDERR)
            {
                TextWriter writer = descriptor == STDOUT ? Console.Out : Console.Error;
                for (ulong i = 0; i < cnt; i++)
                {
                    writer.Write((char)memory[buf + i]);
                }

                return cnt;
            }

            if (!openFiles.ContainsKey((long)descriptor))
            {
                return unchecked((ulong)-1L);
            }

            FileStream stream = openFiles[(long)descriptor];

            try
            {
                for (ulong i = 0; i < cnt; i++)
                {
                    stream.WriteByte(memory[buf + i]);
                }

                return cnt;
            }
            catch (Exception)
            {
                return unchecked((ulong)-1L);
            }
        }

        // syscall 16
        public unsafe ulong CloseFile(ulong descriptor, ulong a1, ulong a2, ulong a3, ulong a4, ulong a5)
        {
            if (descriptor == STDIN || descriptor == STDOUT || descriptor == STDERR)
            {
                return unchecked((ulong)-1L);
            }

            if (!openFiles.ContainsKey((long)descriptor))
            {
                return unchecked((ulong)-1L);
            }

            FileStream stream = openFiles[(long)descriptor];
            stream.Close();

            openFiles.Remove((long)descriptor);
            availableDescriptor.Enqueue((long)descriptor);

            return 0;
        }

        // syscall 17
        public unsafe ulong Exit2(ulong exitCode, ulong a1, ulong a2, ulong a3, ulong a4, ulong a5)
        {
            Exit((long)exitCode);
            return 0;
        }

        public static ulong SignExtendUint(uint value)
        {
            return (ulong)(long)(int)value;
        }

        public static ulong SignExtendUshort(ushort value)
        {
            return (ulong)(long)(short)value;
        }

        public static ulong SignExtendByte(byte value)
        {
            return (ulong)(long)(sbyte)value;
        }

        public static ulong SignExtendImm15(ushort value)
        {
            if ((((uint)value >> 14) & 0x1) == 1) // negative number
            {
                return IMM15_SIGN_EXT | value;
            }
            else // positive number
            {
                return value;
            }
        }

        public static unsafe byte[] GetBytesFromUInt128(UInt128 value)
        {
            byte[] numArray = new byte[16];
            fixed (byte* numPtr = numArray)
                *(UInt128*)numPtr = value;
            return numArray;
        }

        // Adds two 64-bit integers in rs and rt and stores the result in rd. Ignore overflow.
        private void add(int rs, int rt, int rd)
        {
            registers[rd] = registers[rs] + registers[rt];
        }

        // Adds two 32-bit integers in lower 32-bits in rs and rt and stores the sign extended result in rd.
        // Ignore overflow.
        private void addw(int rs, int rt, int rd)
        {
            uint result = (uint)(registers[rs] & WORD_BIT_MASK) + (uint)(registers[rt] & WORD_BIT_MASK);
            registers[rd] = SignExtendUint(result);
        }

        // Adds sign extended 64-bit integer imm and 64-bit integer in rs and stores the result in rt.
        // Ignore the overflow.
        private void addi(int rs, int rt, ushort imm)
        {
            registers[rt] = registers[rs] + SignExtendImm15(imm);
        }

        // Adds zero extended 64-bit integer imm and 64-bit integer in rs and stores the result in rt.
        // Ignore the overflow.
        private void addiu(int rs, int rt, ushort imm)
        {
            registers[rt] = registers[rs] + imm;
        }

        // Adds sign extended 32-bit integer imm and 32-bit integer in lower 32-bits in rs and stores
        // the sign extended result in rt. Ignore the overflow.
        private void addiw(int rs, int rt, ushort imm)
        {
            uint result = (uint)(registers[rs] & WORD_BIT_MASK) + (uint)(SignExtendImm15(imm) & WORD_BIT_MASK);
            registers[rt] = SignExtendUint(result);
        }

        // Subtracts two 64-bit integers in rs and rt and stores the result in rd. Ignore overflow.
        private void sub(int rs, int rt, int rd)
        {
            registers[rd] = registers[rs] - registers[rt];
        }

        // Subtracts two 32-bit integers in lower 32-bits in rs and rt and stores the sign extended
        // result in rd. Ignore overflow.
        private void subw(int rs, int rt, int rd)
        {
            uint result = (uint)(registers[rs] & WORD_BIT_MASK) - (uint)(registers[rt] & WORD_BIT_MASK);
            registers[rd] = SignExtendUint(result);
        }

        // Multiplies two signed 64-bit integers in rs and rt and stores the result 128-bit signed
        // integer in %hi and %lo register. Stores upper 64-bits in %hi and lower 64-bits in %lo.
        private void mul(int rs, int rt, ushort imm)
        {
            long op1 = (long)registers[rs];
            long op2 = (long)registers[rt];

            long hi = Math.BigMul(op1, op2, out long lo);
            registers.Hi = (ulong)hi;
            registers.Lo = (ulong)lo;
        }

        // Multiplies two unsigned 64-bit integers in rs and rt and stores the result 128-bit signed
        // integer in %hi and %lo register. Stores upper 64-bits in %hi and lower 64-bits in %lo.
        private void mulu(int rs, int rt, ushort imm)
        {
            ulong hi = Math.BigMul(registers[rs], registers[rt], out ulong lo);
            registers.Hi = hi;
            registers.Lo = lo;
        }

        // Divides two signed 64-bit integers in rs and rt and stores the 64-bit signed quotients in %hi
        // and 64-bit signed remainder in %lo. If rt is 0, set all bits in %hi to 1. and set %lo to 0.
        private void div(int rs, int rt, ushort imm)
        {
            if (registers[rt] == 0)
            {
                registers.Hi = unchecked((ulong)-1L);
                registers.Lo = 0;
                return;
            }

            long op1 = (long)registers[rs];
            long op2 = (long)registers[rt];

            long quotient = op1 / op2;
            long remainder = op1 % op2;

            registers.Hi = (ulong)quotient;
            registers.Lo = (ulong)remainder;
        }

        // Divides two unsigned 64-bit integers in rs and rt and stores the 64-bit unsigned quotients in
        // %hi and 64-bit unsigned remainder in %lo. If rt is 0, set all bits in %hi to 1. and set %lo to 0.
        private void divu(int rs, int rt, ushort imm)
        {
            if (registers[rt] == 0)
            {
                registers.Hi = unchecked((ulong)-1L);
                registers.Lo = 0;
                return;
            }

            ulong quotient = registers[rs] / registers[rt];
            ulong remainder = registers[rs] % registers[rt];

            registers.Hi = quotient;
            registers.Lo = remainder;
        }

        // Multiplies two 32-bit signed integers in lower 32-bits in rs and rt and stores the signed 64-bit
        // integer result in rd.
        private void mulw(int rs, int rt, int rd)
        {
            int op1 = (int)(uint)(registers[rs] & WORD_BIT_MASK);
            int op2 = (int)(uint)(registers[rt] & WORD_BIT_MASK);
            long result = Math.BigMul(op1, op2);

            registers[rd] = (ulong)result;
        }

        // Multiplies two 32-bit unsigned integers in lower 32-bits in rs and rt and stores the unsigned
        // 64-bit integer result in rd.
        private void muluw(int rs, int rt, int rd)
        {
            uint op1 = (uint)(registers[rs] & WORD_BIT_MASK);
            uint op2 = (uint)(registers[rt] & WORD_BIT_MASK);
            ulong result = (ulong)op1 * op2;

            registers[rd] = result;
        }

        // Calculates logical and of two 64-bit data in rs and rt and stores the result in rd.
        private void and(int rs, int rt, int rd)
        {
            registers[rd] = registers[rs] & registers[rt];
        }

        // Calculates logical or of two 64-bit data in rs and rt and stores the result in rd.
        private void or(int rs, int rt, int rd)
        {
            registers[rd] = registers[rs] | registers[rt];
        }

        // Calculates logical xor of two 64-bit data in rs and rt and stores the result in rd.
        private void xor(int rs, int rt, int rd)
        {
            registers[rd] = registers[rs] ^ registers[rt];
        }

        // Calculates logical and of two 64-bit data in rs and zero extended imm and stores the result in rt.
        private void andi(int rs, int rt, ushort imm)
        {
            registers[rt] = registers[rs] & imm;
        }

        // Calculates logical or of two 64-bit data in rs and zero extended imm and stores the result in rt.
        private void ori(int rs, int rt, ushort imm)
        {
            registers[rt] = registers[rs] | imm;
        }

        // Calculates logical xor of two 64-bit data in rs and zero extended imm and stores the result in rt.
        private void xori(int rs, int rt, ushort imm)
        {
            registers[rt] = registers[rs] ^ imm;
        }

        // Shifts the 64-bit data in rs by amount held in lower 6 bits(unsigned) of rt(range from 0 to 63) to
        // the left and fill the empty space to 0. Stores the result in rd.
        private void sll(int rs, int rt, int rd)
        {
            int shamt = (int)(registers[rt] & 0b11_1111U);
            registers[rd] = registers[rs] << shamt;
        }

        // Shifts the lower 32-bit data in rs by amount held in lower 5 bits(unsigned) of rt(range from 0 to 31)
        // to the left and fill the empty space to 0. Stores the sign extended result in rd.
        private void sllw(int rs, int rt, int rd)
        {
            int shamt = (int)(registers[rt] & 0b1_1111U);
            uint result = ((uint)(registers[rs] & WORD_BIT_MASK)) << shamt;

            registers[rd] = SignExtendUint(result);
        }

        // Shifts the 64-bit data in rs by amount held in lower 6 bits(unsigned) of rt(range from 0 to 63) to
        // the right and fill the empty space to 0. Stores the result in rd.
        private void srl(int rs, int rt, int rd)
        {
            int shamt = (int)(registers[rt] & 0b11_1111U);
            registers[rd] = registers[rs] >> shamt;
        }

        // Shifts the lower 32-bit data in rs by amount held in lower 5 bits(unsigned) of rt(range from 0 to 31)
        // to the right and fill the empty space to 0. Stores the sign extended result in rd.
        private void srlw(int rs, int rt, int rd)
        {
            int shamt = (int)(registers[rt] & 0b1_1111U);
            uint result = ((uint)(registers[rs] & WORD_BIT_MASK)) >> shamt;

            registers[rd] = SignExtendUint(result);
        }

        // Shifts the 64-bit data in rs by amount held in lower 6 bits(unsigned) of rt(range from 0 to 63) to
        // the right and fill the empty space to the most significant bit of rs. Stores the result in rd.
        private void sra(int rs, int rt, int rd)
        {
            int shamt = (int)(registers[rt] & 0b11_1111U);
            registers[rd] = (ulong)((long)registers[rs] >> shamt);
        }

        // Shifts the lower 32-bit data in rs by amount held in lower 5 bits(unsigned) of rt(range from 0 to 31)
        // to the right and fill the empty space to the most significant bit of lower 32-bit data in rs.
        // Stores the sign extended result in rd.
        private void sraw(int rs, int rt, int rd)
        {
            int shamt = (int)(registers[rt] & 0b1_1111U);
            int result = ((int)(uint)(registers[rs] & WORD_BIT_MASK)) >> shamt;

            registers[rd] = SignExtendUint((uint)result);
        }

        // Sets rd to 1 if 64-bit signed integer in rs is less than 64-bit signed integer in rt, or to 0.
        private void slt(int rs, int rt, int rd)
        {
            registers[rd] = (long)registers[rs] < (long)registers[rt] ? 1UL : 0UL;
        }

        // Sets rd to 1 if 64-bit unsigned integer in rs is less than 64-bit unsigned integer in rt, or to 0.
        private void sltu(int rs, int rt, int rd)
        {
            registers[rd] = registers[rs] < registers[rt] ? 1UL : 0UL;
        }

        // Sets rt to 1 if 64-bit signed integer in rs is less than sign extended imm, or to 0.
        private void slti(int rs, int rt, ushort imm)
        {
            registers[rt] = (long)registers[rs] < (long)SignExtendImm15(imm) ? 1UL : 0UL;
        }

        // Sets rt to 1 if 64-bit unsigned integer in rs is less than zero extended imm, or to 0.
        private void sltiu(int rs, int rt, ushort imm)
        {
            registers[rt] = registers[rs] < imm ? 1UL : 0UL;
        }

        // Loads a dword(8 byte data) from memory address rs + rt and stores in rd.
        private void ld(int rs, int rt, int rd)
        {
            registers[rd] = memory.GetDword(registers[rs] + registers[rt]);
        }

        // Loads a dword(8 byte data) from memory address rs + (rt * 8) and stores in rd.
        private void lds(int rs, int rt, int rd)
        {
            registers[rd] = memory.GetDword(registers[rs] + (registers[rt] * 8));
        }

        // Loads a dword(8 byte data) from memory address rs + sign extended imm and stores in rt.
        private void ldi(int rs, int rt, ushort imm)
        {
            registers[rt] = memory.GetDword(registers[rs] + SignExtendImm15(imm));
        }

        // Loads a word(4 byte data) from memory address rs + rt and stores sign extended data in rd.
        private void lw(int rs, int rt, int rd)
        {
            registers[rd] = SignExtendUint(memory.GetWord(registers[rs] + registers[rt]));
        }

        // Loads a word(4 byte data) from memory address rs + rt and stores zero extended data in rd.
        private void lwu(int rs, int rt, int rd)
        {
            registers[rd] = memory.GetWord(registers[rs] + registers[rt]);
        }

        // Loads a word(4 byte data) from memory address rs + (rt * 4) and stores sign extended data in rd.
        private void lws(int rs, int rt, int rd)
        {
            registers[rd] = SignExtendUint(memory.GetWord(registers[rs] + (registers[rt] * 4)));
        }

        // Loads a word(4 byte data) from memory address rs + (rt * 4) and stores zero extended data in rd.
        private void lwsu(int rs, int rt, int rd)
        {
            registers[rd] = memory.GetWord(registers[rs] + (registers[rt] * 4));
        }

        // Loads a word(4 byte data) from memory address rs + sign extended imm and stores sign extended
        // data in rt.
        private void lwi(int rs, int rt, ushort imm)
        {
            registers[rt] = SignExtendUint(memory.GetWord(registers[rs] + SignExtendImm15(imm)));
        }

        // Loads a word(4 byte data) from memory address rs + sign extended imm and stores zero extended
        // data in rt.
        private void lwiu(int rs, int rt, ushort imm)
        {
            registers[rt] = memory.GetWord(registers[rs] + SignExtendImm15(imm));
        }

        // Loads a half(2 byte data) from memory address rs + rt and stores sign extended data in rd.
        private void lh(int rs, int rt, int rd)
        {
            registers[rd] = SignExtendUshort(memory.GetHalf(registers[rs] + registers[rt]));
        }

        // Loads a half(2 byte data) from memory address rs + rt and stores zero extended data in rd.
        private void lhu(int rs, int rt, int rd)
        {
            registers[rd] = memory.GetHalf(registers[rs] + registers[rt]);
        }

        // Loads a half(2 byte data) from memory address rs + (rt * 2) and stores sign extended data in rd.
        private void lhs(int rs, int rt, int rd)
        {
            registers[rd] = SignExtendUshort(memory.GetHalf(registers[rs] + (registers[rt] * 2)));
        }

        // Loads a half(2 byte data) from memory address rs + (rt * 2) and stores zero extended data in rd.
        private void lhsu(int rs, int rt, int rd)
        {
            registers[rd] = memory.GetHalf(registers[rs] + (registers[rt] * 2));
        }

        // Loads a half(2 byte data) from memory address rs + sign extended imm and stores sign extended data
        // in rt.
        private void lhi(int rs, int rt, ushort imm)
        {
            registers[rt] = SignExtendUshort(memory.GetHalf(registers[rs] + SignExtendImm15(imm)));
        }

        // Loads a half(2 byte data) from memory address rs + sign extended imm and stores zero extended data
        // in rt.
        private void lhiu(int rs, int rt, ushort imm)
        {
            registers[rt] = memory.GetHalf(registers[rs] + SignExtendImm15(imm));
        }

        // Loads a byte from memory address rs + rt and stores sign extended data in rd.
        private void lb(int rs, int rt, int rd)
        {
            registers[rd] = SignExtendByte(memory[registers[rs] + registers[rt]]);
        }

        // Loads a byte from memory address rs + rt and stores zero extended data in rd.
        private void lbu(int rs, int rt, int rd)
        {
            registers[rd] = memory[registers[rs] + registers[rt]];
        }

        // Loads a byte from memory address rs + sign extended imm and stores sign extended data in rt.
        private void lbi(int rs, int rt, ushort imm)
        {
            registers[rt] = SignExtendByte(memory[registers[rs] + SignExtendImm15(imm)]);
        }

        // Loads a byte from memory address rs + sign extended imm and stores zero extended data in rt.
        private void lbiu(int rs, int rt, ushort imm)
        {
            registers[rt] = memory[registers[rs] + SignExtendImm15(imm)];
        }

        // Stores a dword(8 byte data) from rd to memory address rs + rt.
        private void sd(int rs, int rt, int rd)
        {
            memory.SetDword(registers[rs] + registers[rt], registers[rd]);
        }

        // Stores a dword(8 byte data) from rd to memory address rs + (rt * 8).
        private void sds(int rs, int rt, int rd)
        {
            memory.SetDword(registers[rs] + (registers[rt] * 8), registers[rd]);
        }

        // Stores a dword(8 byte data) from rt to memory address rs + sign extended imm.
        private void sdi(int rs, int rt, ushort imm)
        {
            memory.SetDword(registers[rs] + SignExtendImm15(imm), registers[rt]);
        }

        // Stores a word(4 byte data) from lower 32-bits in rd to memory address rs + rt.
        private void sw(int rs, int rt, int rd)
        {
            memory.SetWord(registers[rs] + registers[rt], (uint)(registers[rd] & WORD_BIT_MASK));
        }

        // Stores a word(4 byte data) from lower 32-bits in rd to memory address rs + (rt * 4).
        private void sws(int rs, int rt, int rd)
        {
            memory.SetWord(registers[rs] + (registers[rt] * 4), (uint)(registers[rd] & WORD_BIT_MASK));
        }

        // Stores a word(4 byte data) from lower 32-bits in rt to memory address rs + sign extended imm.
        private void swi(int rs, int rt, ushort imm)
        {
            memory.SetWord(registers[rs] + SignExtendImm15(imm), (uint)(registers[rt] & WORD_BIT_MASK));
        }

        // Stores a half(2 byte data) from lower 16-bits in rd to memory address rs + rt.
        private void sh(int rs, int rt, int rd)
        {
            memory.SetHalf(registers[rs] + registers[rt], (ushort)(registers[rd] & HALF_BIT_MASK));
        }

        // Stores a half(2 byte data) from lower 16-bits in rd to memory address rs + (rt * 2).
        private void shs(int rs, int rt, int rd)
        {
            memory.SetHalf(registers[rs] + (registers[rt] * 2), (ushort)(registers[rd] & HALF_BIT_MASK));
        }

        // Stores a half(2 byte data) from lower 16-bits in rt to memory address rs + sign extended imm.
        private void shi(int rs, int rt, ushort imm)
        {
            memory.SetHalf(registers[rs] + SignExtendImm15(imm), (ushort)(registers[rt] & HALF_BIT_MASK));
        }

        // Stores a byte from lower 8-bits in rd to memory address rs + rt.
        private void sb(int rs, int rt, int rd)
        {
            memory[registers[rs] + registers[rt]] = (byte)(registers[rd] & BYTE_BIT_MASK);
        }

        // Stores a byte from lower 8-bits in rt to memory address rs + sign extended imm.
        private void sbi(int rs, int rt, ushort imm)
        {
            memory[registers[rs] + SignExtendImm15(imm)] = (byte)(registers[rt] & BYTE_BIT_MASK);
        }

        // Loads 16-bit imm data to [n * 16 + 15, n * 16] bits in rd. The other bits in rd
        // are unchanged. n can be 0, 1, 2, or 3.
        private void li_0(int rd, ushort imm)
        {
            ulong unchangeMask = ~HALF_BIT_MASK;
            registers[rd] = (registers[rd] & unchangeMask) | imm;
        }

        private void li_1(int rd, ushort imm)
        {
            ulong unchangeMask = ~(HALF_BIT_MASK << 16);
            registers[rd] = (registers[rd] & unchangeMask) | ((ulong)imm << 16);
        }

        private void li_2(int rd, ushort imm)
        {
            ulong unchangeMask = ~(HALF_BIT_MASK << 32);
            registers[rd] = (registers[rd] & unchangeMask) | ((ulong)imm << 32);
        }

        private void li_3(int rd, ushort imm)
        {
            ulong unchangeMask = ~(HALF_BIT_MASK << 48);
            registers[rd] = (registers[rd] & unchangeMask) | ((ulong)imm << 48);
        }

        // Copies value from %hi to rd.
        private void mfhi(int rd, ushort imm)
        {
            registers[rd] = registers.Hi;
        }

        // Copies value from %lo to rd.
        private void mflo(int rd, ushort imm)
        {
            registers[rd] = registers.Lo;
        }

        // Copies value from rd to %hi.
        private void mthi(int rd, ushort imm)
        {
            registers.Hi = registers[rd];
        }

        // Copies value from rd to %lo.
        private void mtlo(int rd, ushort imm)
        {
            registers.Lo = registers[rd];
        }

        // Sets pc to addr << 2 | pc[63..27]. The maximum jump range is 134,217,724(0x7FFFFFC).
        private void j(uint addr)
        {
            ulong unchangeMask = ~JUMP_BIT_MASK;
            registers.PC = (registers.PC & unchangeMask) | (((ulong)addr << 2) & JUMP_BIT_MASK);
        }

        // Sets %ra to the address of next instruction(current pc) then sets pc to
        // addr << 2 | pc[63..27]. The maximum jump range is 134,217,724(0x7FFFFFC).
        private void jal(uint addr)
        {
            registers[31] = registers.PC;

            ulong unchangeMask = ~JUMP_BIT_MASK;
            registers.PC = (registers.PC & unchangeMask) | (((ulong)addr << 2) & JUMP_BIT_MASK);
        }

        // Sets pc to the value in rs.
        private void jr(int rs, int rt, int rd)
        {
            registers.PC = registers[rs];
        }

        // Sets %ra to the address of next instruction(current pc) then sets pc to the value in rs.
        private void jalr(int rs, int rt, int rd)
        {
            registers[31] = registers.PC;
            registers.PC = registers[rs];
        }

        // If rs equals to rt, then sets pc to pc + sign extended imm * 4.
        private void beq(int rs, int rt, ushort imm)
        {
            if (registers[rs] == registers[rt])
            {
                long distance = ((long)SignExtendImm15(imm)) * 4;
                registers.PC += (ulong)distance;
            }
        }

        // If rs is not equal to rt, then sets pc to pc + sign extended imm * 4.
        private void bne(int rs, int rt, ushort imm)
        {
            if (registers[rs] != registers[rt])
            {
                long distance = ((long)SignExtendImm15(imm)) * 4;
                registers.PC += (ulong)distance;
            }
        }

        // If signed 64-bit integer rs is greater than or equal to signed 64-bit integer rt,
        // then sets pc to pc + sign extended imm * 4.
        private void bge(int rs, int rt, ushort imm)
        {
            if ((long)registers[rs] >= (long)registers[rt])
            {
                long distance = ((long)SignExtendImm15(imm)) * 4;
                registers.PC += (ulong)distance;
            }
        }

        // If unsigned 64-bit integer rs is greater than or equal to unsigned 64-bit integer rt,
        // then sets pc to pc + sign extended imm * 4.
        private void bgeu(int rs, int rt, ushort imm)
        {
            if (registers[rs] >= registers[rt])
            {
                long distance = ((long)SignExtendImm15(imm)) * 4;
                registers.PC += (ulong)distance;
            }
        }

        // If signed 64-bit integer rs is less than signed 64-bit integer rt, then sets pc to
        // pc + sign extended imm * 4.
        private void blt(int rs, int rt, ushort imm)
        {
            if ((long)registers[rs] < (long)registers[rt])
            {
                long distance = ((long)SignExtendImm15(imm)) * 4;
                registers.PC += (ulong)distance;
            }
        }

        // If unsigned 64-bit integer rs is less than unsigned 64-bit integer rt, then sets pc
        // to pc + sign extended imm * 4.
        private void bltu(int rs, int rt, ushort imm)
        {
            if (registers[rs] < registers[rt])
            {
                long distance = ((long)SignExtendImm15(imm)) * 4;
                registers.PC += (ulong)distance;
            }
        }

        // Calls OS-implemented system services. The value in %ret is used as the service number.
        // Values in %an are used as arguments.
        private void syscall(uint addr)
        {
            ulong service = registers[2];

            if (!syscallTable.ContainsKey(service))
            {
                throw new Exception($"Syscall error: no such service number {service}");
            }

            registers[2] = syscallTable[service](
                registers[3],
                registers[4],
                registers[5],
                registers[6],
                registers[7],
                registers[8]
                );
        }

        // Does nothing.
        private void nop(uint addr)
        {
            return;
        }

        // Loads 256-bit data from memory address rs + rt to the vector register vrd.
        private void vld(int rs, int rt, int rd)
        {
            vRegisters[rd] = memory.GetVector256<ulong>(registers[rs] + registers[rt]);
        }

        // Loads 128-bit data from memory address rs + rt to lower 128-bits in vector register vrd.
        private void vld_128(int rs, int rt, int rd)
        {
            Vector128<ulong> lo = memory.GetVector128<ulong>(registers[rs] + registers[rt]);
            vRegisters[rd] = Vector256.Create(lo, Vector128<ulong>.Zero);
        }

        // Loads 64-bit data from memory address rs + rt to lower 64-bits in vector register vrd.
        private void vld_64(int rs, int rt, int rd)
        {
            ulong lo = memory.GetDword(registers[rs] + registers[rt]);
            vRegisters[rd] = Vector256.CreateScalar(lo);
        }

        // Loads 32-bit data from memory address rs + rt to lower 32-bits in vector register vrd.
        private void vld_32(int rs, int rt, int rd)
        {
            uint lo = memory.GetWord(registers[rs] + registers[rt]);
            vRegisters[rd] = Vector256.CreateScalar(lo).AsUInt64();
        }

        // Loads 64-bit data from %lo to the lowest 64-bits in vector register vrd and 64-bit data
        // from %hi to the second lowest 64-bits in vrd.
        private void vldhilo(int rs, int rt, int rd)
        {
            vRegisters[rd] = Vector256.Create(registers.Lo, registers.Hi, 0, 0);
        }

        // Loads 64-bit data from rs to lower 64-bits in vector register vrd.
        private void vldr(int rs, int rt, int rd)
        {
            vRegisters[rd] = Vector256.CreateScalar(registers[rs]);
        }

        // Loads lower 32-bit data from rs to lower 32-bits in vector register vrd.
        private void vldr_32(int rs, int rt, int rd)
        {
            uint lo = (uint)(registers[rs] & WORD_BIT_MASK);
            vRegisters[rd] = Vector256.CreateScalar(lo).AsUInt64();
        }

        // Loads sign extended 64-bit data imm to the lower 64-bits in vector register vrd.
        private void vldi_64(int rd, ushort imm)
        {
            vRegisters[rd] = Vector256.CreateScalar(SignExtendUshort(imm));
        }

        // Loads zero extended 64-bit data imm to the lower 64-bits in vector register vrd.
        private void vldiu_64(int rd, ushort imm)
        {
            vRegisters[rd] = Vector256.CreateScalar((ulong)imm);
        }

        // Loads sign extended 32-bit data imm to the lower 32-bits in vector register vrd.
        private void vldi_32(int rd, ushort imm)
        {
            uint lo = (uint)(SignExtendUshort(imm) & WORD_BIT_MASK);
            vRegisters[rd] = Vector256.CreateScalar(lo).AsUInt64();
        }

        // Loads sign extended 32-bit data imm to the lower 32-bits in vector register vrd.
        private void vldiu_32(int rd, ushort imm)
        {
            vRegisters[rd] = Vector256.CreateScalar((uint)imm).AsUInt64();
        }

        // Stores 256-bit data from the vector register vrd to memory address rs + rt.
        private void vst(int rs, int rt, int rd)
        {
            memory.SetVector256(registers[rs] + registers[rt], vRegisters[rd]);
        }

        // Stores lower 128-bit data from the vector register vrd to memory address rs + rt.
        private void vst_128(int rs, int rt, int rd)
        {
            memory.SetVector128(registers[rs] + registers[rt], vRegisters[rd].GetLower());
        }

        // Stores lower 64-bit data from the vector register vrd to memory address rs + rt.
        private void vst_64(int rs, int rt, int rd)
        {
            memory.SetDword(registers[rs] + registers[rt], vRegisters[rd].ToScalar());
        }

        // Stores lower 32-bit data from the vector register vrd to memory address rs + rt.
        private void vst_32(int rs, int rt, int rd)
        {
            memory.SetWord(registers[rs] + registers[rt], vRegisters[rd].AsUInt32().ToScalar());
        }

        // Stores the lowest 64-bit data from the vector register vrd to %lo and the second lowest
        // 64-bit data from vrd to %hi
        private void vsthilo(int rs, int rt, int rd)
        {
            registers.Lo = vRegisters[rd].GetElement(0);
            registers.Hi = vRegisters[rd].GetElement(1);
        }

        // Stores lower 64-bit data from the vector register vrd to rs.
        private void vstr(int rs, int rt, int rd)
        {
            registers[rs] = vRegisters[rd].ToScalar();
        }

        // Stores sign extended lower 32-bit data from the vector register vrd to rs.
        private void vstr_32(int rs, int rt, int rd)
        {
            registers[rs] = SignExtendUint(vRegisters[rd].AsUInt32().ToScalar());
        }

        // Stores zero extended lower 32-bit data from the vector register vrd to rs.
        private void vstr_32u(int rs, int rt, int rd)
        {
            registers[rs] = vRegisters[rd].AsUInt32().ToScalar();
        }

        // Fills the vector register vrd with same 4 64-bit data in memory address rs + rt.
        private void vbroad_64(int rs, int rt, int rd)
        {
            ulong data = memory.GetDword(registers[rs] + registers[rt]);
            vRegisters[rd] = Vector256.Create(data);
        }

        // Fills the vector register vrd with same 8 32-bit data in memory address rs + rt.
        private void vbroad_32(int rs, int rt, int rd)
        {
            uint data = memory.GetWord(registers[rs] + registers[rt]);
            vRegisters[rd] = Vector256.Create(data).AsUInt64();
        }

        // Fills the vector register vrd with same 4 64-bit data in rs.
        private void vbroadr(int rs, int rt, int rd)
        {
            vRegisters[rd] = Vector256.Create(registers[rs]);
        }

        // Fills the vector register vrd with same 8 32-bit data in lower 32-bits in rs.
        private void vbroadr_32(int rs, int rt, int rd)
        {
            uint data = (uint)(registers[rs] & WORD_BIT_MASK);
            vRegisters[rd] = Vector256.Create(data).AsUInt64();
        }

        // Fills the vector register vrd with same 4 64-bit sign extended data imm.
        private void vbroadi_64(int rd, ushort imm)
        {
            vRegisters[rd] = Vector256.Create(SignExtendUshort(imm));
        }

        // Fills the vector register vrd with same 4 64-bit zero extended data imm.
        private void vbroadiu_64(int rd, ushort imm)
        {
            vRegisters[rd] = Vector256.Create((ulong)imm);
        }

        // Fills the vector register vrd with same 8 32-bit sign extended data imm.
        private void vbroadi_32(int rd, ushort imm)
        {
            uint data = (uint)(SignExtendUshort(imm) & WORD_BIT_MASK);
            vRegisters[rd] = Vector256.Create(data).AsUInt64();
        }

        // Fills the vector register vrd with same 8 32-bit zero extended data imm.
        private void vbroadiu_32(int rd, ushort imm)
        {
            vRegisters[rd] = Vector256.Create((uint)imm).AsUInt64();
        }

        // Converts 4 64-bit signed integers in vector register vrs to 4 double
        // precision floating numbers and stores in vector register vrd.
        private void vcvti64tof64(int rs, int rt, int rd)
        {
            vRegisters[rd] = Vector256.ConvertToDouble(vRegisters[rs].AsInt64()).AsUInt64();
        }

        // Converts 64-bit signed integer in lower 64-bits in vector register vrs
        // to double precision floating number and stores in lower 64-bits in vector register vrd.
        private void vcvti64tof64_s(int rs, int rt, int rd)
        {
            vRegisters[rd] = Vector256.CreateScalar((double)vRegisters[rs].AsInt64().ToScalar()).AsUInt64();
        }

        // Converts 4 64-bit unsigned integers in vector register vrs to 4 double precision floating
        // numbers and stores in vector register vrd.
        private void vcvtu64tof64(int rs, int rt, int rd)
        {
            vRegisters[rd] = Vector256.ConvertToDouble(vRegisters[rs]).AsUInt64();
        }

        // Converts 64-bit unsigned integer in lower 64-bits in vector register vrs to double precision
        // floating number and stores in lower 64-bits in vector register vrd.
        private void vcvtu64tof64_s(int rs, int rt, int rd)
        {
            vRegisters[rd] = Vector256.CreateScalar((double)vRegisters[rs].ToScalar()).AsUInt64();
        }

        // Converts 4 64-bit signed integers in vector register vrs to 4 single precision floating
        // numbers and stores in lower 128-bits in vector register vrd.
        private void vcvti64tof32(int rs, int rt, int rd)
        {
            Vector256<double> doubles = Vector256.ConvertToDouble(vRegisters[rs].AsInt64());
            Vector128<float> singles = Avx.ConvertToVector128Single(doubles);
            vRegisters[rd] = Vector256.Create(singles, Vector128<float>.Zero).AsUInt64();
        }

        // Converts 64-bit signed integer in lower 64-bits in vector register vrs to single precision
        // floating number and stores in lower 32-bits in vector register vrd.
        private void vcvti64tof32_s(int rs, int rt, int rd)
        {
            vRegisters[rd] = Vector256.CreateScalar((float)vRegisters[rs].AsInt64().ToScalar()).AsUInt64();
        }

        // Converts 4 64-bit unsigned integers in vector register vrs to 4 single precision floating
        // numbers and stores in lower 128-bits in vector register vrd.
        private void vcvtu64tof32(int rs, int rt, int rd)
        {
            Vector256<double> doubles = Vector256.ConvertToDouble(vRegisters[rs]);
            Vector128<float> singles = Avx.ConvertToVector128Single(doubles);
            vRegisters[rd] = Vector256.Create(singles, Vector128<float>.Zero).AsUInt64();
        }

        // Converts 64-bit unsigned integer in lower 64-bits in vector register vrs to single precision
        // floating number and stores in lower 32-bits in vector register vrd.
        private void vcvtu64tof32_s(int rs, int rt, int rd)
        {
            vRegisters[rd] = Vector256.CreateScalar((float)vRegisters[rs].ToScalar()).AsUInt64();
        }

        // Converts 4 double precision floating numbers in vector register vrs to 4 single precision
        // floating numbers and stores in lower 128-bits in vector register vrd.
        private void vcvtf64tof32(int rs, int rt, int rd)
        {
            Vector128<float> singles = Avx.ConvertToVector128Single(vRegisters[rs].AsDouble());
            vRegisters[rd] = Vector256.Create(singles, Vector128<float>.Zero).AsUInt64();
        }

        // Converts double precision floating numbers in lower 64-bits in vector register vrs to single
        // precision floating numbers and stores in lower 32-bits in vector register vrd.
        private void vcvtf64tof32_s(int rs, int rt, int rd)
        {
            vRegisters[rd] = Vector256.CreateScalar((float)vRegisters[rs].AsDouble().ToScalar()).AsUInt64();
        }

        // Converts 4 single precision floating numbers in lower 128-bits in vector register vrs to 4
        // double precision floating numbers and stores in vector register vrd.
        private void vcvtf32tof64(int rs, int rt, int rd)
        {
            Vector128<float> singles = vRegisters[rs].GetLower().AsSingle();
            vRegisters[rd] = Avx.ConvertToVector256Double(singles).AsUInt64();
        }

        // Converts single precision floating numbers in lower 32-bits in vector register vrs to double
        // precision floating numbers and stores in lower 64-bits in vector register vrd.
        private void vcvtf32tof64_s(int rs, int rt, int rd)
        {
            vRegisters[rd] = Vector256.CreateScalar((double)vRegisters[rs].AsSingle().ToScalar()).AsUInt64();
        }

        // Converts 4 double precision floating numbers in vector register vrs to 4 64-bit integers and
        // stores in vector register vrd. The rounding mode is determined with the lowest 2-bits in rt.
        // rt = 0: round to the closest integer.
        // rt = 1: round to the largest integer which is less than or equal to the value.
        // otherwise: round to the smallest integer which is larger than or equal to the value.
        private void vcvtf64toi64(int rs, int rt, int rd)
        {
            ulong flag = registers[rt] & 0b11UL;
            Func<double, long> converter;
            if (flag == 0)
            {
                converter = d => (long)Math.Round(d);
            }
            else if (flag == 1)
            {
                converter = d => (long)Math.Floor(d);
            }
            else
            {
                converter = d => (long)Math.Ceiling(d);
            }

            Vector256<double> doubles = vRegisters[rs].AsDouble();
            vRegisters[rd] = Vector256.Create(
                converter(doubles.GetElement(0)),
                converter(doubles.GetElement(1)),
                converter(doubles.GetElement(2)),
                converter(doubles.GetElement(3))).AsUInt64();
        }

        // Converts double precision floating number in lower 64-bits in vector register vrs to 64-bit
        // integer and stores in lower 64-bits in vector register vrd. The rounding mode is determined
        // with the lowest 2-bits in rt.
        // rt = 0: round to the closest integer.
        // rt = 1: round to the largest integer which is less than or equal to the value.
        // otherwise: round to the smallest integer which is larger than or equal to the value.
        private void vcvtf64toi64_s(int rs, int rt, int rd)
        {
            ulong flag = registers[rt] & 0b11UL;
            Func<double, long> converter;
            if (flag == 0)
            {
                converter = d => (long)Math.Round(d);
            }
            else if (flag == 1)
            {
                converter = d => (long)Math.Floor(d);
            }
            else
            {
                converter = d => (long)Math.Ceiling(d);
            }

            vRegisters[rd] = Vector256.CreateScalar(converter(vRegisters[rs].AsDouble().ToScalar())).AsUInt64();
        }

        // Converts 4 double precision floating numbers in vector register vrs to 4 32-bit integers and
        // stores in lower 128-bits in vector register vrd. The rounding mode is determined with the lowest
        // 2-bits in rt.
        // rt = 0: round to the closest integer.
        // rt = 1: round to the largest integer which is less than or equal to the value.
        // otherwise: round to the smallest integer which is larger than or equal to the value.
        private void vcvtf64toi32(int rs, int rt, int rd)
        {
            ulong flag = registers[rt] & 0b11UL;
            Func<double, int> converter;
            if (flag == 0)
            {
                converter = d => (int)Math.Round(d);
            }
            else if (flag == 1)
            {
                converter = d => (int)Math.Floor(d);
            }
            else
            {
                converter = d => (int)Math.Ceiling(d);
            }

            Vector256<double> doubles = vRegisters[rs].AsDouble();
            vRegisters[rd] = Vector256.Create(
                converter(doubles.GetElement(0)),
                converter(doubles.GetElement(1)),
                converter(doubles.GetElement(2)),
                converter(doubles.GetElement(3)),
                0, 0, 0, 0).AsUInt64();
        }

        // Converts double precision floating number in lower 64-bits in vector register vrs to 32-bit
        // integer and stores in lower 32-bits in vector register vrd. The rounding mode is determined
        // with the lowest 2-bits in rt.
        // rt = 0: round to the closest integer.
        // rt = 1: round to the largest integer which is less than or equal to the value.
        // otherwise: round to the smallest integer which is larger than or equal to the value.
        private void vcvtf64toi32_s(int rs, int rt, int rd)
        {
            ulong flag = registers[rt] & 0b11UL;
            Func<double, int> converter;
            if (flag == 0)
            {
                converter = d => (int)Math.Round(d);
            }
            else if (flag == 1)
            {
                converter = d => (int)Math.Floor(d);
            }
            else
            {
                converter = d => (int)Math.Ceiling(d);
            }

            vRegisters[rd] = Vector256.CreateScalar(converter(vRegisters[rs].AsDouble().ToScalar())).AsUInt64();
        }

        // Converts 4 single precision floating numbers in lower 128-bits in vector register vrs to 4 64-bit
        // integers and stores in vector register vrd. The rounding mode is determined with the lowest
        // 2-bits in rt.
        // rt = 0: round to the closest integer.
        // rt = 1: round to the largest integer which is less than or equal to the value.
        // otherwise: round to the smallest integer which is larger than or equal to the value.
        private void vcvtf32toi64(int rs, int rt, int rd)
        {
            ulong flag = registers[rt] & 0b11UL;
            Func<float, long> converter;
            if (flag == 0)
            {
                converter = d => (long)MathF.Round(d);
            }
            else if (flag == 1)
            {
                converter = d => (long)MathF.Floor(d);
            }
            else
            {
                converter = d => (long)MathF.Ceiling(d);
            }

            Vector128<float> floats = vRegisters[rs].GetLower().AsSingle();
            vRegisters[rd] = Vector256.Create(
                converter(floats.GetElement(0)),
                converter(floats.GetElement(1)),
                converter(floats.GetElement(2)),
                converter(floats.GetElement(3))).AsUInt64();
        }

        // Converts single precision floating number in lower 32-bits in vector register vrs to 64-bit
        // integer and stores in lower 64-bits in vector register vrd. The rounding mode is determined
        // with the lowest 2-bits in rt.
        // rt = 0: round to the closest integer.
        // rt = 1: round to the largest integer which is less than or equal to the value.
        // otherwise: round to the smallest integer which is larger than or equal to the value.
        private void vcvtf32toi64_s(int rs, int rt, int rd)
        {
            ulong flag = registers[rt] & 0b11UL;
            Func<float, long> converter;
            if (flag == 0)
            {
                converter = d => (long)MathF.Round(d);
            }
            else if (flag == 1)
            {
                converter = d => (long)MathF.Floor(d);
            }
            else
            {
                converter = d => (long)MathF.Ceiling(d);
            }

            vRegisters[rd] = Vector256.CreateScalar(converter(vRegisters[rs].AsSingle().ToScalar())).AsUInt64();
        }

        // Converts 8 single precision floating numbers in vector register vrs to 8 32-bit integers and
        // stores in vector register vrd. The rounding mode is determined with the lowest 2-bits in rt.
        // rt = 0: round to the closest integer.
        // rt = 1: round to the largest integer which is less than or equal to the value.
        // otherwise: round to the smallest integer which is larger than or equal to the value.
        private void vcvtf32toi32(int rs, int rt, int rd)
        {
            ulong flag = registers[rt] & 0b11UL;
            Func<float, int> converter;
            if (flag == 0)
            {
                converter = d => (int)MathF.Round(d);
            }
            else if (flag == 1)
            {
                converter = d => (int)MathF.Floor(d);
            }
            else
            {
                converter = d => (int)MathF.Ceiling(d);
            }

            Vector256<float> floats = vRegisters[rs].AsSingle();
            vRegisters[rd] = Vector256.Create(
                converter(floats.GetElement(0)),
                converter(floats.GetElement(1)),
                converter(floats.GetElement(2)),
                converter(floats.GetElement(3)),
                converter(floats.GetElement(4)),
                converter(floats.GetElement(5)),
                converter(floats.GetElement(6)),
                converter(floats.GetElement(7))).AsUInt64();
        }

        // Converts single precision floating number in lower 32-bits in vector register vrs to 32-bit
        // integer and stores in lower 32-bits in vector register vrd. The rounding mode is determined
        // with the lowest 2-bits in rt.
        // rt = 0: round to the closest integer.
        // rt = 1: round to the largest integer which is less than or equal to the value.
        // otherwise: round to the smallest integer which is larger than or equal to the value.
        private void vcvtf32toi32_s(int rs, int rt, int rd)
        {
            ulong flag = registers[rt] & 0b11UL;
            Func<float, int> converter;
            if (flag == 0)
            {
                converter = d => (int)MathF.Round(d);
            }
            else if (flag == 1)
            {
                converter = d => (int)MathF.Floor(d);
            }
            else
            {
                converter = d => (int)MathF.Ceiling(d);
            }

            vRegisters[rd] = Vector256.CreateScalar(converter(vRegisters[rs].AsSingle().ToScalar())).AsUInt64();
        }

        // Adds 4 64-bit integers component-wise in two vector registers vrs and vrt and stores the result
        // in vector register vrd.
        private void vaddi64(int rs, int rt, int rd)
        {
            vRegisters[rd] = vRegisters[rs] + vRegisters[rt];
        }

        // Adds 8 32-bit integers component-wise in two vector registers vrs and vrt and stores the result
        // in vector register vrd.
        private void vaddi32(int rs, int rt, int rd)
        {
            Vector256<uint> vrs = vRegisters[rs].AsUInt32();
            Vector256<uint> vrt = vRegisters[rt].AsUInt32();
            vRegisters[rd] = (vrs + vrt).AsUInt64();
        }

        // Subtracts 4 64-bit integers component-wise in two vector registers vrs and vrt and stores the
        // result in vector register vrd.
        private void vsubi64(int rs, int rt, int rd)
        {
            vRegisters[rd] = vRegisters[rs] - vRegisters[rt];
        }

        // Subtracts 8 32-bit integers component-wise in two vector registers vrs and vrt and stores the
        // result in vector register vrd.
        private void vsubi32(int rs, int rt, int rd)
        {
            Vector256<uint> vrs = vRegisters[rs].AsUInt32();
            Vector256<uint> vrt = vRegisters[rt].AsUInt32();
            vRegisters[rd] = (vrs - vrt).AsUInt64();
        }

        // Adds 4 double precision floating numbers component-wise in two vector registers vrs and vrt
        // and stores the result in vector register vrd.
        private void vaddf64(int rs, int rt, int rd)
        {
            Vector256<double> vrs = vRegisters[rs].AsDouble();
            Vector256<double> vrt = vRegisters[rt].AsDouble();
            vRegisters[rd] = (vrs + vrt).AsUInt64();
        }

        // Adds double precision floating numbers in lower 64-bits in two vector registers vrs and vrt
        // and stores the result in lower 64-bits in vector register vrd.
        private void vaddf64_s(int rs, int rt, int rd)
        {
            Vector256<double> vrs = vRegisters[rs].AsDouble();
            Vector256<double> vrt = vRegisters[rt].AsDouble();
            vRegisters[rd] = Vector256.CreateScalar(vrs.ToScalar() + vrt.ToScalar()).AsUInt64();
        }

        // Adds 8 single precision floating numbers component-wise in two vector registers vrs and vrt and
        // stores the result in vector register vrd.
        private void vaddf32(int rs, int rt, int rd)
        {
            Vector256<float> vrs = vRegisters[rs].AsSingle();
            Vector256<float> vrt = vRegisters[rt].AsSingle();
            vRegisters[rd] = (vrs + vrt).AsUInt64();
        }

        // Adds single precision floating numbers in lower 32-bits in two vector registers vrs and vrt and
        // stores the result in lower 32-bits in vector register vrd.
        private void vaddf32_s(int rs, int rt, int rd)
        {
            Vector256<float> vrs = vRegisters[rs].AsSingle();
            Vector256<float> vrt = vRegisters[rt].AsSingle();
            vRegisters[rd] = Vector256.CreateScalar(vrs.ToScalar() + vrt.ToScalar()).AsUInt64();
        }

        // Subtracts 4 double precision floating numbers component-wise in two vector registers vrs and vrt
        // and stores the result in vector register vrd.
        private void vsubf64(int rs, int rt, int rd)
        {
            Vector256<double> vrs = vRegisters[rs].AsDouble();
            Vector256<double> vrt = vRegisters[rt].AsDouble();
            vRegisters[rd] = (vrs - vrt).AsUInt64();
        }

        // Subtracts double precision floating numbers in lower 64-bits in two vector registers vrs and vrt
        // and stores the result in lower 64-bits in vector register vrd.
        private void vsubf64_s(int rs, int rt, int rd)
        {
            Vector256<double> vrs = vRegisters[rs].AsDouble();
            Vector256<double> vrt = vRegisters[rt].AsDouble();
            vRegisters[rd] = Vector256.CreateScalar(vrs.ToScalar() - vrt.ToScalar()).AsUInt64();
        }

        // Subtracts 8 single precision floating numbers component-wise in two vector registers vrs and vrt
        // and stores the result in vector register vrd.
        private void vsubf32(int rs, int rt, int rd)
        {
            Vector256<float> vrs = vRegisters[rs].AsSingle();
            Vector256<float> vrt = vRegisters[rt].AsSingle();
            vRegisters[rd] = (vrs - vrt).AsUInt64();
        }

        // Subtracts single precision floating numbers in lower 32-bits in two vector registers vrs and vrt
        // and stores the result in lower 32-bits in vector register vrd.
        private void vsubf32_s(int rs, int rt, int rd)
        {
            Vector256<float> vrs = vRegisters[rs].AsSingle();
            Vector256<float> vrt = vRegisters[rt].AsSingle();
            vRegisters[rd] = Vector256.CreateScalar(vrs.ToScalar() - vrt.ToScalar()).AsUInt64();
        }

        // Multiplies 4 signed 32-bit integers component-wise in lower 128-bits in two vector registers vrs
        // and vrt and stores the 4 signed 64-bit integers result in vector register vrd.
        private void vmuli32(int rs, int rt, int rd)
        {
            Vector128<int> vrs = vRegisters[rs].GetLower().AsInt32();
            Vector128<int> vrt = vRegisters[rt].GetLower().AsInt32();
            vRegisters[rd] = Vector256.Create(
                (long)vrs[0] * vrt[0],
                (long)vrs[1] * vrt[1],
                (long)vrs[2] * vrt[2],
                (long)vrs[3] * vrt[3]).AsUInt64();
        }

        // Multiplies 4 unsigned 32-bit integers component-wise in lower 128-bits in two vector registers vrs
        // and vrt and stores the 4 unsigned 64-bit integers result in vector register vrd.
        private void vmulu32(int rs, int rt, int rd)
        {
            Vector128<uint> vrs = vRegisters[rs].GetLower().AsUInt32();
            Vector128<uint> vrt = vRegisters[rt].GetLower().AsUInt32();
            vRegisters[rd] = Vector256.Create(
                (ulong)vrs[0] * vrt[0],
                (ulong)vrs[1] * vrt[1],
                (ulong)vrs[2] * vrt[2],
                (ulong)vrs[3] * vrt[3]);
        }

        // Multiplies 4 double precision floating numbers component-wise in two vector registers vrs and vrt
        // and stores the result in vector register vrd.
        private void vmulf64(int rs, int rt, int rd)
        {
            Vector256<double> vrs = vRegisters[rs].AsDouble();
            Vector256<double> vrt = vRegisters[rt].AsDouble();
            vRegisters[rd] = (vrs * vrt).AsUInt64();
        }

        // Multiplies double precision floating numbers in lower 64-bits in two vector registers vrs and vrt
        // and stores the result in lower 64-bits in vector register vrd.
        private void vmulf64_s(int rs, int rt, int rd)
        {
            Vector256<double> vrs = vRegisters[rs].AsDouble();
            Vector256<double> vrt = vRegisters[rt].AsDouble();
            vRegisters[rd] = Vector256.CreateScalar(vrs.ToScalar() * vrt.ToScalar()).AsUInt64();
        }

        // Multiplies 8 single precision floating numbers component-wise in two vector registers vrs and vrt
        // and stores the result in vector register vrd.
        private void vmulf32(int rs, int rt, int rd)
        {
            Vector256<float> vrs = vRegisters[rs].AsSingle();
            Vector256<float> vrt = vRegisters[rt].AsSingle();
            vRegisters[rd] = (vrs * vrt).AsUInt64();
        }

        // Multiplies single precision floating numbers in lower 32-bits in two vector registers vrs and vrt
        // and stores the result in lower 32-bits in vector register vrd.
        private void vmulf32_s(int rs, int rt, int rd)
        {
            Vector256<float> vrs = vRegisters[rs].AsSingle();
            Vector256<float> vrt = vRegisters[rt].AsSingle();
            vRegisters[rd] = Vector256.CreateScalar(vrs.ToScalar() * vrt.ToScalar()).AsUInt64();
        }

        // Divides 4 double precision floating numbers component-wise in two vector registers vrs and vrt
        // and stores the result in vector register vrd.
        private void vdivf64(int rs, int rt, int rd)
        {
            Vector256<double> vrs = vRegisters[rs].AsDouble();
            Vector256<double> vrt = vRegisters[rt].AsDouble();
            vRegisters[rd] = (vrs / vrt).AsUInt64();
        }

        // Divides double precision floating numbers in lower 64-bits in two vector registers vrs and vrt
        // and stores the result in lower 64-bits in vector register vrd.
        private void vdivf64_s(int rs, int rt, int rd)
        {
            Vector256<double> vrs = vRegisters[rs].AsDouble();
            Vector256<double> vrt = vRegisters[rt].AsDouble();
            vRegisters[rd] = Vector256.CreateScalar(vrs.ToScalar() / vrt.ToScalar()).AsUInt64();
        }

        // Divides 8 single precision floating numbers component-wise in two vector registers vrs and vrt
        // and stores the result in vector register vrd.
        private void vdivf32(int rs, int rt, int rd)
        {
            Vector256<float> vrs = vRegisters[rs].AsSingle();
            Vector256<float> vrt = vRegisters[rt].AsSingle();
            vRegisters[rd] = (vrs / vrt).AsUInt64();
        }

        // Divides single precision floating numbers in lower 32-bits in two vector registers vrs and vrt
        // and stores the result in lower 32-bits in vector register vrd.
        private void vdivf32_s(int rs, int rt, int rd)
        {
            Vector256<float> vrs = vRegisters[rs].AsSingle();
            Vector256<float> vrt = vRegisters[rt].AsSingle();
            vRegisters[rd] = Vector256.CreateScalar(vrs.ToScalar() / vrt.ToScalar()).AsUInt64();
        }

        // Calculates logical and of two vector registers vrs and vrt and stores the result in the vector
        // register vrd.
        private void vand(int rs, int rt, int rd)
        {
            vRegisters[rd] = vRegisters[rs] & vRegisters[rt];
        }

        // Calculates logical or of two vector registers vrs and vrt and stores the result in the vector
        // register vrd.
        private void vor(int rs, int rt, int rd)
        {
            vRegisters[rd] = vRegisters[rs] | vRegisters[rt];
        }

        // Calculates logical xor of two vector registers vrs and vrt and stores the result in the vector
        // register vrd.
        private void vxor(int rs, int rt, int rd)
        {
            vRegisters[rd] = vRegisters[rs] ^ vRegisters[rt];
        }

        // Shifts the 256-bit data in vector register vrs to the left by amount held in lower 8 bits in rt.
        // Fills the empty space to 0. Stores the result in the vector register vrd.
        private void vslla(int rs, int rt, int rd)
        {
            int shamt = (int)(uint)(registers[rt] & 0b1111_1111U);

            Vector256<ulong> vrs = vRegisters[rs];
            UInt128 lo = new UInt128(vrs[1], vrs[0]);
            UInt128 hi = new UInt128(vrs[3], vrs[2]);

            UInt128 upper;
            UInt128 lower;

            if (shamt > 128)
            {
                upper = lo << (shamt - 128);
                lower = UInt128.Zero;
            }
            else
            {
                lower = lo << shamt;
                upper = (hi << shamt) | (lo >> (128 - shamt));
            }

            vRegisters[rd] = Vector256.Create(
                Vector128.Create(GetBytesFromUInt128(lower)),
                Vector128.Create(GetBytesFromUInt128(upper))).AsUInt64();
        }

        // Shifts 4 64-bit data in vector register vrs to the left by amount of values held in each
        // lower 6 bits of corresponding segment in vector register vrt. Fills the empty space to 0.
        // Stores the result in the vector register vrd.
        private void vsll_64(int rs, int rt, int rd)
        {
            Vector256<ulong> mask = Vector256.Create(0b11_1111UL);
            Vector256<ulong> shamt = vRegisters[rt] & mask;
            vRegisters[rd] = Avx2.ShiftLeftLogicalVariable(vRegisters[rs], shamt);
        }

        // Shifts 8 32-bit data in vector register vrs to the left by amount of values held in each
        // lower 5 bits of corresponding segment in vector register vrt. Fills the empty space to 0.
        // Stores the result in the vector register vrd.
        private void vsll_32(int rs, int rt, int rd)
        {
            Vector256<uint> mask = Vector256.Create(0b1_1111U);
            Vector256<uint> shamt = vRegisters[rt].AsUInt32() & mask;
            vRegisters[rd] = Avx2.ShiftLeftLogicalVariable(vRegisters[rs].AsUInt32(), shamt).AsUInt64();
        }

        // Shifts the 256-bit data in vector register vrs to the right by amount held in lower 8 bits in
        // rt. Fills the empty space to 0. Stores the result in the vector register vrd.
        private void vsrla(int rs, int rt, int rd)
        {
            int shamt = (int)(uint)(registers[rt] & 0b1111_1111U);

            Vector256<ulong> vrs = vRegisters[rs];
            UInt128 lo = new UInt128(vrs[1], vrs[0]);
            UInt128 hi = new UInt128(vrs[3], vrs[2]);

            UInt128 upper;
            UInt128 lower;

            if (shamt > 128)
            {
                lower = hi >> (shamt - 128);
                upper = UInt128.Zero;
            }
            else
            {
                lower = (lo >> shamt) | (hi << (128 - shamt));
                upper = hi >> shamt;
            }

            vRegisters[rd] = Vector256.Create(
                Vector128.Create(GetBytesFromUInt128(lower)),
                Vector128.Create(GetBytesFromUInt128(upper))).AsUInt64();
        }

        // Shifts 4 64-bit data in vector register vrs to the right by amount of values held in each
        // lower 6 bits of corresponding segment in vector register vrt. Fills the empty space to 0.
        // Stores the result in the vector register vrd.
        private void vsrl_64(int rs, int rt, int rd)
        {
            Vector256<ulong> mask = Vector256.Create(0b11_1111UL);
            Vector256<ulong> shamt = vRegisters[rt] & mask;
            vRegisters[rd] = Avx2.ShiftRightLogicalVariable(vRegisters[rs], shamt);
        }

        // Shifts 8 32-bit data in vector register vrs to the right by amount of values held in each lower
        // 5 bits of corresponding segment in vector register vrt. Fills the empty space to 0. Stores the
        // result in the vector register vrd.
        private void vsrl_32(int rs, int rt, int rd)
        {
            Vector256<uint> mask = Vector256.Create(0b1_1111U);
            Vector256<uint> shamt = vRegisters[rt].AsUInt32() & mask;
            vRegisters[rd] = Avx2.ShiftRightLogicalVariable(vRegisters[rs].AsUInt32(), shamt).AsUInt64();
        }

        // Shifts 4 64-bit data in vector register vrs to the right by amount of values held in each lower
        // 6 bits of corresponding segment in vector register vrt. Fills the empty space to the most
        // significant bit of each data. Stores the result in the vector register vrd.
        private void vsra_64(int rs, int rt, int rd)
        {
            Vector256<ulong> mask = Vector256.Create(0b11_1111UL);
            Vector256<ulong> shamt = vRegisters[rt] & mask;
            Vector256<long> vrs = vRegisters[rs].AsInt64();
            vRegisters[rd] = Vector256.Create(
                vrs[0] >> (int)(uint)shamt[0],
                vrs[1] >> (int)(uint)shamt[1],
                vrs[2] >> (int)(uint)shamt[2],
                vrs[3] >> (int)(uint)shamt[3]).AsUInt64();
        }

        // Shifts 8 32-bit data in vector register vrs to the right by amount of values held in each lower
        // 5 bits of corresponding segment in vector register vrt. Fills the empty space to the most
        // significant bit of each data. Stores the result in the vector register vrd.
        private void vsra_32(int rs, int rt, int rd)
        {
            Vector256<uint> mask = Vector256.Create(0b1_1111U);
            Vector256<uint> shamt = vRegisters[rt].AsUInt32() & mask;
            vRegisters[rd] = Avx2.ShiftRightArithmeticVariable(vRegisters[rs].AsInt32(), shamt).AsUInt64();
        }

        // Compares 4 signed 64-bit integers componentwise in two vector registers vrs and vrt.
        // If component in vrs is greater than or equal to component in vrt, stores 1 in the component
        // in vector register vrd. Else, stores 0.
        private void vsgei64(int rs, int rt, int rd)
        {
            Vector256<long> vrs = vRegisters[rs].AsInt64();
            Vector256<long> vrt = vRegisters[rt].AsInt64();

            Vector256<long> result = Vector256.GreaterThanOrEqual(vrs, vrt);
            Vector256<long> mask = Vector256.Create(1L);
            vRegisters[rd] = (result & mask).AsUInt64();
        }

        // Compares 4 unsigned 64-bit integers componentwise in two vector registers vrs and vrt.
        // If component in vrs is greater than or equal to component in vrt, stores 1 in the component
        // in vector register vrd. Else, stores 0.
        private void vsgeu64(int rs, int rt, int rd)
        {
            Vector256<ulong> vrs = vRegisters[rs];
            Vector256<ulong> vrt = vRegisters[rt];

            Vector256<ulong> result = Vector256.GreaterThanOrEqual(vrs, vrt);
            Vector256<ulong> mask = Vector256.Create(1UL);
            vRegisters[rd] = result & mask;
        }

        // Compares 8 signed 32-bit integers componentwise in two vector registers vrs and vrt.
        // If component in vrs is greater than or equal to component in vrt, stores 1 in the component
        // in vector register vrd. Else, stores 0.
        private void vsgei32(int rs, int rt, int rd)
        {
            Vector256<int> vrs = vRegisters[rs].AsInt32();
            Vector256<int> vrt = vRegisters[rt].AsInt32();

            Vector256<int> result = Vector256.GreaterThanOrEqual(vrs, vrt);
            Vector256<int> mask = Vector256.Create(1);
            vRegisters[rd] = (result & mask).AsUInt64();
        }

        // Compares 8 unsigned 32-bit integers componentwise in two vector registers vrs and vrt.
        // If component in vrs is greater than or equal to component in vrt, stores 1 in the component
        // in vector register vrd. Else, stores 0.
        private void vsgeu32(int rs, int rt, int rd)
        {
            Vector256<uint> vrs = vRegisters[rs].AsUInt32();
            Vector256<uint> vrt = vRegisters[rt].AsUInt32();

            Vector256<uint> result = Vector256.GreaterThanOrEqual(vrs, vrt);
            Vector256<uint> mask = Vector256.Create(1U);
            vRegisters[rd] = (result & mask).AsUInt64();
        }

        // Compares 4 double precision floating point numbers componentwise in two vector registers
        // vrs and vrt. If component in vrs is greater than or equal to component in vrt, stores 1
        // in the component in vector register vrd. Else, stores 0.
        private void vsgef64(int rs, int rt, int rd)
        {
            Vector256<double> vrs = vRegisters[rs].AsDouble();
            Vector256<double> vrt = vRegisters[rt].AsDouble();

            Vector256<double> result = Vector256.GreaterThanOrEqual(vrs, vrt);
            Vector256<double> mask = Vector256.Create(1UL).AsDouble();
            vRegisters[rd] = (result & mask).AsUInt64();
        }

        // Compares two lower 64-bit double precision floating point numbers in two vector registers
        // vrs and vrt. If number in vrs is greater than or equal to number in vrt, stores 1 in rd.
        // Else, stores 0.
        private void vsgef64_s(int rs, int rt, int rd)
        {
            Vector256<double> vrs = vRegisters[rs].AsDouble();
            Vector256<double> vrt = vRegisters[rt].AsDouble();
            registers[rd] = vrs.ToScalar() >= vrt.ToScalar() ? 1UL : 0UL;
        }

        // Compares 8 single precision floating point numbers componentwise in two vector registers
        // vrs and vrt. If component in vrs is greater than or equal to component in vrt, stores 1
        // in the component in vector register vrd. Else, stores 0.
        private void vsgef32(int rs, int rt, int rd)
        {
            Vector256<float> vrs = vRegisters[rs].AsSingle();
            Vector256<float> vrt = vRegisters[rt].AsSingle();

            Vector256<float> result = Vector256.GreaterThanOrEqual(vrs, vrt);
            Vector256<float> mask = Vector256.Create(1U).AsSingle();
            vRegisters[rd] = (result & mask).AsUInt64();
        }

        // Compares two lower 32-bit single precision floating point numbers in two vector registers
        // vrs and vrt. If number in vrs is greater than or equal to number in vrt, stores 1 in rd.
        // Else, stores 0.
        private void vsgef32_s(int rs, int rt, int rd)
        {
            Vector256<float> vrs = vRegisters[rs].AsSingle();
            Vector256<float> vrt = vRegisters[rt].AsSingle();
            registers[rd] = vrs.ToScalar() >= vrt.ToScalar() ? 1UL : 0UL;
        }

        // Compares 4 signed 64-bit integers componentwise in two vector registers vrs and vrt.
        // If component in vrs is less than component in vrt, stores 1 in the component in vector
        // register vrd. Else, stores 0.
        private void vslti64(int rs, int rt, int rd)
        {
            Vector256<long> vrs = vRegisters[rs].AsInt64();
            Vector256<long> vrt = vRegisters[rt].AsInt64();

            Vector256<long> result = Vector256.LessThan(vrs, vrt);
            Vector256<long> mask = Vector256.Create(1L);
            vRegisters[rd] = (result & mask).AsUInt64();
        }

        // Compares 4 unsigned 64-bit integers componentwise in two vector registers vrs and vrt.
        // If component in vrs is less than component in vrt, stores 1 in the component in vector
        // register vrd. Else, stores 0.
        private void vsltu64(int rs, int rt, int rd)
        {
            Vector256<ulong> vrs = vRegisters[rs];
            Vector256<ulong> vrt = vRegisters[rt];

            Vector256<ulong> result = Vector256.LessThan(vrs, vrt);
            Vector256<ulong> mask = Vector256.Create(1UL);
            vRegisters[rd] = (result & mask).AsUInt64();
        }

        // Compares 8 signed 32-bit integers componentwise in two vector registers vrs and vrt.
        // If component in vrs is less than component in vrt, stores 1 in the component in vector
        // register vrd. Else, stores 0.
        private void vslti32(int rs, int rt, int rd)
        {
            Vector256<int> vrs = vRegisters[rs].AsInt32();
            Vector256<int> vrt = vRegisters[rt].AsInt32();

            Vector256<int> result = Vector256.LessThan(vrs, vrt);
            Vector256<int> mask = Vector256.Create(1);
            vRegisters[rd] = (result & mask).AsUInt64();
        }

        // Compares 8 unsigned 32-bit integers componentwise in two vector registers vrs and vrt.
        // If component in vrs is less than component in vrt, stores 1 in the component in vector
        // register vrd. Else, stores 0.
        private void vsltu32(int rs, int rt, int rd)
        {
            Vector256<uint> vrs = vRegisters[rs].AsUInt32();
            Vector256<uint> vrt = vRegisters[rt].AsUInt32();

            Vector256<uint> result = Vector256.LessThan(vrs, vrt);
            Vector256<uint> mask = Vector256.Create(1U);
            vRegisters[rd] = (result & mask).AsUInt64();
        }

        // Compares 4 double precision floating point numbers componentwise in two vector registers
        // vrs and vrt. If component in vrs is less than component in vrt, stores 1 in the component
        // in vector register vrd. Else, stores 0.
        private void vsltf64(int rs, int rt, int rd)
        {
            Vector256<double> vrs = vRegisters[rs].AsDouble();
            Vector256<double> vrt = vRegisters[rt].AsDouble();

            Vector256<double> result = Vector256.LessThan(vrs, vrt);
            Vector256<double> mask = Vector256.Create(1UL).AsDouble();
            vRegisters[rd] = (result & mask).AsUInt64();
        }

        // Compares two lower 64-bit double precision floating point numbers in two vector registers vrs
        // and vrt. If number in vrs is less than number in vrt, stores 1 in rd. Else, stores 0.
        private void vsltf64_s(int rs, int rt, int rd)
        {
            Vector256<double> vrs = vRegisters[rs].AsDouble();
            Vector256<double> vrt = vRegisters[rt].AsDouble();
            registers[rd] = vrs.ToScalar() < vrt.ToScalar() ? 1UL : 0UL;
        }

        // Compares 8 single precision floating point numbers componentwise in two vector registers
        // vrs and vrt. If component in vrs is less than component in vrt, stores 1 in the component
        // in vector register vrd. Else, stores 0.
        private void vsltf32(int rs, int rt, int rd)
        {
            Vector256<float> vrs = vRegisters[rs].AsSingle();
            Vector256<float> vrt = vRegisters[rt].AsSingle();

            Vector256<float> result = Vector256.LessThan(vrs, vrt);
            Vector256<float> mask = Vector256.Create(1U).AsSingle();
            vRegisters[rd] = (result & mask).AsUInt64();
        }

        // Compares two lower 32-bit single precision floating point numbers in two vector registers vrs
        // and vrt. If number in vrs is less than number in vrt, stores 1 in rd. Else, stores 0.
        private void vsltf32_s(int rs, int rt, int rd)
        {
            Vector256<float> vrs = vRegisters[rs].AsSingle();
            Vector256<float> vrt = vRegisters[rt].AsSingle();
            registers[rd] = vrs.ToScalar() < vrt.ToScalar() ? 1UL : 0UL;
        }

        // Extracts 4 least significant bits from each 64-bit components in vector register vrs and
        // concatenate 4 bits then stores in rd.
        private void vextlsb_64(int rs, int rt, int rd)
        {
            Vector256<ulong> vrs = vRegisters[rs];

            Vector256<ulong> mask = Vector256.Create(1UL);
            Vector256<ulong> lsb = vrs & mask;

            ulong concat = 0UL;
            for (int i = 0; i < 4; i++)
            {
                concat |= (lsb[i] << i);
            }

            registers[rd] = concat;
        }

        // Extracts 8 least significant bits from each 32-bit components in vector register vrs
        // and concatenate 8 bits then stores in rd.
        private void vextlsb_32(int rs, int rt, int rd)
        {
            Vector256<uint> vrs = vRegisters[rs].AsUInt32();

            Vector256<uint> mask = Vector256.Create(1U);
            Vector256<uint> lsb = vrs & mask;

            ulong concat = 0UL;
            for (int i = 0; i < 8; i++)
            {
                concat |= (lsb[i] << i);
            }

            registers[rd] = concat;
        }

        // Extracts 4 most significant bits from each 64-bit components in vector register vrs and
        // concatenate 4 bits then stores in rd.
        private void vextmsb_64(int rs, int rt, int rd)
        {
            Vector256<ulong> vrs = vRegisters[rs];

            Vector256<ulong> mask = Vector256.Create(1UL);
            Vector256<ulong> msb = Vector256.ShiftRightLogical(vrs, 63) & mask;

            ulong concat = 0UL;
            for (int i = 0; i < 4; i++)
            {
                concat |= (msb[i] << i);
            }

            registers[rd] = concat;
        }

        // Extracts 8 most significant bits from each 32-bit components in vector register vrs and
        // concatenate 8 bits then stores in rd.
        private void vextmsb_32(int rs, int rt, int rd)
        {
            Vector256<uint> vrs = vRegisters[rs].AsUInt32();

            Vector256<uint> mask = Vector256.Create(1U);
            Vector256<uint> msb = Vector256.ShiftRightLogical(vrs, 31) & mask;

            ulong concat = 0UL;
            for (int i = 0; i < 8; i++)
            {
                concat |= (msb[i] << i);
            }

            registers[rd] = concat;
        }

        // Adds all 4 64-bit integers in vector register vrs and stores the result in rd.
        private void vsumi64(int rs, int rt, int rd)
        {
            Vector256<ulong> vrs = vRegisters[rs].AsUInt64();

            registers[rd] = vrs[0] + vrs[1] + vrs[2] + vrs[3];
        }

        // Adds all 8 32-bit integers in vector register vrs and stores the result in lower 32-bits in rd.
        private void vsumi32(int rs, int rt, int rd)
        {
            Vector256<int> vrs = vRegisters[rs].AsInt32();
            Vector256<int> sum = Avx2.HorizontalAdd(vrs, vrs);

            registers[rd] = (uint)(sum[0] + sum[1] + sum[4] + sum[5]);
        }

        // Adds all 4 double precision floating numbers in vector register vrs and stores the result in
        // lower 64-bits in vector register vrd.
        private void vsumf64(int rs, int rt, int rd)
        {
            Vector256<double> vrs = vRegisters[rs].AsDouble();
            Vector256<double> sum = Avx.HorizontalAdd(vrs, vrs);

            vRegisters[rd] = Vector256.CreateScalar(sum[0] + sum[2]).AsUInt64();
        }

        // Adds all 8 single precision floating numbers in vector register vrs and stores the result in
        // lower 32-bits in vector register vrd.
        private void vsumf32(int rs, int rt, int rd)
        {
            Vector256<float> vrs = vRegisters[rs].AsSingle();
            Vector256<float> sum = Avx.HorizontalAdd(vrs, vrs);

            vRegisters[rd] = Vector256.CreateScalar(sum[0] + sum[1] + sum[4] + sum[5]).AsUInt64();
        }

        // Adds all 4 32-bit integers in lower 128-bits in vector register vrs and stores the result in lower
        // 32-bits in rd.
        private void vsumi32_128(int rs, int rt, int rd)
        {
            Vector128<int> vrs = vRegisters[rs].GetLower().AsInt32();
            Vector128<int> sum = Ssse3.HorizontalAdd(vrs, vrs);

            registers[rd] = (uint)(sum[0] + sum[1]);
        }

        // Adds all 4 single precision floating numbers in lower 128-bits in vector register vrs and stores
        // the result in lower 32-bits in vector register vrd.
        private void vsumf32_128(int rs, int rt, int rd)
        {
            Vector128<float> vrs = vRegisters[rs].GetLower().AsSingle();
            Vector128<float> sum = Sse3.HorizontalAdd(vrs, vrs);

            vRegisters[rd] = Vector256.CreateScalar(sum[0] + sum[1]).AsUInt64();
        }

        // Copies 64-bit data in vector register vrs to vector register vrd at location specified in rt.
        // The rule is follow.
        // For i = 0 to 3:
        // j = i* 64
        // k = rt[i * 2 + 1..i * 2]
        // vrd[j + 63..j] = vrs[k * 64 + 63..k * 64]

        private void vshuffle_64(int rs, int rt, int rd)
        {
            vRegisters[rd] = Avx2.Permute4x64(vRegisters[rs], (byte)registers[rt]);
        }

        // Copies 32-bit data in vector register vrs to vector register vrd at location specified in rt.
        // The rule is follow.
        // For i = 0 to 7:
        // j = i* 32
        // k = rt[i * 3 + 2..i * 3]
        // vrd[j + 31..j] = vrs[k * 32 + 31..k * 32]
        private void vshuffle_32(int rs, int rt, int rd)
        {
            Vector256<uint> vrs = vRegisters[rs].AsUInt32();
            ulong flag = registers[rt];

            Span<uint> shuffle = stackalloc uint[8];

            for (int i = 0; i < 8; i++)
            {
                shuffle[i] = vrs[(int)((flag >> (3 * i)) & 0b111U)];
            }

            ReadOnlySpan<uint> sh = shuffle;

            vRegisters[rd] = Vector256.Create(sh).AsUInt64();
        }
    }
}
