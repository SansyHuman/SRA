using System.Net;
using System.Text;

namespace SRA_Assembler
{
    public enum Segment
    {
        Data, Text, KData, KText, KTextStart, Uninitialized
    }

    public enum SymbolType
    {
        Data, Text, KData, KText
    }

    public enum SymbolScope
    {
        Internal, Global
    }

    public enum RelocationType
    {
        IFormatImm, JFormatAddr, EIFormatImm,

        // address of LA instruction.
        LAAddress,

        // For kernel section instructions
        KIFormatImm, KJFormatAddr, KEIFormatImm,
        KLAAddress,
    }

    public struct SymbolTableElement
    {
        public string Label;
        public SymbolType Type;
        public SymbolScope Scope;
        public ulong Position;

        public SymbolTableElement(string label, SymbolType type, SymbolScope scope, ulong position)
        {
            Label = label;
            Type = type;
            Scope = scope;
            Position = position;
        }
    }

    public struct RelocationTableElement
    {
        public ulong Position;
        public string Label;
        public RelocationType Type;
        public SymbolScope LabelSearchLocation;

        public RelocationTableElement(ulong position, string label, RelocationType type, SymbolScope labelSearchLocation)
        {
            Position = position;
            Label = label;
            Type = type;
            LabelSearchLocation = labelSearchLocation;
        }
    }

    public static class Assembler
    {
        public const ulong TEXT_START = 0x40_0000U;
        public const ulong DATA_START = 0x10_0000_0000U;
        public const ulong KTEXT_START = 0x80_0000_0000U;
        public const ulong KDATA_START = 0x1000_0000_0000U;
        public const ulong MIN_STATIC_SIZE = 0x1_0000U;

        static int FindLabelEndIndex(string code)
        {
            int labelEndIndex = code.IndexOf(':');
            if (labelEndIndex == -1)
                return -1;

            // check if the label end is in the string literal
            int openQuotationIndex = code.IndexOf('\"');
            int closeQuotationIndex = code.LastIndexOf('\"');
            if (labelEndIndex > openQuotationIndex && labelEndIndex < closeQuotationIndex)
            {
                return -1;
            }

            return labelEndIndex;
        }

        static string MakeError(int line, string code)
        {
            return $"Error in line {line} ({code})";
        }

        // returns the position of the new data that satisfies alignment and moves
        // the stream position to the boundary.
        static long MoveToByteBoundary(MemoryStream mem, int dataSize, int alignment)
        {
            if (alignment == -1)
            {
                alignment = dataSize;
            }

            long currPos = mem.Position;
            if (currPos % alignment == 0)
            {
                return currPos;
            }

            currPos = alignment * (currPos / alignment + 1);
            mem.Position = currPos;

            return currPos;
        }

        static ulong LiteralToDword(string literal, int line, string originCode)
        {
            try
            {
                ulong value = 0U;

                string radix = string.Empty;
                if (literal.Length >= 2)
                {
                    radix = literal.Substring(0, 2).ToLower();
                }

                if (radix == "0x")
                {
                    value = Convert.ToUInt64(literal.Substring(2), 16);
                }
                else if (radix == "0b")
                {
                    value = Convert.ToUInt64(literal.Substring(2), 2);
                }
                else if (radix != string.Empty && radix[0] == '0')
                {
                    value = Convert.ToUInt64(literal.Substring(1), 8);
                }
                else
                {
                    if (literal[0] == '-')
                    {
                        long valuei = Convert.ToInt64(literal);
                        value = (ulong)valuei;
                    }
                    else
                    {
                        value = Convert.ToUInt64(literal);
                    }
                }

                return value;
            }
            catch (Exception e)
            {
                throw new Exception($"{MakeError(line, originCode)}: {e.Message}", e);
            }
        }

        static uint LiteralToWord(string literal, int line, string originCode)
        {
            try
            {
                uint value = 0U;

                string radix = string.Empty;
                if (literal.Length >= 2)
                {
                    radix = literal.Substring(0, 2).ToLower();
                }

                if (radix == "0x")
                {
                    value = Convert.ToUInt32(literal.Substring(2), 16);
                }
                else if (radix == "0b")
                {
                    value = Convert.ToUInt32(literal.Substring(2), 2);
                }
                else if (radix != string.Empty && radix[0] == '0')
                {
                    value = Convert.ToUInt32(literal.Substring(1), 8);
                }
                else
                {
                    if (literal[0] == '-')
                    {
                        int valuei = Convert.ToInt32(literal);
                        value = (uint)valuei;
                    }
                    else
                    {
                        value = Convert.ToUInt32(literal);
                    }
                }

                return value;
            }
            catch (Exception e)
            {
                throw new Exception($"{MakeError(line, originCode)}: {e.Message}", e);
            }
        }

        static ushort LiteralToHalf(string literal, int line, string originCode)
        {
            try
            {
                ushort value = (ushort)0U;

                string radix = string.Empty;
                if (literal.Length >= 2)
                {
                    radix = literal.Substring(0, 2).ToLower();
                }

                if (radix == "0x")
                {
                    value = Convert.ToUInt16(literal.Substring(2), 16);
                }
                else if (radix == "0b")
                {
                    value = Convert.ToUInt16(literal.Substring(2), 2);
                }
                else if (radix != string.Empty && radix[0] == '0')
                {
                    value = Convert.ToUInt16(literal.Substring(1), 8);
                }
                else
                {
                    if (literal[0] == '-')
                    {
                        short valuei = Convert.ToInt16(literal);
                        value = (ushort)valuei;
                    }
                    else
                    {
                        value = Convert.ToUInt16(literal);
                    }
                }

                return value;
            }
            catch (Exception e)
            {
                throw new Exception($"{MakeError(line, originCode)}: {e.Message}", e);
            }
        }

        static byte LiteralToByte(string literal, int line, string originCode)
        {
            try
            {
                byte value = (byte)0U;

                string radix = string.Empty;
                if (literal.Length >= 2)
                {
                    radix = literal.Substring(0, 2).ToLower();
                }

                if (radix == "0x")
                {
                    value = Convert.ToByte(literal.Substring(2), 16);
                }
                else if (radix == "0b")
                {
                    value = Convert.ToByte(literal.Substring(2), 2);
                }
                else if (radix != string.Empty && radix[0] == '0')
                {
                    value = Convert.ToByte(literal.Substring(1), 8);
                }
                else
                {
                    if (literal[0] == '-')
                    {
                        sbyte valuei = Convert.ToSByte(literal);
                        value = (byte)valuei;
                    }
                    else
                    {
                        value = Convert.ToByte(literal);
                    }
                }

                return value;
            }
            catch (Exception e)
            {
                throw new Exception($"{MakeError(line, originCode)}: {e.Message}", e);
            }
        }

        static float LiteralToFloat(string literal, int line, string originCode)
        {
            try
            {
                return Convert.ToSingle(literal);
            }
            catch (Exception e)
            {
                throw new Exception($"{MakeError(line, originCode)}: {e.Message}", e);
            }
        }

        static double LiteralToDouble(string literal, int line, string originCode)
        {
            try
            {
                return Convert.ToDouble(literal);
            }
            catch (Exception e)
            {
                throw new Exception($"{MakeError(line, originCode)}: {e.Message}", e);
            }
        }

        static byte[] LiteralToString(string literal, Encoding encoder, int line, string originCode)
        {
            if (literal[0] != '\"' || literal[literal.Length - 1] != '\"' || literal.Length <= 1)
            {
                throw new Exception($"{MakeError(line, originCode)}: wrong string literal form");
            }

            if (literal.Length == 2)
            {
                throw new Exception($"{MakeError(line, originCode)}: empty string");
            }

            try
            {
                literal = literal.Substring(1, literal.Length - 2);
                return encoder.GetBytes(literal);
            }
            catch (Exception e)
            {
                throw new Exception($"{MakeError(line, originCode)}: {e.Message}", e);
            }
        }

        static (InstructionSyntax[] insts, string label) ProcessLA(string code)
        {
            if (code.Length < 2)
            {
                throw new Exception("Unknown instruction");
            }
            if (code.Substring(0, 2).ToLower() != "la")
            {
                throw new Exception("Unknown instruction");
            }

            string[] args = code.Substring(3).Split(',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (args.Length < 2)
            {
                throw new Exception("Insturction LA has less arguments than needed");
            }
            if (args.Length > 2)
            {
                throw new Exception("Insturction LA has more arguments than needed");
            }

            string rd = InstructionSyntax.GetRegNumber(args[0]);
            string label = args[1];
            if (!InstructionSyntax.IsLabel(label))
            {
                throw new Exception($"Wrong label {label}");
            }

            InstructionSyntax[] insts = new InstructionSyntax[4];

            insts[0] = new InstructionSyntax($"li.0 %{rd}, 0");
            insts[1] = new InstructionSyntax($"li.1 %{rd}, 0");
            insts[2] = new InstructionSyntax($"li.2 %{rd}, 0");
            insts[3] = new InstructionSyntax($"li.3 %{rd}, 0");

            return (insts, label);
        }

        static InstructionSyntax[] ProcessPseudoInst(string code)
        {
            string[] tmp = code.Split(InstructionSyntax.whitespace,
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            string opcode = tmp[0].ToLower();

            InstructionSyntax[] insts = null;

            if (opcode == "not")
            {
                string[] regs = code.Substring(opcode.Length).Trim()
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (regs.Length < 2)
                {
                    throw new Exception($"Instruction {opcode} does not have enough registers");
                }
                if (regs.Length > 2)
                {
                    throw new Exception($"Instruction {opcode} have more registers than needed");
                }

                string rt = InstructionSyntax.GetRegNumber(regs[0]);
                string rs = InstructionSyntax.GetRegNumber(regs[1]);

                insts = new InstructionSyntax[2];
                insts[0] = new InstructionSyntax("addi %asm, %zero, -1");
                insts[1] = new InstructionSyntax($"xor %{rt}, %asm, %{rs}");
            }
            else if (opcode == "neg")
            {
                string[] regs = code.Substring(opcode.Length).Trim()
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (regs.Length < 2)
                {
                    throw new Exception($"Instruction {opcode} does not have enough registers");
                }
                if (regs.Length > 2)
                {
                    throw new Exception($"Instruction {opcode} have more registers than needed");
                }

                string rt = InstructionSyntax.GetRegNumber(regs[0]);
                string rs = InstructionSyntax.GetRegNumber(regs[1]);

                insts = new InstructionSyntax[1];
                insts[0] = new InstructionSyntax($"sub %{rt}, %zero, %{rs}");
            }
            else if (opcode == "negw")
            {
                string[] regs = code.Substring(opcode.Length).Trim()
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (regs.Length < 2)
                {
                    throw new Exception($"Instruction {opcode} does not have enough registers");
                }
                if (regs.Length > 2)
                {
                    throw new Exception($"Instruction {opcode} have more registers than needed");
                }

                string rt = InstructionSyntax.GetRegNumber(regs[0]);
                string rs = InstructionSyntax.GetRegNumber(regs[1]);

                insts = new InstructionSyntax[1];
                insts[0] = new InstructionSyntax($"subw %{rt}, %zero, %{rs}");
            }
            else if (opcode == "mov")
            {
                string[] regs = code.Substring(opcode.Length).Trim()
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (regs.Length < 2)
                {
                    throw new Exception($"Instruction {opcode} does not have enough registers");
                }
                if (regs.Length > 2)
                {
                    throw new Exception($"Instruction {opcode} have more registers than needed");
                }

                string rt = InstructionSyntax.GetRegNumber(regs[0]);
                string rs = InstructionSyntax.GetRegNumber(regs[1]);

                insts = new InstructionSyntax[1];
                insts[0] = new InstructionSyntax($"addi %{rt}, %{rs}, 0");
            }
            else if (opcode == "mov")
            {
                string[] regs = code.Substring(opcode.Length).Trim()
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (regs.Length < 2)
                {
                    throw new Exception($"Instruction {opcode} does not have enough registers");
                }
                if (regs.Length > 2)
                {
                    throw new Exception($"Instruction {opcode} have more registers than needed");
                }

                string rt = InstructionSyntax.GetRegNumber(regs[0]);
                string rs = InstructionSyntax.GetRegNumber(regs[1]);

                insts = new InstructionSyntax[1];
                insts[0] = new InstructionSyntax($"addi %{rt}, %{rs}, 0");
            }
            else if (opcode == "movw")
            {
                string[] regs = code.Substring(opcode.Length).Trim()
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (regs.Length < 2)
                {
                    throw new Exception($"Instruction {opcode} does not have enough registers");
                }
                if (regs.Length > 2)
                {
                    throw new Exception($"Instruction {opcode} have more registers than needed");
                }

                string rt = InstructionSyntax.GetRegNumber(regs[0]);
                string rs = InstructionSyntax.GetRegNumber(regs[1]);

                insts = new InstructionSyntax[1];
                insts[0] = new InstructionSyntax($"addiw %{rt}, %{rs}, 0");
            }
            else if (opcode == "push")
            {
                string[] regs = code.Substring(opcode.Length).Trim()
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (regs.Length < 1)
                {
                    throw new Exception($"Instruction {opcode} does not have enough registers");
                }
                if (regs.Length > 1)
                {
                    throw new Exception($"Instruction {opcode} have more registers than needed");
                }

                string rs = InstructionSyntax.GetRegNumber(regs[0]);

                insts = new InstructionSyntax[2];
                insts[0] = new InstructionSyntax($"addi %sp, %sp, -8");
                insts[1] = new InstructionSyntax($"sdi %{rs}, [%sp]0");
            }
            else if (opcode == "pop")
            {
                string[] regs = code.Substring(opcode.Length).Trim()
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (regs.Length < 1)
                {
                    throw new Exception($"Instruction {opcode} does not have enough registers");
                }
                if (regs.Length > 1)
                {
                    throw new Exception($"Instruction {opcode} have more registers than needed");
                }

                string rd = InstructionSyntax.GetRegNumber(regs[0]);

                insts = new InstructionSyntax[2];
                insts[0] = new InstructionSyntax($"ldi %{rd}, [%sp]0");
                insts[1] = new InstructionSyntax($"addi %sp, %sp, 8");
            }
            else if (opcode == "ret")
            {
                string[] regs = code.Substring(opcode.Length).Trim()
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (regs.Length != 0)
                {
                    throw new Exception($"Instruction {opcode} have more registers than needed");
                }

                insts = new InstructionSyntax[1];
                insts[0] = new InstructionSyntax($"jr %ra");
            }
            else
            {
                throw new Exception($"Unknown instruction {opcode}");
            }

            return insts;
        }

        public static void AssembleObj(string inputFile, string outputFile)
        {
            Dictionary<string, SymbolTableElement> symbolTable = new Dictionary<string, SymbolTableElement>();
            List<RelocationTableElement> relocationTable = new List<RelocationTableElement>();

            string entrypoint = string.Empty;
            bool hasEntryPoint = false;

            byte[] dataBin = null;
            byte[] textBin = null;
            
            byte[] kdataBin = null;
            byte[] ktextBin = null;
            byte[] ktextStartBin = null;

            ulong dataSize = 0U;
            ulong textSize = 0U;

            ulong kdataSize = 0U;
            ulong ktextSize = 0U;
            ulong ktextStartSize = 0U;
            uint ktextType = ProgramHeader.PT_KLOAD;
            ulong ktextStartAddr = KTEXT_START;

            using (StreamReader fread = new StreamReader(inputFile))
            {
                using (MemoryStream data = new MemoryStream(), text = new MemoryStream(),
                    ktextStart = new MemoryStream(), ktextOther = new MemoryStream(),
                    kdata = new MemoryStream())
                {
                    using (BinaryWriter dwrite = new BinaryWriter(data), twrite = new BinaryWriter(text),
                        ktswrite = new BinaryWriter(ktextStart), ktowrite = new BinaryWriter(ktextOther),
                        kdwrite = new BinaryWriter(kdata))
                    {
                        string currLabel = string.Empty;
                        bool useLabel = false;
                        bool isGlobal = false;

                        Segment currSegment = Segment.Uninitialized;
                        int currAlignment = -1;

                        // used in .repeat
                        int prevDataSize = -1;
                        ulong prevData = 0U;

                        for (int line = 1; true; line++)
                        {
                            string fline = fread.ReadLine();
                            if (fline == null) // EOF
                            {
                                break;
                            }

                            string flineOriginal = new string(fline);

                            // remove comment
                            int commentIndex = fline.IndexOf("//");
                            if (commentIndex >= 0)
                            {
                                fline = fline.Substring(0, commentIndex);
                            }
                            fline = fline.Trim();

                            if (fline == string.Empty) // no codes
                            {
                                continue;
                            }

                            // fine label
                            int labelEndIndex = FindLabelEndIndex(fline);
                            if (labelEndIndex >= 0)
                            {
                                if (useLabel) // label already exists
                                {
                                    throw new Exception($"{MakeError(line, flineOriginal)}: two consecutive labels");
                                }

                                if (labelEndIndex == 0)
                                {
                                    throw new Exception($"{MakeError(line, flineOriginal)}: no label name");
                                }

                                string[] label = fline.Substring(0, labelEndIndex)
                                    .Split(InstructionSyntax.whitespace,
                                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                                    );

                                if (label.Length > 2)
                                {
                                    throw new Exception($"{MakeError(line, flineOriginal)}: label cannot have whitespace");
                                }

                                if (label.Length == 2)
                                {
                                    if (label[0] != ".global")
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: label can only have prefix \'.global\'");
                                    }
                                    isGlobal = true;

                                    label[0] = label[1];
                                }

                                if (!InstructionSyntax.IsLabel(label[0]))
                                {
                                    throw new Exception($"{MakeError(line, flineOriginal)}: wrong label form");
                                }

                                currLabel = label[0];
                                useLabel = true;

                                if (symbolTable.ContainsKey(currLabel))
                                {
                                    throw new Exception($"{MakeError(line, flineOriginal)}: duplicated label");
                                }
                            }


                            if (fline.Length - 1 == labelEndIndex) // label only
                            {
                                continue;
                            }

                            // process code
                            string code = fline.Substring(labelEndIndex + 1).Trim();

                            if (currSegment == Segment.Uninitialized)
                            {
                                if (code == ".data")
                                {
                                    currSegment = Segment.Data;
                                    currAlignment = -1;
                                }
                                else if (code == ".text")
                                {
                                    currSegment = Segment.Text;
                                }
                                else if (code == ".kdata")
                                {
                                    currSegment = Segment.KData;
                                    currAlignment = -1;
                                }
                                else if (code.StartsWith(".ktext"))
                                {
                                    currSegment = Segment.KText;
                                    if (code != ".ktext") // with address
                                    {
                                        string address = code.Substring(6).Trim();
                                        if (!address.StartsWith("0x") && !address.StartsWith("0X"))
                                        {
                                            throw new Exception($"{MakeError(line, flineOriginal)}: address of .ktext should be hex");
                                        }

                                        ktextType = ProgramHeader.PT_KLOADSTART;
                                        ktextStartAddr = LiteralToDword(address, line, flineOriginal);
                                        currSegment = Segment.KTextStart;

                                        if (ktextStartAddr % 4 != 0)
                                        {
                                            throw new Exception($"{MakeError(line, flineOriginal)}: address of .ktext should be multiple of 4");
                                        }
                                    }
                                }
                                else
                                {
                                    throw new Exception($"{MakeError(line, flineOriginal)}: segment specifier should exist before any code");
                                }

                                if (useLabel)
                                {
                                    throw new Exception($"{MakeError(line, flineOriginal)}: no actual code after label");
                                }
                            }
                            else if (currSegment == Segment.Data || currSegment == Segment.KData)
                            {
                                string[] args = code.Split(
                                    InstructionSyntax.whitespace,
                                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                                    );

                                MemoryStream dataStream = currSegment == Segment.Data ? data : kdata;
                                BinaryWriter dataWriter = currSegment == Segment.Data ? dwrite : kdwrite;

                                if (args[0] == ".data")
                                {
                                    if (args.Length != 1)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: .data should be used alone");
                                    }

                                    currSegment = Segment.Data;
                                    currAlignment = -1;

                                    if (useLabel)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no actual code after label");
                                    }
                                }
                                else if (args[0] == ".text")
                                {
                                    if (args.Length != 1)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: .text should be used alone");
                                    }

                                    currSegment = Segment.Text;

                                    if (useLabel)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no actual code after label");
                                    }
                                }
                                else if (args[0] == ".kdata")
                                {
                                    if (args.Length != 1)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: .kdata should be used alone");
                                    }

                                    currSegment = Segment.KData;
                                    currAlignment = -1;

                                    if (useLabel)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no actual code after label");
                                    }
                                }
                                else if (args[0] == ".ktext")
                                {
                                    if (args.Length == 1)
                                    {
                                        currSegment = Segment.KText;
                                    }
                                    else if (args.Length == 2)
                                    {
                                        if (ktextType == ProgramHeader.PT_KLOADSTART)
                                        {
                                            throw new Exception($"{MakeError(line, flineOriginal)}: there should be only one .ktext with address");
                                        }

                                        if (!args[1].StartsWith("0x") && !args[1].StartsWith("0X"))
                                        {
                                            throw new Exception($"{MakeError(line, flineOriginal)}: address of .ktext should be hex");
                                        }

                                        ktextType = ProgramHeader.PT_KLOADSTART;
                                        ktextStartAddr = LiteralToDword(args[1], line, flineOriginal);
                                        currSegment = Segment.KTextStart;


                                        if (ktextStartAddr % 4 != 0)
                                        {
                                            throw new Exception($"{MakeError(line, flineOriginal)}: address of .ktext should be multiple of 4");
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: .ktext has more arguments than needed");
                                    }
                                }
                                else if (args[0] == ".dword")
                                {
                                    if (args.Length < 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no literal after .dword");
                                    }
                                    if (args.Length > 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: unnecessary arguments after .dword");
                                    }

                                    ulong value = LiteralToDword(args[1], line, flineOriginal);
                                    long pos = MoveToByteBoundary(dataStream, 8, currAlignment);
                                    dataWriter.Write(value);

                                    prevDataSize = 8;
                                    prevData = value;

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            currSegment == Segment.Data ? SymbolType.Data : SymbolType.KData,
                                            isGlobal ? SymbolScope.Global : SymbolScope.Internal,
                                            (ulong)pos);

                                        useLabel = false;
                                        isGlobal = false;
                                    }
                                }
                                else if (args[0] == ".word")
                                {
                                    if (args.Length < 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no literal after .word");
                                    }
                                    if (args.Length > 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: unnecessary arguments after .word");
                                    }

                                    uint value = LiteralToWord(args[1], line, flineOriginal);
                                    long pos = MoveToByteBoundary(dataStream, 4, currAlignment);
                                    dataWriter.Write(value);

                                    prevDataSize = 4;
                                    prevData = value;

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            currSegment == Segment.Data ? SymbolType.Data : SymbolType.KData,
                                            isGlobal ? SymbolScope.Global : SymbolScope.Internal,
                                            (ulong)pos);

                                        useLabel = false;
                                        isGlobal = false;
                                    }
                                }
                                else if (args[0] == ".half")
                                {
                                    if (args.Length < 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no literal after .half");
                                    }
                                    if (args.Length > 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: unnecessary arguments after .half");
                                    }

                                    ushort value = LiteralToHalf(args[1], line, flineOriginal);
                                    long pos = MoveToByteBoundary(dataStream, 2, currAlignment);
                                    dataWriter.Write(value);

                                    prevDataSize = 2;
                                    prevData = value;

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            currSegment == Segment.Data ? SymbolType.Data : SymbolType.KData,
                                            isGlobal ? SymbolScope.Global : SymbolScope.Internal,
                                            (ulong)pos);

                                        useLabel = false;
                                        isGlobal = false;
                                    }
                                }
                                else if (args[0] == ".byte")
                                {
                                    if (args.Length < 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no literal after .byte");
                                    }
                                    if (args.Length > 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: unnecessary arguments after .byte");
                                    }

                                    byte value = LiteralToByte(args[1], line, flineOriginal);
                                    long pos = MoveToByteBoundary(dataStream, 1, currAlignment);
                                    dataWriter.Write(value);

                                    prevDataSize = 1;
                                    prevData = value;

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            currSegment == Segment.Data ? SymbolType.Data : SymbolType.KData,
                                            isGlobal ? SymbolScope.Global : SymbolScope.Internal,
                                            (ulong)pos);

                                        useLabel = false;
                                        isGlobal = false;
                                    }
                                }
                                else if (args[0] == ".float")
                                {
                                    if (args.Length < 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no literal after .float");
                                    }
                                    if (args.Length > 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: unnecessary arguments after .float");
                                    }

                                    float value = LiteralToFloat(args[1], line, flineOriginal);
                                    long pos = MoveToByteBoundary(dataStream, 4, currAlignment);
                                    dataWriter.Write(value);

                                    prevDataSize = 4;
                                    unsafe
                                    {
                                        prevData = *((uint*)&value);
                                    }

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            currSegment == Segment.Data ? SymbolType.Data : SymbolType.KData,
                                            isGlobal ? SymbolScope.Global : SymbolScope.Internal,
                                            (ulong)pos);

                                        useLabel = false;
                                        isGlobal = false;
                                    }
                                }
                                else if (args[0] == ".double")
                                {
                                    if (args.Length < 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no literal after .double");
                                    }
                                    if (args.Length > 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: unnecessary arguments after .double");
                                    }

                                    double value = LiteralToDouble(args[1], line, flineOriginal);
                                    long pos = MoveToByteBoundary(dataStream, 8, currAlignment);
                                    dataWriter.Write(value);

                                    prevDataSize = 8;
                                    unsafe
                                    {
                                        prevData = *((ulong*)&value);
                                    }

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            currSegment == Segment.Data ? SymbolType.Data : SymbolType.KData,
                                            isGlobal ? SymbolScope.Global : SymbolScope.Internal,
                                            (ulong)pos);

                                        useLabel = false;
                                        isGlobal = false;
                                    }
                                }
                                else if (args[0] == ".ascii")
                                {
                                    if (args.Length < 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no literal after .ascii");
                                    }

                                    string strLiteral = code.Substring(6).Trim();
                                    byte[] encode = LiteralToString(strLiteral, Encoding.ASCII, line, flineOriginal);

                                    long pos = MoveToByteBoundary(dataStream, 1, currAlignment);
                                    dataWriter.Write(encode);

                                    prevDataSize = -1; // string literal does not support .repeat

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            currSegment == Segment.Data ? SymbolType.Data : SymbolType.KData,
                                            isGlobal ? SymbolScope.Global : SymbolScope.Internal,
                                            (ulong)pos);

                                        useLabel = false;
                                        isGlobal = false;
                                    }
                                }
                                else if (args[0] == ".asciiz")
                                {
                                    if (args.Length < 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no literal after .asciiz");
                                    }

                                    string strLiteral = code.Substring(7).Trim();
                                    byte[] encode = LiteralToString(strLiteral, Encoding.ASCII, line, flineOriginal);

                                    long pos = MoveToByteBoundary(dataStream, 1, currAlignment);
                                    dataWriter.Write(encode);
                                    dataWriter.Write((byte)0U); // null-terminated

                                    prevDataSize = -1;

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            currSegment == Segment.Data ? SymbolType.Data : SymbolType.KData,
                                            isGlobal ? SymbolScope.Global : SymbolScope.Internal,
                                            (ulong)pos);

                                        useLabel = false;
                                        isGlobal = false;
                                    }
                                }
                                else if (args[0] == ".unicode")
                                {
                                    if (args.Length < 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no literal after .unicode");
                                    }

                                    string strLiteral = code.Substring(8).Trim();
                                    byte[] encode = LiteralToString(strLiteral, Encoding.UTF8, line, flineOriginal);

                                    long pos = MoveToByteBoundary(dataStream, 8, currAlignment);
                                    dataWriter.Write((ulong)encode.Length); // store the byte length of the string
                                    dataWriter.Write(encode);

                                    prevDataSize = -1;

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            currSegment == Segment.Data ? SymbolType.Data : SymbolType.KData,
                                            isGlobal ? SymbolScope.Global : SymbolScope.Internal,
                                            (ulong)pos);

                                        useLabel = false;
                                        isGlobal = false;
                                    }
                                }
                                else if (args[0] == ".space")
                                {
                                    if (args.Length < 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no literal after .space");
                                    }
                                    if (args.Length > 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: unnecessary arguments after .space");
                                    }

                                    long cnt = (long)LiteralToDword(args[1], line, flineOriginal);
                                    if (cnt < 0)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: argument after .space is too large");
                                    }
                                    long pos = dataStream.Position;
                                    dataWriter.Write(new byte[cnt]);

                                    prevDataSize = -1;

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            currSegment == Segment.Data ? SymbolType.Data : SymbolType.KData,
                                            isGlobal ? SymbolScope.Global : SymbolScope.Internal,
                                            (ulong)pos);

                                        useLabel = false;
                                        isGlobal = false;
                                    }
                                }
                                else if (args[0] == ".repeat")
                                {
                                    if (args.Length < 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no literal after .repeat");
                                    }
                                    if (args.Length > 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: unnecessary arguments after .repeat");
                                    }

                                    if (prevDataSize < 0) // No supported previous data
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no supported data before .repeat");
                                    }

                                    ulong cnt = LiteralToDword(args[1], line, flineOriginal);
                                    long pos = MoveToByteBoundary(data, prevDataSize, currAlignment);

                                    for (ulong i = 0; i < cnt; i++)
                                    {
                                        MoveToByteBoundary(dataStream, prevDataSize, currAlignment);
                                        switch (prevDataSize)
                                        {
                                            case 1:
                                                dataWriter.Write((byte)prevData);
                                                break;
                                            case 2:
                                                dataWriter.Write((ushort)prevData);
                                                break;
                                            case 4:
                                                dataWriter.Write((uint)prevData);
                                                break;
                                            case 8:
                                                dataWriter.Write(prevData);
                                                break;
                                            default:
                                                throw new Exception("Unknown error");
                                        }
                                    }

                                    prevDataSize = -1;

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            currSegment == Segment.Data ? SymbolType.Data : SymbolType.KData,
                                            isGlobal ? SymbolScope.Global : SymbolScope.Internal,
                                            (ulong)pos);

                                        useLabel = false;
                                        isGlobal = false;
                                    }
                                }
                                else if (args[0] == ".align")
                                {
                                    if (args.Length < 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no literal after .repeat");
                                    }
                                    if (args.Length > 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: unnecessary arguments after .repeat");
                                    }

                                    switch (args[1])
                                    {
                                        case "1":
                                            currAlignment = 1;
                                            break;
                                        case "2":
                                            currAlignment = 2;
                                            break;
                                        case "4":
                                            currAlignment = 4;
                                            break;
                                        case "8":
                                            currAlignment = 8;
                                            break;
                                        case "16":
                                            currAlignment = 16;
                                            break;
                                        case "32":
                                            currAlignment = 32;
                                            break;
                                        case "64":
                                            currAlignment = 64;
                                            break;
                                        case "128":
                                            currAlignment = 128;
                                            break;
                                        case "256":
                                            currAlignment = 256;
                                            break;
                                        default:
                                            throw new Exception($"{MakeError(line, flineOriginal)}: unsupported alignment size");
                                    }

                                    if (useLabel)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no actual code after label");
                                    }
                                }
                                else
                                {
                                    throw new Exception($"{MakeError(line, flineOriginal)}: unknown data section instruction");
                                }
                            }
                            else if (currSegment == Segment.Text || currSegment == Segment.KText || currSegment == Segment.KTextStart)
                            {
                                MemoryStream textStream;
                                BinaryWriter textWriter;
                                if (currSegment == Segment.Text)
                                {
                                    textStream = text;
                                    textWriter = twrite;
                                }
                                else if (currSegment == Segment.KText)
                                {
                                    textStream = ktextOther;
                                    textWriter = ktowrite;
                                }
                                else // currSegment == Segment.KTextStart
                                {
                                    textStream = ktextStart;
                                    textWriter = ktswrite;
                                }

                                string[] args = code.Split(
                                    InstructionSyntax.whitespace,
                                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                                    );

                                if (args[0] == ".data")
                                {
                                    if (args.Length != 1)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: .data should be used alone");
                                    }

                                    currSegment = Segment.Data;
                                    currAlignment = -1;

                                    if (useLabel)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no actual code after label");
                                    }
                                }
                                else if (args[0] == ".text")
                                {
                                    if (args.Length != 1)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: .text should be used alone");
                                    }

                                    currSegment = Segment.Text;

                                    if (useLabel)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no actual code after label");
                                    }
                                }
                                else if (args[0] == ".kdata")
                                {
                                    if (args.Length != 1)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: .kdata should be used alone");
                                    }

                                    currSegment = Segment.KData;
                                    currAlignment = -1;

                                    if (useLabel)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no actual code after label");
                                    }
                                }
                                else if (args[0] == ".ktext")
                                {
                                    if (args.Length == 1)
                                    {
                                        currSegment = Segment.KText;
                                    }
                                    else if (args.Length == 2)
                                    {
                                        if (ktextType == ProgramHeader.PT_KLOADSTART)
                                        {
                                            throw new Exception($"{MakeError(line, flineOriginal)}: there should be only one .ktext with address");
                                        }

                                        if (!args[1].StartsWith("0x") && !args[1].StartsWith("0X"))
                                        {
                                            throw new Exception($"{MakeError(line, flineOriginal)}: address of .ktext should be hex");
                                        }

                                        ktextType = ProgramHeader.PT_KLOADSTART;
                                        ktextStartAddr = LiteralToDword(args[1], line, flineOriginal);
                                        currSegment = Segment.KTextStart;


                                        if (ktextStartAddr % 4 != 0)
                                        {
                                            throw new Exception($"{MakeError(line, flineOriginal)}: address of .ktext should be multiple of 4");
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: .ktext has more arguments than needed");
                                    }
                                }
                                else if (args[0] == ".entry")
                                {
                                    if (currSegment != Segment.Text)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: .ktext segment cannot have entrypoint");
                                    }
                                    if (args.Length < 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no label after .entry");
                                    }
                                    if (args.Length > 2)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: unnecessary arguments after .entry");
                                    }

                                    if (hasEntryPoint)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: multiple entry points");
                                    }

                                    entrypoint = args[1];
                                    hasEntryPoint = true;

                                    if (useLabel)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: no actual code after label");
                                    }
                                }
                                else
                                {
                                    bool useExternLabel = false;
                                    if (args[0] == ".extern")
                                    {
                                        if (args.Length == 1)
                                        {
                                            throw new Exception($"{MakeError(line, flineOriginal)}: no actual code after .extern");
                                        }

                                        useExternLabel = true;
                                        code = code.Substring(7).Trim();
                                    }

                                    try
                                    {
                                        InstructionSyntax syntax = new InstructionSyntax(code);
                                        long pos = textStream.Position;
                                        if (pos % 4 != 0)
                                        {
                                            throw new Exception("Unknown error in text alignment");
                                        }

                                        switch (syntax.Format)
                                        {
                                            case InstructionFormat.R:
                                                break;
                                            case InstructionFormat.I:
                                                if (syntax.Imm.Length > 0 && InstructionSyntax.IsLabel(syntax.Imm))
                                                {
                                                    relocationTable.Add(new RelocationTableElement(
                                                        (ulong)pos,
                                                        syntax.Imm,
                                                        currSegment == Segment.Text ? RelocationType.IFormatImm : RelocationType.KIFormatImm,
                                                        useExternLabel ? SymbolScope.Global : SymbolScope.Internal));
                                                }
                                                break;
                                            case InstructionFormat.J:
                                                if (syntax.Addr.Length > 0 && InstructionSyntax.IsLabel(syntax.Addr))
                                                {
                                                    relocationTable.Add(new RelocationTableElement(
                                                        (ulong)pos,
                                                        syntax.Addr,
                                                        currSegment == Segment.Text ? RelocationType.JFormatAddr : RelocationType.KJFormatAddr,
                                                        useExternLabel ? SymbolScope.Global : SymbolScope.Internal));
                                                }
                                                break;
                                            case InstructionFormat.EI:
                                                if (syntax.Imm.Length > 0 && InstructionSyntax.IsLabel(syntax.Imm))
                                                {
                                                    relocationTable.Add(new RelocationTableElement(
                                                        (ulong)pos,
                                                        syntax.Imm,
                                                        currSegment == Segment.Text ? RelocationType.EIFormatImm : RelocationType.KEIFormatImm,
                                                        useExternLabel ? SymbolScope.Global : SymbolScope.Internal));
                                                }
                                                break;
                                            case InstructionFormat.Pseudo:
                                                break;
                                            case InstructionFormat.Unknown:
                                                throw new Exception("Unknown instruction");
                                        }

                                        if (syntax.Format == InstructionFormat.Pseudo)
                                        {
                                            InstructionSyntax[] realInsts = null;
                                            if (syntax.Opcode == "la")
                                            {
                                                (realInsts, string label) = ProcessLA(code);
                                                relocationTable.Add(new RelocationTableElement(
                                                    (ulong)pos,
                                                    label,
                                                    currSegment == Segment.Text ? RelocationType.LAAddress : RelocationType.KLAAddress,
                                                    useExternLabel ? SymbolScope.Global : SymbolScope.Internal));
                                            }
                                            else
                                            {
                                                realInsts = ProcessPseudoInst(code);
                                            }

                                            for (int i = 0; i < realInsts.Length; i++)
                                            {
                                                textWriter.Write(realInsts[i].ToBinary());
                                            }
                                        }
                                        else
                                        {
                                            textWriter.Write(syntax.ToBinary());
                                        }

                                        if (useLabel)
                                        {
                                            symbolTable[currLabel] = new SymbolTableElement(
                                                currLabel,
                                                currSegment == Segment.Text ? SymbolType.Text : SymbolType.KText,
                                                isGlobal ? SymbolScope.Global : SymbolScope.Internal,
                                                (ulong)pos);

                                            useLabel = false;
                                            isGlobal = false;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        throw new Exception($"{MakeError(line, flineOriginal)}: {e.Message}", e);
                                    }
                                }
                            }
                        }

                        dataBin = data.GetBuffer();
                        textBin = text.GetBuffer();

                        kdataBin = kdata.GetBuffer();
                        ktextBin = ktextOther.GetBuffer();
                        ktextStartBin = ktextStart.GetBuffer();

                        dataSize = (ulong)data.Length;
                        textSize = (ulong)text.Length;

                        kdataSize = (ulong)kdata.Length;
                        ktextSize = (ulong)ktextOther.Length;
                        ktextStartSize = (ulong)ktextStart.Length;
                    }
                }

                fread.Close();
            }

            foreach (var rtElem in relocationTable)
            {
                if (rtElem.LabelSearchLocation == SymbolScope.Internal)
                {
                    if (!symbolTable.ContainsKey(rtElem.Label))
                    {
                        throw new Exception($"Error in file {inputFile}: undefined internal label {rtElem.Label}");
                    }
                }
            }

            if (hasEntryPoint)
            {
                if (!symbolTable.ContainsKey(entrypoint))
                {
                    throw new Exception($"Error in file {inputFile}: no entry point named {entrypoint}");
                }
            }

            byte[] relocBin = null;
            byte[] symBin = null;

            ulong relocSize = 0U;
            ulong symSize = 0U;

            using (MemoryStream reloc = new MemoryStream(), sym = new MemoryStream())
            {
                using (BinaryWriter relwrite = new BinaryWriter(reloc), symwrite = new BinaryWriter(sym))
                {
                    // relocation table struct
                    // first 8 byte: number of entries
                    // reloc entries:
                    // 8 byte: position in data section
                    // 4 byte: byte length(n) of the label string
                    // n byte: UTF8 encoded label string
                    // 2 byte: relocation type
                    // 2 byte: label search location
                    relwrite.Write((ulong)relocationTable.Count);

                    foreach (var relEntry in relocationTable)
                    {
                        relwrite.Write(relEntry.Position);
                        byte[] labelBin = Encoding.UTF8.GetBytes(relEntry.Label);
                        relwrite.Write((uint)labelBin.Length);
                        relwrite.Write(labelBin);
                        relwrite.Write((ushort)relEntry.Type);
                        relwrite.Write((ushort)relEntry.LabelSearchLocation);
                    }

                    // symbol table struct
                    // first 8 byte: number of entries
                    // symbol entries:
                    // 4 byte: byte length(n) of the label string
                    // n byte: UTF8 encoded label string
                    // 2 byte: symbol type
                    // 2 byte: symbol scope
                    // 8 byte: position of the symbol in data/text section
                    symwrite.Write((ulong)symbolTable.Count);

                    foreach (var symEntryPair in symbolTable)
                    {
                        var symEntry = symEntryPair.Value;

                        byte[] labelBin = Encoding.UTF8.GetBytes(symEntry.Label);
                        symwrite.Write((uint)labelBin.Length);
                        symwrite.Write(labelBin);
                        symwrite.Write((ushort)symEntry.Type);
                        symwrite.Write((ushort)symEntry.Scope);
                        symwrite.Write(symEntry.Position);
                    }

                    relocBin = reloc.GetBuffer();
                    symBin = sym.GetBuffer();

                    relocSize = (ulong)reloc.Length;
                    symSize = (ulong)sym.Length;
                }
            }

            ELFHeader elfHeader = new ELFHeader();
            elfHeader.e_type = ELFHeader.ET_REL;
            elfHeader.e_entry = hasEntryPoint ? TEXT_START + symbolTable[entrypoint].Position : 0;
            elfHeader.e_phoff = elfHeader.e_ehsize;
            elfHeader.e_phnum = 6; // text, data, ktext, kdata, reloc, symbol

            ProgramHeader textHeader = new ProgramHeader();
            textHeader.p_type = ProgramHeader.PT_LOAD;
            textHeader.p_offset = elfHeader.e_ehsize + (ulong)elfHeader.e_phentsize * elfHeader.e_phnum;
            textHeader.p_vaddr = TEXT_START;
            textHeader.p_paddr = 0;
            textHeader.p_filesz = textSize;
            textHeader.p_memsz = textSize;
            textHeader.p_flags = ProgramHeader.PF_X | ProgramHeader.PF_R;
            textHeader.p_align = 4;

            ProgramHeader dataHeader = new ProgramHeader();
            dataHeader.p_type = ProgramHeader.PT_LOAD;
            dataHeader.p_offset = textHeader.p_offset + textHeader.p_filesz;
            dataHeader.p_vaddr = DATA_START;
            dataHeader.p_paddr = 0;
            dataHeader.p_filesz = dataSize;
            dataHeader.p_memsz = dataSize > MIN_STATIC_SIZE ? dataSize + MIN_STATIC_SIZE / 2 : MIN_STATIC_SIZE;
            dataHeader.p_flags = ProgramHeader.PF_R | ProgramHeader.PF_W;
            dataHeader.p_align = 32;

            ProgramHeader ktextHeader = new ProgramHeader();
            ktextHeader.p_type = ktextType;
            ktextHeader.p_offset = dataHeader.p_offset + dataHeader.p_filesz;
            ktextHeader.p_vaddr = ktextStartAddr;
            ktextHeader.p_paddr = 0;
            ktextHeader.p_filesz = ktextSize + ktextStartSize;
            ktextHeader.p_memsz = ktextSize + ktextStartSize;
            ktextHeader.p_flags = ProgramHeader.PF_X | ProgramHeader.PF_R;
            ktextHeader.p_align = 4;

            ProgramHeader kdataHeader = new ProgramHeader();
            kdataHeader.p_type = ProgramHeader.PT_KLOAD;
            kdataHeader.p_offset = ktextHeader.p_offset + ktextHeader.p_filesz;
            kdataHeader.p_vaddr = KDATA_START;
            kdataHeader.p_paddr = 0;
            kdataHeader.p_filesz = kdataSize;
            kdataHeader.p_memsz = kdataSize;
            kdataHeader.p_flags = ProgramHeader.PF_R | ProgramHeader.PF_W;
            kdataHeader.p_align = 32;

            ProgramHeader relocHeader = new ProgramHeader();
            relocHeader.p_type = ProgramHeader.PT_RELOCTABLE;
            relocHeader.p_offset = kdataHeader.p_offset + kdataHeader.p_filesz;
            relocHeader.p_vaddr = 0;
            relocHeader.p_paddr = 0;
            relocHeader.p_filesz = relocSize;
            relocHeader.p_memsz = 0;
            relocHeader.p_flags = 0;
            relocHeader.p_align = 0;

            ProgramHeader symHeader = new ProgramHeader();
            symHeader.p_type = ProgramHeader.PT_SYMBOLTABLE;
            symHeader.p_offset = relocHeader.p_offset + relocHeader.p_filesz;
            symHeader.p_vaddr = 0;
            symHeader.p_paddr = 0;
            symHeader.p_filesz = symSize;
            symHeader.p_memsz = 0;
            symHeader.p_flags = 0;
            symHeader.p_align = 0;

            using (FileStream fout = new FileStream(outputFile, FileMode.Create))
            {
                using (BinaryWriter fwrite = new BinaryWriter(fout))
                {
                    fwrite.Write(HeaderUtils.GetBytes(elfHeader));
                    fwrite.Write(HeaderUtils.GetBytes(textHeader));
                    fwrite.Write(HeaderUtils.GetBytes(dataHeader));
                    fwrite.Write(HeaderUtils.GetBytes(ktextHeader));
                    fwrite.Write(HeaderUtils.GetBytes(kdataHeader));
                    fwrite.Write(HeaderUtils.GetBytes(relocHeader));
                    fwrite.Write(HeaderUtils.GetBytes(symHeader));

                    fwrite.Write(textBin, 0, (int)textSize);
                    fwrite.Write(dataBin, 0, (int)dataSize);

                    fwrite.Write(ktextStartBin, 0, (int)ktextStartSize);
                    fwrite.Write(ktextBin, 0, (int)ktextSize);
                    fwrite.Write(kdataBin, 0, (int)kdataSize);

                    fwrite.Write(relocBin, 0, (int)relocSize);
                    fwrite.Write(symBin, 0, (int)symSize);
                }
            }

            return;
        }
    }
}
