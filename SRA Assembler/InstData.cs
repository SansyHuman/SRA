namespace SRA_Assembler
{
    public static class InstData
    {
        public static readonly string instList =
@"add,R,0x0,0x0
addw,R,0x0,0x200
addi,I,0x1,-
addiu,I,0x2,-
addiw,I,0x41,-
sub,R,0x0,0x1
subw,R,0x0,0x201
mul,I,0x3,-
mulu,I,0x4,-
div,I,0x5,-
divu,I,0x6,-
mulw,R,0x0,0x202
muluw,R,0x0,0x203
and,R,0x0,0x4
or,R,0x0,0x5
xor,R,0x0,0x6
andi,I,0x7,-
ori,I,0x8,-
xori,I,0x9,-
sll,R,0x0,0x7
sllw,R,0x0,0x207
srl,R,0x0,0x8
srlw,R,0x0,0x208
sra,R,0x0,0x9
sraw,R,0x0,0x209
slt,R,0x0,0xa
sltu,R,0x0,0xb
slti,I,0xa,-
sltiu,I,0xb,-
not,PSEUDO,-,-
neg,PSEUDO,-,-
negw,PSEUDO,-,-
mov,PSEUDO,-,-
movw,PSEUDO,-,-
ld,R,0x0,0x10
lds,R,0x0,0x30
ldi,I,0x10,-
lw,R,0x0,0x11
lwu,R,0x0,0x111
lws,R,0x0,0x31
lwsu,R,0x0,0x131
lwi,I,0x11,-
lwiu,I,0x31,-
lh,R,0x0,0x12
lhu,R,0x0,0x112
lhs,R,0x0,0x32
lhsu,R,0x0,0x132
lhi,I,0x12,-
lhiu,I,0x32,-
lb,R,0x0,0x13
lbu,R,0x0,0x113
lbi,I,0x13,-
lbiu,I,0x33,-
sd,R,0x0,0x14
sds,R,0x0,0x34
sdi,I,0x14,-
sw,R,0x0,0x15
sws,R,0x0,0x35
swi,I,0x15,-
sh,R,0x0,0x16
shs,R,0x0,0x36
shi,I,0x16,-
sb,R,0x0,0x17
sbi,I,0x17,-
li.0,EI,0x18,0x0
li.1,EI,0x18,0x1
li.2,EI,0x18,0x2
li.3,EI,0x18,0x3
mfhi,EI,0x19,0x0
mflo,EI,0x19,0x1
mthi,EI,0x19,0x2
mtlo,EI,0x19,0x3
la,PSEUDO,-,-
push,PSEUDO,-,-
pop,PSEUDO,-,-
j,J,0x20,-
jal,J,0x21,-
jr,R,0x0,0x18
jalr,R,0x0,0x19
beq,I,0x22,-
bne,I,0x23,-
bge,I,0x24,-
bgeu,I,0x25,-
blt,I,0x26,-
bltu,I,0x27,-
syscall,J,0x28,-
nop,J,0x29,-
ret,PSEUDO,-,-
vld,R,0x7f,0x0
vld.128,R,0x7f,0x1
vld.64,R,0x7f,0x2
vld.32,R,0x7f,0x3
vldhilo,R,0x7f,0x4
vldr,R,0x7f,0x5
vldr.32,R,0x7f,0x6
vldi.64,EI,0x60,0x0
vldiu.64,EI,0x60,0x1
vldi.32,EI,0x60,0x2
vldiu.32,EI,0x60,0x3
vst,R,0x7f,0x8
vst.128,R,0x7f,0x9
vst.64,R,0x7f,0xa
vst.32,R,0x7f,0xb
vsthilo,R,0x7f,0xc
vstr,R,0x7f,0xd
vstr.32,R,0x7f,0xe
vstr.32u,R,0x7f,0xf
vbroad.64,R,0x7f,0x10
vbroad.32,R,0x7f,0x11
vbroadr,R,0x7f,0x12
vbroadr.32,R,0x7f,0x13
vbroadi.64,EI,0x60,0x4
vbroadiu.64,EI,0x60,0x5
vbroadi.32,EI,0x60,0x6
vbroadiu.32,EI,0x60,0x7
vcvti64tof64,R,0x7f,0x14
vcvti64tof64.s,R,0x7f,0x214
vcvtu64tof64,R,0x7f,0x1c
vcvtu64tof64.s,R,0x7f,0x21c
vcvti64tof32,R,0x7f,0x15
vcvti64tof32.s,R,0x7f,0x215
vcvtu64tof32,R,0x7f,0x1d
vcvtu64tof32.s,R,0x7f,0x21d
vcvtf64tof32,R,0x7f,0x16
vcvtf64tof32.s,R,0x7f,0x216
vcvtf32tof64,R,0x7f,0x17
vcvtf32tof64.s,R,0x7f,0x217
vcvtf64toi64,R,0x7f,0x18
vcvtf64toi64.s,R,0x7f,0x218
vcvtf64toi32,R,0x7f,0x19
vcvtf64toi32.s,R,0x7f,0x219
vcvtf32toi64,R,0x7f,0x1a
vcvtf32toi64.s,R,0x7f,0x21a
vcvtf32toi32,R,0x7f,0x1b
vcvtf32toi32.s,R,0x7f,0x21b
vaddi64,R,0x7f,0x20
vaddi32,R,0x7f,0x21
vsubi64,R,0x7f,0x22
vsubi32,R,0x7f,0x23
vaddf64,R,0x7f,0x24
vaddf64.s,R,0x7f,0x224
vaddf32,R,0x7f,0x25
vaddf32.s,R,0x7f,0x225
vsubf64,R,0x7f,0x26
vsubf64.s,R,0x7f,0x226
vsubf32,R,0x7f,0x27
vsubf32.s,R,0x7f,0x227
vmuli32,R,0x7f,0x28
vmulu32,R,0x7f,0x29
vmulf64,R,0x7f,0x2a
vmulf64.s,R,0x7f,0x22a
vmulf32,R,0x7f,0x2b
vmulf32.s,R,0x7f,0x22b
vdivf64,R,0x7f,0x2c
vdivf64.s,R,0x7f,0x22c
vdivf32,R,0x7f,0x2d
vdivf32.s,R,0x7f,0x22d
vand,R,0x7f,0x2e
vor,R,0x7f,0x2f
vxor,R,0x7f,0x30
vslla,R,0x7f,0x31
vsll.64,R,0x7f,0x32
vsll.32,R,0x7f,0x33
vsrla,R,0x7f,0x34
vsrl.64,R,0x7f,0x35
vsrl.32,R,0x7f,0x36
vsra.64,R,0x7f,0x37
vsra.32,R,0x7f,0x38
vsgei64,R,0x7f,0x39
vsgeu64,R,0x7f,0x3a
vsgei32,R,0x7f,0x3b
vsgeu32,R,0x7f,0x3c
vsgef64,R,0x7f,0x3d
vsgef64.s,R,0x7f,0x23d
vsgef32,R,0x7f,0x3e
vsgef32.s,R,0x7f,0x23e
vslti64,R,0x7f,0x3f
vsltu64,R,0x7f,0x40
vslti32,R,0x7f,0x41
vsltu32,R,0x7f,0x42
vsltf64,R,0x7f,0x43
vsltf64.s,R,0x7f,0x243
vsltf32,R,0x7f,0x44
vsltf32.s,R,0x7f,0x244
vextlsb.64,R,0x7f,0x80
vextlsb.32,R,0x7f,0x81
vextmsb.64,R,0x7f,0x82
vextmsb.32,R,0x7f,0x83
vsumi64,R,0x7f,0x84
vsumi32,R,0x7f,0x85
vsumf64,R,0x7f,0x86
vsumf32,R,0x7f,0x87
vsumi32.128,R,0x7f,0x88
vsumf32.128,R,0x7f,0x89
vshuffle.64,R,0x7f,0x8a
vshuffle.32,R,0x7f,0x8b
";

        public static readonly IReadOnlyDictionary<string, Instruction> instructions;

        // Normal instructions
        public static readonly IReadOnlySet<string> rInsts;
        public static readonly IReadOnlySet<string> iInsts;
        public static readonly IReadOnlySet<string> jInsts;
        public static readonly IReadOnlySet<string> eiInsts;
        public static readonly IReadOnlySet<string> pseudoInsts;

        // SIMD instructions
        public static readonly IReadOnlySet<string> vrInsts;
        public static readonly IReadOnlySet<string> veiInsts;

        static InstData()
        {
            string[] instData = InstData.instList.Split(new char[] { ',', '\n' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            Dictionary<string, Instruction> instructions = new Dictionary<string, Instruction>();
            HashSet<uint> opcodesWithNoFunc = new HashSet<uint>();
            Dictionary<uint, HashSet<uint>> opcodesWithFunc = new Dictionary<uint, HashSet<uint>>();

            HashSet<string> rInsts = new HashSet<string>();
            HashSet<string> iInsts = new HashSet<string>();
            HashSet<string> jInsts = new HashSet<string>();
            HashSet<string> eiInsts = new HashSet<string>();
            HashSet<string> pseudoInsts = new HashSet<string>();

            HashSet<string> vrInsts = new HashSet<string>();
            HashSet<string> veiInsts = new HashSet<string>();

            for (int i = 0; i < instData.Length; i += 4)
            {
                string name = instData[i];
                string formatStr = instData[i + 1];
                string opcodeStr = instData[i + 2];
                string funcStr = instData[i + 3];

                InstructionFormat format = InstructionFormat.Unknown;
                switch (formatStr)
                {
                    case "R":
                        format = InstructionFormat.R;
                        break;
                    case "I":
                        format = InstructionFormat.I;
                        break;
                    case "J":
                        format = InstructionFormat.J;
                        break;
                    case "EI":
                        format = InstructionFormat.EI;
                        break;
                    case "PSEUDO":
                        format = InstructionFormat.Pseudo;
                        break;
                    default:
                        throw new Exception($"Unknown instruction format {formatStr}");
                }

                uint opcode = opcodeStr != "-" ? Convert.ToUInt32(opcodeStr, 16) : 0;
                uint func = funcStr != "-" ? Convert.ToUInt32(funcStr, 16) : 0;

                Instruction inst = new Instruction(name, format, opcode, func);

                // Opcode duplication check
                switch (inst.Format)
                {
                    case InstructionFormat.R:
                    case InstructionFormat.EI:
                        if (opcodesWithNoFunc.Contains(inst.Opcode))
                        {
                            throw new Exception("Duplicated opcode");
                        }

                        if (opcodesWithFunc.ContainsKey(inst.Opcode))
                        {
                            var funcs = opcodesWithFunc[inst.Opcode];
                            if (funcs.Contains(inst.Func))
                            {
                                throw new Exception("Duplicated func");
                            }

                            funcs.Add(inst.Func);
                        }
                        else
                        {
                            opcodesWithFunc[inst.Opcode] = new HashSet<uint>();
                            opcodesWithFunc[inst.Opcode].Add(inst.Func);
                        }
                        break;
                    case InstructionFormat.Pseudo:
                        break;
                    case InstructionFormat.I:
                    case InstructionFormat.J:
                        if (opcodesWithNoFunc.Contains(inst.Opcode) ||
                            opcodesWithFunc.ContainsKey(inst.Opcode))
                        {
                            throw new Exception("Duplicated opcode");
                        }
                        opcodesWithNoFunc.Add(inst.Opcode);
                        break;
                    default:
                        throw new Exception("Unknown instruction format");
                }

                if (instructions.ContainsKey(inst.Name))
                {
                    throw new Exception("Duplicated instruction name");
                }

                instructions[inst.Name] = inst;

                switch (inst.Format)
                {
                    case InstructionFormat.R:
                        if (inst.Name[0] == 'v')
                        {
                            vrInsts.Add(inst.Name);
                        }
                        else
                        {
                            rInsts.Add(inst.Name);
                        }
                        break;
                    case InstructionFormat.EI:
                        if (inst.Name[0] == 'v')
                        {
                            veiInsts.Add(inst.Name);
                        }
                        else
                        {
                            eiInsts.Add(inst.Name);
                        }
                        break;
                    case InstructionFormat.Pseudo:
                        pseudoInsts.Add(inst.Name);
                        break;
                    case InstructionFormat.I:
                        iInsts.Add(inst.Name);
                        break;
                    case InstructionFormat.J:
                        jInsts.Add(inst.Name);
                        break;
                    default:
                        throw new Exception("Unknown instruction format");
                }
            }

            InstData.instructions = instructions;
            InstData.rInsts = rInsts;
            InstData.iInsts = iInsts;
            InstData.jInsts = jInsts;
            InstData.eiInsts = eiInsts;
            InstData.pseudoInsts = pseudoInsts;
            InstData.vrInsts = vrInsts;
            InstData.veiInsts = veiInsts;
        }

        public static InstructionFormat GetFormat(string opcode)
        {
            opcode = opcode.ToLower();
            if (rInsts.Contains(opcode) || vrInsts.Contains(opcode))
            {
                return InstructionFormat.R;
            }

            if (iInsts.Contains(opcode))
            {
                return InstructionFormat.I;
            }

            if (jInsts.Contains(opcode))
            {
                return InstructionFormat.J;
            }

            if (eiInsts.Contains(opcode) || veiInsts.Contains(opcode))
            {
                return InstructionFormat.EI;
            }

            if (pseudoInsts.Contains(opcode))
            {
                return InstructionFormat.Pseudo;
            }

            return InstructionFormat.Unknown;
        }
    }
}
