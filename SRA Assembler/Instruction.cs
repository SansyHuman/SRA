using System.Text;
using System.Text.RegularExpressions;

namespace SRA_Assembler
{
    public enum InstructionFormat
    {
        R, I, J, EI, Pseudo, Unknown
    }

    public struct Instruction
    {
        public string Name;
        public InstructionFormat Format;
        public uint Opcode;
        public uint Func;

        public Instruction(string name, InstructionFormat format, uint opcode, uint func)
        {
            Name = name;
            Format = format;
            Opcode = opcode;
            Func = func;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"{Name} [Format: {Format}");

            if (Format != InstructionFormat.Pseudo)
            {
                builder.Append($", Opcode: 0x{Opcode:X}");
            }

            if (Format == InstructionFormat.R || Format == InstructionFormat.EI)
            {
                builder.Append($", Func: 0x{Func:X}");
            }

            builder.Append("]");

            return builder.ToString();
        }
    }

    public struct InstructionSyntax
    {
        public InstructionFormat Format;
        public string Opcode;
        public string RS;
        public string RT;
        public string RD;
        public string Imm;
        public string Addr;

        public static readonly char[] whitespace = { ' ', '\t' };

        public static readonly ISet<string> regs;
        public static readonly IDictionary<string, string> regAlts;
        public static readonly IDictionary<string, string> kregAlts;
        public static readonly ISet<string> vregs;

        public static readonly ISet<string> loadStoreImmOps;
        public static readonly ISet<string> branchImmOps;

        public static readonly ISet<string> vrdRsRtOps;
        public static readonly ISet<string> vrdOps;
        public static readonly ISet<string> vrdRsOps;
        public static readonly ISet<string> vrdVrsOps;
        public static readonly ISet<string> vrdVrsRtOps;
        public static readonly ISet<string> rdVrsVrtOps;
        public static readonly ISet<string> rdVrsOps;

        static InstructionSyntax()
        {
            var reg = new HashSet<string>();
            var vreg = new HashSet<string>();
            InstructionSyntax.regs = reg;
            InstructionSyntax.vregs = vreg;

            for (int i = 0; i <= 31; i++)
            {
                reg.Add($"%{i}");
                vreg.Add($"%v{i}");
            }

            var regalt = new Dictionary<string, string>();
            InstructionSyntax.regAlts = regalt;

            regalt["%zero"] = "%0";
            regalt["%asm"] = "%1";
            regalt["%ret"] = "%2";
            for (int i = 0; i <= 5; i++)
            {
                regalt[$"%a{i}"] = $"%{i + 3}";
            }
            for (int i = 0; i <= 9; i++)
            {
                regalt[$"%t{i}"] = $"%{i + 9}";
            }
            for (int i = 0; i <= 8; i++)
            {
                regalt[$"%s{i}"] = $"%{i + 19}";
            }
            regalt["%fp"] = "%28";
            regalt["%sp"] = "%29";
            regalt["%gp"] = "%30";
            regalt["%ra"] = "%31";

            var kregalt = new Dictionary<string, string>();
            InstructionSyntax.kregAlts = kregalt;

            kregalt["%epc"] = "%1";
            kregalt["%ie"] = "%2";
            kregalt["%ip"] = "%3";
            kregalt["%cause"] = "%4";
            kregalt["%time"] = "%5";
            kregalt["%timecmp"] = "%6";

            loadStoreImmOps = new HashSet<string>(new string[]
            {
                "ldi",
                "lwi",
                "lwiu",
                "lhi",
                "lhiu",
                "lbi",
                "lbiu",
                "sdi",
                "swi",
                "shi",
                "sbi",
            });

            branchImmOps = new HashSet<string>(new string[]
            {
                "beq",
                "bne",
                "bge",
                "bgeu",
                "blt",
                "bltu"
            });

            vrdRsRtOps = new HashSet<string>(new string[]
            {
                "vld",
                "vld.128",
                "vld.64",
                "vld.32",
                "vst",
                "vst.128",
                "vst.64",
                "vst.32",
                "vbroad.64",
                "vbroad.32"
            });

            vrdOps = new HashSet<string>(new string[]
            {
                "vldhilo",
                "vsthilo"
            });

            vrdRsOps = new HashSet<string>(new string[]
            {
                "vldr",
                "vldr.32",
                "vstr",
                "vstr.32",
                "vstr.32u",
                "vbroadr",
                "vbroadr.32"
            });

            vrdVrsOps = new HashSet<string>(new string[]
            {
                "vcvti64tof64",
                "vcvti64tof64.s",
                "vcvtu64tof64",
                "vcvtu64tof64.s",
                "vcvti64tof32",
                "vcvti64tof32.s",
                "vcvtu64tof32",
                "vcvtu64tof32.s",
                "vcvtf64tof32",
                "vcvtf64tof32.s",
                "vcvtf32tof64",
                "vcvtf32tof64.s",
                "vsumf64",
                "vsumf32",
                "vsumf32.128"
            });

            vrdVrsRtOps = new HashSet<string>(new string[]
            {
                "vcvtf64toi64",
                "vcvtf64toi64.s",
                "vcvtf64toi32",
                "vcvtf64toi32.s",
                "vcvtf32toi64",
                "vcvtf32toi64.s",
                "vcvtf32toi32",
                "vcvtf32toi32.s",
                "vslla",
                "vsrla",
                "vshuffle.64",
                "vshuffle.32"
            });

            rdVrsVrtOps = new HashSet<string>(new string[]
            {
                "vsgef64.s",
                "vsgef32.s",
                "vsltf64.s",
                "vsltf32.s"
            });

            rdVrsOps = new HashSet<string>(new string[]
            {
                "vextlsb.64",
                "vextlsb.32",
                "vextmsb.64",
                "vextmsb.32",
                "vsumi64",
                "vsumi32",
                "vsumi32.128"
            });
        }

        public InstructionSyntax(string inst)
        {
            inst = inst.Trim();
            string[] tmp = inst.Split(whitespace, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            Opcode = tmp[0].ToLower();
            Format = InstData.GetFormat(Opcode);
            if (Format == InstructionFormat.Unknown)
            {
                throw new Exception($"Unknown instruction {Opcode}");
            }

            RS = string.Empty;
            RT = string.Empty;
            RD = string.Empty;
            Imm = string.Empty;
            Addr = string.Empty;

            if (InstData.rInsts.Contains(Opcode))
            {
                if (Opcode == "jr" || Opcode == "jalr") // op rs
                {
                    string rs = inst.Substring(Opcode.Length).Trim();
                    if (rs == string.Empty)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough registers");
                    }
                    RS = GetRegNumber(rs);
                }
                else if (Opcode == "krr") // op rd, krs
                {
                    string[] regs = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (regs.Length < 2)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough registers");
                    }
                    if (regs.Length > 2)
                    {
                        throw new Exception($"Instruction {Opcode} have more registers than needed");
                    }

                    RD = GetRegNumber(regs[0]);
                    RS = GetKRegNumber(regs[1]);
                }
                else if (Opcode == "krw") // op krd, rs
                {
                    string[] regs = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (regs.Length < 2)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough registers");
                    }
                    if (regs.Length > 2)
                    {
                        throw new Exception($"Instruction {Opcode} have more registers than needed");
                    }

                    RD = GetKRegNumber(regs[0]);
                    RS = GetRegNumber(regs[1]);
                }
                else // op rd, rs, rt
                {
                    string[] regs = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (regs.Length < 3)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough registers");
                    }
                    if (regs.Length > 3)
                    {
                        throw new Exception($"Instruction {Opcode} have more registers than needed");
                    }

                    RD = GetRegNumber(regs[0]);
                    RS = GetRegNumber(regs[1]);
                    RT = GetRegNumber(regs[2]);
                }
            }
            else if (InstData.iInsts.Contains(Opcode))
            {
                if (Opcode == "mul" || Opcode == "mulu" ||
                    Opcode == "div" || Opcode == "divu") // op rs, rt
                {
                    string[] regs = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (regs.Length < 2)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough registers");
                    }
                    if (regs.Length > 2)
                    {
                        throw new Exception($"Instruction {Opcode} have more registers than needed");
                    }

                    RS = GetRegNumber(regs[0]);
                    RT = GetRegNumber(regs[1]);
                }
                else if (loadStoreImmOps.Contains(Opcode)) // op rt, [rs]imm
                {
                    string[] operands = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (operands.Length < 2)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough operands");
                    }
                    if (operands.Length > 2)
                    {
                        throw new Exception($"Instruction {Opcode} have more operands than needed");
                    }

                    RT = GetRegNumber(operands[0]);

                    int openBracket = operands[1].IndexOf('[');
                    int closeBracket = operands[1].IndexOf(']');
                    if (openBracket != 0 || closeBracket < 0)
                    {
                        throw new Exception($"Instruction {Opcode} have wrong addressing format");
                    }

                    RS = GetRegNumber(operands[1].Substring(1, closeBracket - 1));
                    Imm = CheckImmediateNum(operands[1].Substring(closeBracket + 1), true);
                }
                else if (branchImmOps.Contains(Opcode)) // op rs, rt, imm(label or number)
                {
                    string[] operands = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (operands.Length < 3)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough operands");
                    }
                    if (operands.Length > 3)
                    {
                        throw new Exception($"Instruction {Opcode} have more operands than needed");
                    }

                    RS = GetRegNumber(operands[0]);
                    RT = GetRegNumber(operands[1]);

                    if (IsLabel(operands[2]))
                    {
                        Imm = operands[2];
                    }
                    else
                    {
                        Imm = CheckImmediateNum(operands[2], true);
                    }
                }
                else // op rt, rs, imm
                {
                    string[] operands = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (operands.Length < 3)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough operands");
                    }
                    if (operands.Length > 3)
                    {
                        throw new Exception($"Instruction {Opcode} have more operands than needed");
                    }

                    RT = GetRegNumber(operands[0]);
                    RS = GetRegNumber(operands[1]);
                    Imm = CheckImmediateNum(operands[2], true);
                }
            }
            else if (InstData.jInsts.Contains(Opcode))
            {
                if (Opcode == "syscall" || Opcode == "nop" ||
                    Opcode == "eret" || Opcode == "ecall") // op
                {
                    string[] operands = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (operands.Length != 0)
                    {
                        throw new Exception($"Instruction {Opcode} have more operand than needed");
                    }
                }
                else // op addr
                {
                    string[] operands = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (operands.Length < 1)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough operands");
                    }
                    if (operands.Length > 1)
                    {
                        throw new Exception($"Instruction {Opcode} have more operand than needed");
                    }

                    if (IsLabel(operands[0]))
                    {
                        Addr = operands[0];
                    }
                    else
                    {
                        throw new Exception($"Address of {Opcode} cannot be an immediate value");
                    }
                }
            }
            else if (InstData.eiInsts.Contains(Opcode))
            {
                if (Opcode == "mfhi" || Opcode == "mflo" ||
                    Opcode == "mthi" || Opcode == "mtlo") // op rd
                {
                    string[] operands = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (operands.Length < 1)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough operands");
                    }
                    if (operands.Length > 1)
                    {
                        throw new Exception($"Instruction {Opcode} have more operand than needed");
                    }

                    RD = GetRegNumber(operands[0]);
                }
                else // op rd, imm
                {
                    string[] operands = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (operands.Length < 2)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough operands");
                    }
                    if (operands.Length > 2)
                    {
                        throw new Exception($"Instruction {Opcode} have more operand than needed");
                    }

                    RD = GetRegNumber(operands[0]);
                    Imm = CheckImmediateNum(operands[1], true);
                }
            }
            else if (InstData.vrInsts.Contains(Opcode))
            {
                if (vrdRsRtOps.Contains(Opcode)) // op vrd, rs, rt
                {
                    string[] regs = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (regs.Length < 3)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough registers");
                    }
                    if (regs.Length > 3)
                    {
                        throw new Exception($"Instruction {Opcode} have more registers than needed");
                    }

                    RD = GetVRegNumber(regs[0]);
                    RS = GetRegNumber(regs[1]);
                    RT = GetRegNumber(regs[2]);
                }
                else if (vrdOps.Contains(Opcode)) // op vrd
                {
                    string[] regs = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (regs.Length < 1)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough registers");
                    }
                    if (regs.Length > 1)
                    {
                        throw new Exception($"Instruction {Opcode} have more registers than needed");
                    }

                    RD = GetVRegNumber(regs[0]);
                }
                else if (vrdRsOps.Contains(Opcode)) // op vrd, rs
                {
                    string[] regs = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (regs.Length < 2)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough registers");
                    }
                    if (regs.Length > 2)
                    {
                        throw new Exception($"Instruction {Opcode} have more registers than needed");
                    }

                    RD = GetVRegNumber(regs[0]);
                    RS = GetRegNumber(regs[1]);
                }
                else if (vrdVrsOps.Contains(Opcode)) // op vrd, vrs
                {
                    string[] regs = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (regs.Length < 2)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough registers");
                    }
                    if (regs.Length > 2)
                    {
                        throw new Exception($"Instruction {Opcode} have more registers than needed");
                    }

                    RD = GetVRegNumber(regs[0]);
                    RS = GetVRegNumber(regs[1]);
                }
                else if (vrdVrsRtOps.Contains(Opcode)) // op vrd, vrs, rt
                {
                    string[] regs = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (regs.Length < 3)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough registers");
                    }
                    if (regs.Length > 3)
                    {
                        throw new Exception($"Instruction {Opcode} have more registers than needed");
                    }

                    RD = GetVRegNumber(regs[0]);
                    RS = GetVRegNumber(regs[1]);
                    RT = GetRegNumber(regs[2]);
                }
                else if (rdVrsVrtOps.Contains(Opcode)) // op rd, vrs, vrt
                {
                    string[] regs = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (regs.Length < 3)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough registers");
                    }
                    if (regs.Length > 3)
                    {
                        throw new Exception($"Instruction {Opcode} have more registers than needed");
                    }

                    RD = GetRegNumber(regs[0]);
                    RS = GetVRegNumber(regs[1]);
                    RT = GetVRegNumber(regs[2]);
                }
                else if (rdVrsOps.Contains(Opcode)) // op rd, vrs
                {
                    string[] regs = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (regs.Length < 2)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough registers");
                    }
                    if (regs.Length > 2)
                    {
                        throw new Exception($"Instruction {Opcode} have more registers than needed");
                    }

                    RD = GetRegNumber(regs[0]);
                    RS = GetVRegNumber(regs[1]);
                }
                else // op vrd, vrs, vrt
                {
                    string[] regs = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (regs.Length < 3)
                    {
                        throw new Exception($"Instruction {Opcode} does not have enough registers");
                    }
                    if (regs.Length > 3)
                    {
                        throw new Exception($"Instruction {Opcode} have more registers than needed");
                    }

                    RD = GetVRegNumber(regs[0]);
                    RS = GetVRegNumber(regs[1]);
                    RT = GetVRegNumber(regs[2]);
                }
            }
            else if (InstData.veiInsts.Contains(Opcode)) // op vrd, imm
            {
                string[] operands = inst.Substring(Opcode.Length).Trim()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (operands.Length < 2)
                {
                    throw new Exception($"Instruction {Opcode} does not have enough operands");
                }
                if (operands.Length > 2)
                {
                    throw new Exception($"Instruction {Opcode} have more operands than needed");
                }

                RD = GetVRegNumber(operands[0]);
                Imm = CheckImmediateNum(operands[1], true);
            }
        }

        public static string GetRegNumber(string reg)
        {
            if (InstructionSyntax.regs.Contains(reg))
            {
                return reg.Substring(1);
            }

            if (InstructionSyntax.regAlts.ContainsKey(reg))
            {
                return regAlts[reg].Substring(1);
            }

            throw new Exception($"Register {reg} is not valid register name.");
        }

        public static string GetVRegNumber(string reg)
        {
            if (InstructionSyntax.vregs.Contains(reg))
            {
                return reg.Substring(2);
            }

            throw new Exception($"Register {reg} is not valid vector register name.");
        }

        public static string GetKRegNumber(string reg)
        {
            if (InstructionSyntax.regs.Contains(reg))
            {
                return reg.Substring(1);
            }

            if (InstructionSyntax.kregAlts.ContainsKey(reg))
            {
                return kregAlts[reg].Substring(1);
            }

            throw new Exception($"Register {reg} is not valid kernel register name.");
        }

        public static string CheckImmediateNum(string imm, bool intOnly)
        {
            string radix = string.Empty;
            if (imm.Length >= 2)
            {
                radix = imm.Substring(0, 2).ToLower();
            }

            if (radix == "0x")
            {
                if (!Regex.IsMatch(imm, @"^(0x|0X)[0-9a-fA-F]+$"))
                {
                    throw new Exception($"Hex immediate {imm} has wrong digit");
                }
            }
            else if (radix == "0b")
            {
                if (!Regex.IsMatch(imm, @"^(0b|0B)[0-1]+$"))
                {
                    throw new Exception($"Binary immediate {imm} has wrong digit");
                }
            }
            else if (radix != string.Empty && radix[0] == '0')
            {
                if (!Regex.IsMatch(imm, @"^0[0-7]+$"))
                {
                    throw new Exception($"Oct immediate {imm} has wrong digit");
                }
            }
            else if (imm.Contains('.'))
            {
                if (intOnly)
                {
                    throw new Exception("Immediate cannot be a floating number.");
                }

                if (!Regex.IsMatch(imm, @"^[+-]?([0-9]*[.][0-9]+)((e|E)[+-][0-9]+)?$"))
                {
                    throw new Exception($"Float immediate {imm} has wrong form");
                }
            }
            else
            {
                if (!Regex.IsMatch(imm, @"^[+-]?[0-9]+$"))
                {
                    throw new Exception($"Decimal immediate {imm} has wrong digit");
                }
            }

            return imm;
        }

        public static bool IsLabel(string imm)
        {
            // labels do not start with ., +, -, or number.
            bool result = imm[0] != '.' && !char.IsNumber(imm[0])
                && imm[0] != '+' && imm[0] != '-';

            // labels do not have whitespace.
            if (imm.Any(char.IsWhiteSpace))
            {
                throw new Exception("Label cannot have whitespace");
            }

            return result;
        }

        public override string ToString()
        {
            switch (Format)
            {
                case InstructionFormat.R:
                    return $"{Opcode} rs: {RS}, rt: {RT}, rd: {RD}";
                case InstructionFormat.EI:
                    return $"{Opcode} rd: {RD}, imm: {Imm}";
                case InstructionFormat.Pseudo:
                    return $"{Opcode}";
                case InstructionFormat.I:
                    return $"{Opcode} rs: {RS}, rt: {RT}, imm: {Imm}";
                case InstructionFormat.J:
                    return $"{Opcode} addr: {Addr}";
                default:
                    return "Unknown instruction";
            }
        }

        public uint ToBinary()
        {
            uint bin = 0U;

            Instruction instData = InstData.instructions[Opcode];

            uint opcode = instData.Opcode;
            bin |= (opcode & 0b111_1111U) << 25;

            switch (Format)
            {
                case InstructionFormat.R:
                    {
                        uint rs = RS == string.Empty ? 0U : uint.Parse(RS);
                        uint rt = RT == string.Empty ? 0U : uint.Parse(RT);
                        uint rd = RD == string.Empty ? 0U : uint.Parse(RD);
                        uint func = instData.Func;

                        bin |= (rs & 0b1_1111U) << 20;
                        bin |= (rt & 0b1_1111U) << 15;
                        bin |= (rd & 0b1_1111U) << 10;
                        bin |= func & 0b11_1111_1111U;
                    }
                    break;
                case InstructionFormat.I:
                    {
                        uint rs = RS == string.Empty ? 0U : uint.Parse(RS);
                        uint rt = RT == string.Empty ? 0U : uint.Parse(RT);
                        uint imm = 0U;
                        if (Imm.Length > 0 && !IsLabel(Imm))
                        {
                            imm = ImmToNumber(Imm, 15);
                        }

                        bin |= (rs & 0b1_1111U) << 20;
                        bin |= (rt & 0b1_1111U) << 15;
                        bin |= imm & 0b111_1111_1111_1111U;
                    }
                    break;
                case InstructionFormat.J:
                    break;
                case InstructionFormat.EI:
                    {
                        uint rd = RD == string.Empty ? 0U : uint.Parse(RD);
                        uint func = instData.Func;
                        uint imm = 0U;
                        if (Imm.Length > 0 && !IsLabel(Imm))
                        {
                            imm = ImmToNumber(Imm, 16);
                        }

                        bin |= (rd & 0b1_1111U) << 20;
                        bin |= (func & 0b1111U) << 16;
                        bin |= imm & 0b1111_1111_1111_1111U;
                    }
                    break;
                default:
                    throw new Exception("Unknown instruction");
            }

            return bin;
        }

        public static uint ImmToNumber(string imm, int bitLen)
        {
            uint value = 0U;

            uint uiMax = (1U << bitLen) - 1U;
            int iMax = (int)((1U << (bitLen - 1)) - 1U);
            int iMin = -iMax - 1;

            string radix = string.Empty;
            if (imm.Length >= 2)
            {
                radix = imm.Substring(0, 2).ToLower();
            }

            if (radix == "0x")
            {
                value = Convert.ToUInt32(imm.Substring(2), 16);
                if (value > uiMax)
                {
                    throw new Exception("Immediate field out of range");
                }
            }
            else if (radix == "0b")
            {
                value = Convert.ToUInt32(imm.Substring(2), 2);
                if (value > uiMax)
                {
                    throw new Exception("Immediate field out of range");
                }
            }
            else if (radix != string.Empty && radix[0] == '0')
            {
                value = Convert.ToUInt32(imm.Substring(1), 8);
                if (value > uiMax)
                {
                    throw new Exception("Immediate field out of range");
                }
            }
            else
            {
                if (imm[0] == '-')
                {
                    int valuei = Convert.ToInt32(imm);
                    if (valuei < iMin || valuei > iMax)
                    {
                        throw new Exception("Immediate field out of range");
                    }
                    value = (uint)valuei;
                }
                else
                {
                    value = Convert.ToUInt32(imm);
                    if (value > uiMax)
                    {
                        throw new Exception("Immediate field out of range");
                    }
                }
            }

            return value;
        }
    }
}
