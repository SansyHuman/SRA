using System.Text;

namespace SRA_Assembler
{
    public enum Segment
    {
        Data, Text, KData, KText, Uninitialized
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
        LAAddress
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

            ulong dataSize = 0U;
            ulong textSize = 0U;

            using (StreamReader fread = new StreamReader(inputFile))
            {
                using (MemoryStream data = new MemoryStream(), text = new MemoryStream())
                {
                    using (BinaryWriter dwrite = new BinaryWriter(data), twrite = new BinaryWriter(text))
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
                                else
                                {
                                    throw new Exception($"{MakeError(line, flineOriginal)}: segment specifier should exist before any code");
                                }

                                if (useLabel)
                                {
                                    throw new Exception($"{MakeError(line, flineOriginal)}: no actual code after label");
                                }
                            }
                            else if (currSegment == Segment.Data)
                            {
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
                                    long pos = MoveToByteBoundary(data, 8, currAlignment);
                                    dwrite.Write(value);

                                    prevDataSize = 8;
                                    prevData = value;

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            SymbolType.Data,
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
                                    long pos = MoveToByteBoundary(data, 4, currAlignment);
                                    dwrite.Write(value);

                                    prevDataSize = 4;
                                    prevData = value;

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            SymbolType.Data,
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
                                    long pos = MoveToByteBoundary(data, 2, currAlignment);
                                    dwrite.Write(value);

                                    prevDataSize = 2;
                                    prevData = value;

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            SymbolType.Data,
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
                                    long pos = MoveToByteBoundary(data, 1, currAlignment);
                                    dwrite.Write(value);

                                    prevDataSize = 1;
                                    prevData = value;

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            SymbolType.Data,
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
                                    long pos = MoveToByteBoundary(data, 4, currAlignment);
                                    dwrite.Write(value);

                                    prevDataSize = 4;
                                    unsafe
                                    {
                                        prevData = *((uint*)&value);
                                    }

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            SymbolType.Data,
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
                                    long pos = MoveToByteBoundary(data, 8, currAlignment);
                                    dwrite.Write(value);

                                    prevDataSize = 8;
                                    unsafe
                                    {
                                        prevData = *((ulong*)&value);
                                    }

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            SymbolType.Data,
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

                                    long pos = MoveToByteBoundary(data, 1, currAlignment);
                                    dwrite.Write(encode);

                                    prevDataSize = -1; // string literal does not support .repeat

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            SymbolType.Data,
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

                                    long pos = MoveToByteBoundary(data, 1, currAlignment);
                                    dwrite.Write(encode);
                                    dwrite.Write((byte)0U); // null-terminated

                                    prevDataSize = -1;

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            SymbolType.Data,
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

                                    long pos = MoveToByteBoundary(data, 8, currAlignment);
                                    dwrite.Write((ulong)encode.Length); // store the byte length of the string
                                    dwrite.Write(encode);

                                    prevDataSize = -1;

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            SymbolType.Data,
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
                                    long pos = data.Position;
                                    dwrite.Write(new byte[cnt]);

                                    prevDataSize = -1;

                                    if (useLabel)
                                    {
                                        symbolTable[currLabel] = new SymbolTableElement(
                                            currLabel,
                                            SymbolType.Data,
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
                                        MoveToByteBoundary(data, prevDataSize, currAlignment);
                                        switch (prevDataSize)
                                        {
                                            case 1:
                                                dwrite.Write((byte)prevData);
                                                break;
                                            case 2:
                                                dwrite.Write((ushort)prevData);
                                                break;
                                            case 4:
                                                dwrite.Write((uint)prevData);
                                                break;
                                            case 8:
                                                dwrite.Write(prevData);
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
                                            SymbolType.Data,
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
                            else if (currSegment == Segment.Text)
                            {
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
                                else if (args[0] == ".entry")
                                {
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
                                        long pos = text.Position;
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
                                                        RelocationType.IFormatImm,
                                                        useExternLabel ? SymbolScope.Global : SymbolScope.Internal));
                                                }
                                                break;
                                            case InstructionFormat.J:
                                                if (syntax.Addr.Length > 0 && InstructionSyntax.IsLabel(syntax.Addr))
                                                {
                                                    relocationTable.Add(new RelocationTableElement(
                                                        (ulong)pos,
                                                        syntax.Addr,
                                                        RelocationType.JFormatAddr,
                                                        useExternLabel ? SymbolScope.Global : SymbolScope.Internal));
                                                }
                                                break;
                                            case InstructionFormat.EI:
                                                if (syntax.Imm.Length > 0 && InstructionSyntax.IsLabel(syntax.Imm))
                                                {
                                                    relocationTable.Add(new RelocationTableElement(
                                                        (ulong)pos,
                                                        syntax.Imm,
                                                        RelocationType.EIFormatImm,
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
                                                    RelocationType.LAAddress,
                                                    useExternLabel ? SymbolScope.Global : SymbolScope.Internal));
                                            }
                                            else
                                            {
                                                realInsts = ProcessPseudoInst(code);
                                            }

                                            for (int i = 0; i < realInsts.Length; i++)
                                            {
                                                twrite.Write(realInsts[i].ToBinary());
                                            }
                                        }
                                        else
                                        {
                                            twrite.Write(syntax.ToBinary());
                                        }

                                        if (useLabel)
                                        {
                                            symbolTable[currLabel] = new SymbolTableElement(
                                                currLabel,
                                                SymbolType.Text,
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

                        dataSize = (ulong)data.Length;
                        textSize = (ulong)text.Length;
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
            elfHeader.e_phnum = 4; // text, data, reloc, symbol

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

            ProgramHeader relocHeader = new ProgramHeader();
            relocHeader.p_type = ProgramHeader.PT_RELOCTABLE;
            relocHeader.p_offset = dataHeader.p_offset + dataHeader.p_filesz;
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
                    fwrite.Write(HeaderUtils.GetBytes(relocHeader));
                    fwrite.Write(HeaderUtils.GetBytes(symHeader));

                    fwrite.Write(textBin, 0, (int)textSize);
                    fwrite.Write(dataBin, 0, (int)dataSize);
                    fwrite.Write(relocBin, 0, (int)relocSize);
                    fwrite.Write(symBin, 0, (int)symSize);
                }
            }

            return;
        }
    }
}
