using Microsoft.Win32;

using SRA_Assembler;

using SRA_Simulator;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

using static System.Windows.Forms.DataFormats;

namespace SRA_Debugger
{
    public static class Disassembler
    {
        public static readonly IReadOnlyDictionary<OpcodeFunc, string> opcodeToInst;
        public static readonly IReadOnlyDictionary<uint, InstructionFormat> opcodeFormats;
        public static readonly IReadOnlyDictionary<int, string> regnumToName;

        public static readonly IReadOnlySet<string> unsignedImmInsts;
        public static readonly IReadOnlySet<string> hexImmInsts;

        static Disassembler()
        {
            var optoinst = new Dictionary<OpcodeFunc, string>();
            opcodeToInst = optoinst;

            var opformat = new Dictionary<uint, InstructionFormat>();
            opcodeFormats = opformat;

            foreach (var inst in InstData.instructions)
            {
                Instruction instruction = inst.Value;
                if (instruction.Format == InstructionFormat.Pseudo)
                {
                    continue;
                }

                optoinst[new OpcodeFunc(instruction.Opcode, instruction.Func)] = instruction.Name;

                if (!opformat.ContainsKey(instruction.Opcode))
                {
                    opformat[instruction.Opcode] = instruction.Format;
                }
            }

            var regtoname = new Dictionary<int, string>();
            regnumToName = regtoname;

            foreach (var reg in InstructionSyntax.regAlts)
            {
                regtoname[Convert.ToInt32(reg.Value.Substring(1), 10)] = reg.Key;
            }

            unsignedImmInsts = new HashSet<string>(new string[]
            {
                "addiu",
                "sltiu",
                "vldiu.64",
                "vldiu.32",
                "vbroadiu.64",
                "vbroadiu.32"
            });

            hexImmInsts = new HashSet<string>(new string[]
            {
                "andi",
                "ori",
                "xori",
                "li.0",
                "li.1",
                "li.2",
                "li.3"
            });
        }

        public static string DisassembleInstruction(uint inst, ulong instAddr)
        {
            uint opcode = (inst >> 25) & 0b111_1111U;
            if (!opcodeFormats.ContainsKey(opcode))
            {
                throw new Exception($"Disassemble error: unknown opcode 0x{opcode:x}");
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

                        string name = opcodeToInst[new OpcodeFunc(opcode, func)];

                        if (InstData.rInsts.Contains(name))
                        {
                            if (name == "jr" || name == "jalr") // op rs
                            {
                                return $"{name} {regnumToName[rs]}";
                            }
                            else // op rd, rs, rt
                            {
                                return $"{name} {regnumToName[rd]}, {regnumToName[rs]}, {regnumToName[rt]}";
                            }
                        }
                        else if (InstData.vrInsts.Contains(name))
                        {
                            if (InstructionSyntax.vrdRsRtOps.Contains(name)) // op vrd, rs, rt
                            {
                                return $"{name} %v{rd}, {regnumToName[rs]}, {regnumToName[rt]}";
                            }
                            else if (InstructionSyntax.vrdOps.Contains(name)) // op vrd
                            {
                                return $"{name} %v{rd}";
                            }
                            else if (InstructionSyntax.vrdRsOps.Contains(name)) // op vrd, rs
                            {
                                return $"{name} %v{rd}, {regnumToName[rs]}";
                            }
                            else if (InstructionSyntax.vrdVrsOps.Contains(name)) // op vrd, vrs
                            {
                                return $"{name} %v{rd}, %v{rs}";
                            }
                            else if (InstructionSyntax.vrdVrsRtOps.Contains(name)) // op vrd, vrs, rt
                            {
                                return $"{name} %v{rd}, %v{rs}, {regnumToName[rt]}";
                            }
                            else if (InstructionSyntax.rdVrsVrtOps.Contains(name)) // op rd, vrs, vrt
                            {
                                return $"{name} {regnumToName[rd]}, %v{rs}, %v{rt}";
                            }
                            else if (InstructionSyntax.rdVrsOps.Contains(name)) // op rd, vrs
                            {
                                return $"{name} {regnumToName[rd]}, %v{rs}";
                            }
                            else // op vrd, vrs, vrt
                            {
                                return $"{name} %v{rd}, %v{rs}, %v{rt}";
                            }
                        }
                        else
                        {
                            throw new Exception($"Disassemble error: unknown opcode 0x{opcode:x}");
                        }
                    }
                case InstructionFormat.I:
                    {
                        int rs = (int)((inst >> 20) & 0b1_1111U);
                        int rt = (int)((inst >> 15) & 0b1_1111U);
                        ushort imm = (ushort)(inst & 0b111_1111_1111_1111U);

                        string name = opcodeToInst[new OpcodeFunc(opcode, 0)];

                        if (name == "mul" || name == "mulu" ||
                            name == "div" || name == "divu") // op rs, rt
                        {
                            return $"{name} {regnumToName[rs]}, {regnumToName[rt]}";
                        }
                        else if (InstructionSyntax.loadStoreImmOps.Contains(name)) // op rt, [rs]imm
                        {
                            long simm = (long)CPU.SignExtendImm15(imm);
                            return $"{name} {regnumToName[rt]}, [{regnumToName[rs]}]{simm}";
                        }
                        else if (InstructionSyntax.branchImmOps.Contains(name)) // op rs, rt, imm(label or number)
                        {
                            long simm = (long)CPU.SignExtendImm15(imm);
                            return $"{name} {regnumToName[rs]}, {regnumToName[rt]}, {simm}";
                        }
                        else // op rt, rs, imm
                        {
                            if (unsignedImmInsts.Contains(name))
                            {
                                return $"{name} {regnumToName[rt]}, {regnumToName[rs]}, {imm}";
                            }
                            else if (hexImmInsts.Contains(name))
                            {
                                return $"{name} {regnumToName[rt]}, {regnumToName[rs]}, 0x{imm:x}";
                            }
                            else
                            {
                                long simm = (long)CPU.SignExtendImm15(imm);
                                return $"{name} {regnumToName[rt]}, {regnumToName[rs]}, {simm}";
                            }
                        }
                    }
                case InstructionFormat.J:
                    {
                        uint addr = inst & 0x1FFFFFFU;

                        string name = opcodeToInst[new OpcodeFunc(opcode, 0)];

                        if (name == "syscall" || name == "nop") // op
                        {
                            return name;
                        }
                        else // op addr
                        {
                            ulong unchangeMask = ~CPU.JUMP_BIT_MASK;
                            ulong realAddr = (instAddr & unchangeMask) | (((ulong)addr << 2) & CPU.JUMP_BIT_MASK);
                            return $"{name} 0x{realAddr:x16}";
                        }
                    }
                case InstructionFormat.EI:
                    {
                        int rd = (int)((inst >> 20) & 0b1_1111U);
                        uint func = (inst >> 16) & 0b1111U;
                        ushort imm = (ushort)(inst & 0xFFFFU);

                        string name = opcodeToInst[new OpcodeFunc(opcode, func)];

                        if (InstData.eiInsts.Contains(name))
                        {
                            if (name == "mfhi" || name == "mflo" ||
                                name == "mthi" || name == "mtlo") // op rd
                            {
                                return $"{name} {regnumToName[rd]}";
                            }
                            else // op rd, imm
                            {
                                if (unsignedImmInsts.Contains(name))
                                {
                                    return $"{name} {regnumToName[rd]}, {imm}";
                                }
                                else if (hexImmInsts.Contains(name))
                                {
                                    return $"{name} {regnumToName[rd]}, 0x{imm:x}";
                                }
                                else
                                {
                                    return $"{name} {regnumToName[rd]}, {(short)imm}";
                                }
                            }
                        }
                        else if (InstData.veiInsts.Contains(name)) // op vrd, imm
                        {
                            if (unsignedImmInsts.Contains(name))
                            {
                                return $"{name} %v{rd}, {imm}";
                            }
                            else if (hexImmInsts.Contains(name))
                            {
                                return $"{name} %v{rd}, 0x{imm:x}";
                            }
                            else
                            {
                                return $"{name} %v{rd}, {(short)imm}";
                            }
                        }
                        else
                        {
                            throw new Exception($"Disassemble error: unknown opcode 0x{opcode:x}");
                        }
                    }
                default:
                    throw new Exception("Unknown instruction");
            }
        }

        public static string[] Disassemble(byte[] text, ulong textStartAddr)
        {
            if (text.Length % 4 != 0)
            {
                throw new Exception("Text byte size is not the multiple of 4");
            }

            string[] asm = new string[text.Length / 4];

            using (MemoryStream textBin = new MemoryStream(text, false))
            {
                using (BinaryReader tr = new BinaryReader(textBin))
                {
                    for (ulong i = 0; i < (ulong)textBin.Length / 4; i++)
                    {
                        asm[i] = DisassembleInstruction(tr.ReadUInt32(), textStartAddr + i * 4);
                    }
                }
            }

            return asm;
        }
    }
}
