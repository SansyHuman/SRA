[assembly: System.Reflection.AssemblyVersion("0.0.*")]

namespace SRA_Assembler
{
    internal class Program
    {
        static string[] instSamples = new string[]
        {
            "add %t0, %zero, %t1",
            "addw %t1, %zero, %t2",
            "addi %s0, %t3, -1",
            "addiu %s1, %t3, 0xfc",
            "addiw %s4, %s5, 0b1101",
            "sub %t8, %sp, %fp",
            "subw %t7, %gp, %ra",
            "mul %s5, %s8",
            "mulu %s7, %t3",
            "div %sp, %zero",
            "divu %fp, %gp",
            "mulw %s5, %s8, %t0",
            "muluw %t1, %t0, %t0",
            "and %s2, %t3, %t9",
            "or %s0, %t7, %t4",
            "xor %s5, %t2, %t1",
            "andi %s2, %t3, 07223",
            "ori %s5, %s5, 352",
            "xori %s1, %t0, 0x10c",
            "sll %sp, %sp, %t0",
            "sllw %gp, %fp, %t2",
            "srl %sp, %sp, %t0",
            "srlw %gp, %fp, %t2",
            "sra %t5, %s0, %t3",
            "sraw %t2, %asm, %t4",
            "slt %s5, %t0, %t1",
            "sltu %s5, %t4, %t8",
            "slti %s5, %t0, -325",
            "sltiu %s5, %t0, 7512",

            "ld %t0, %s0, %t2",
            "lds %t2, %s0, %s1",
            "ldi %t3, [%t1]-24",
            "lw %t3, %gp, %s3",
            "lwu %t4, %sp, %s1",
            "lws %asm, %fp, %s8",
            "lwsu %s0, %sp, %t0",
            "lwi %t3, [%gp]0XFC0",
            "lwiu %t7, [%fp]+32",
            "lh %t3, %gp, %s3",
            "lhu %t4, %sp, %s1",
            "lhs %asm, %fp, %s8",
            "lhsu %s0, %sp, %t0",
            "lhi %t3, [%zero]0705",
            "lhiu %t7, [%t2]0x1CD",
            "lb %t3, %gp, %s3",
            "lbu %t4, %sp, %s1",
            "lbi %t3, [%zero]0705",
            "lbiu %t7, [%t2]0x1CD",
            "sd %t0, %s0, %t2",
            "sds %t2, %s0, %s1",
            "sdi %t3, [%t1]-24",
            "sw %t3, %gp, %s3",
            "sws %asm, %fp, %s8",
            "swi %t3, [%gp]0XFC0",
            "sh %t3, %gp, %s3",
            "shs %asm, %fp, %s8",
            "shi %t3, [%zero]0705",
            "sb %t3, %gp, %s3",
            "sbi %t3, [%zero]0705",
            "li.0 %gp, 0b1100111100110010",
            "li.1 %gp, 05470",
            "li.2 %gp, -14565",
            "li.3 %gp, 0xcc35",
            "mfhi %t0",
            "mflo %t1",
            "mthi %t2",
            "mtlo %t3",

            "j fibonacci",
            "jal sum",
            "jr %ra",
            "jalr %t6",
            "beq %t0, %zero, -4",
            "bne %t1, %s0, loop_start",
            "bge %t2, %zero, 0x22f",
            "bgeu %t3, %asm, 0172",
            "blt %t2, %zero, fibonacci",
            "bltu %t3, %asm, main_loop",
            "syscall",
            "nop",

            "vld %v1, %ret, %zero",
            "vld.128 %v0, %gp, %t0",
            "vld.64 %v4, %sp, %t3",
            "vld.32 %v31, %fp, %t2",
            "vldhilo %v25",
            "vldr %v9, %s3",
            "vldr.32 %v10, %ret",
            "vldi.64 %v15, -352",
            "vldiu.64 %v12, 051235",
            "vldi.32 %v2, 0b00101111",
            "vldiu.32 %v23, 0x10cc",
            "vst %v1, %ret, %zero",
            "vst.128 %v0, %gp, %t0",
            "vst.64 %v4, %sp, %t3",
            "vst.32 %v31, %fp, %t2",
            "vsthilo %v25",
            "vstr %v9, %s3",
            "vstr.32 %v10, %ret",
            "vstr.32u %v17, %t2",
            "vbroad.64 %v6, %fp, %t2",
            "vbroad.32 %v7, %gp, %t5",
            "vbroadr %v20, %s5",
            "vbroadr.32 %v7, %s7",
            "vbroadi.64 %v2, -1",
            "vbroadiu.64 %v1, 0x55a",
            "vbroadi.32 %v30, 0b1101",
            "vbroadiu.32 %v29, 011",

            "vcvti64tof64 %v29, %v29",
            "vcvti64tof64.s %v12, %v1",
            "vcvtu64tof64 %v1, %v2",
            "vcvtu64tof64.s %v4, %v5",
            "vcvti64tof32 %v29, %v29",
            "vcvti64tof32.s %v12, %v1",
            "vcvtu64tof32 %v1, %v2",
            "vcvtu64tof32.s %v4, %v5",
            "vcvtf64tof32 %v7, %v4",
            "vcvtf64tof32.s %v2, %v5",
            "vcvtf32tof64 %v7, %v4",
            "vcvtf32tof64.s %v2, %v5",
            "vcvtf64toi64 %v8, %v5, %s0",
            "vcvtf64toi64.s %v2, %v25, %s3",
            "vcvtf64toi32 %v8, %v5, %s0",
            "vcvtf64toi32.s %v2, %v25, %s3",
            "vcvtf32toi64 %v8, %v5, %s0",
            "vcvtf32toi64.s %v2, %v25, %s3",
            "vcvtf32toi32 %v8, %v5, %s0",
            "vcvtf32toi32.s %v2, %v25, %s3",

            "vaddi64 %v5, %v5, %v6",
            "vaddi32 %v1, %v8, %v19",
            "vsubi64 %v7, %v31, %v19",
            "vsubi32 %v12, %v20, %v4",
            "vaddf64 %v5, %v5, %v6",
            "vaddf64.s %v7, %v1, %v0",
            "vaddf32 %v5, %v5, %v6",
            "vaddf32.s %v7, %v1, %v0",
            "vsubf64 %v5, %v5, %v6",
            "vsubf64.s %v7, %v1, %v0",
            "vsubf32 %v5, %v5, %v6",
            "vsubf32.s %v7, %v1, %v0",
            "vmuli32 %v1, %v8, %v19",
            "vmulu32 %v5, %v0, %v31",
            "vmulf64 %v5, %v5, %v6",
            "vmulf64.s %v7, %v1, %v0",
            "vmulf32 %v5, %v5, %v6",
            "vmulf32.s %v7, %v1, %v0",
            "vdivf64 %v5, %v5, %v6",
            "vdivf64.s %v7, %v1, %v0",
            "vdivf32 %v5, %v5, %v6",
            "vdivf32.s %v7, %v1, %v0",
            "vand %v5, %v5, %v6",
            "vor %v30, %v20, %v10",
            "vxor %v3, %v2, %v1",
            "vslla %v15, %v16, %s7",
            "vsll.64 %v1, %v18, %v2",
            "vsll.32 %v9, %v0, %v4",
            "vsrla %v15, %v16, %s7",
            "vsrl.64 %v1, %v18, %v2",
            "vsrl.32 %v9, %v0, %v4",
            "vsra.64 %v1, %v18, %v2",
            "vsra.32 %v9, %v0, %v4",
            "vsgei64 %v13, %v12, %v15",
            "vsgeu64 %v8, %v6, %v0",
            "vsgei32 %v13, %v12, %v15",
            "vsgeu32 %v8, %v6, %v0",
            "vsgef64 %v13, %v12, %v15",
            "vsgef64.s %s0, %v6, %v1",
            "vsgef32 %v13, %v12, %v15",
            "vsgef32.s %s0, %v6, %v1",
            "vslti64 %v13, %v12, %v15",
            "vsltu64 %v8, %v6, %v0",
            "vslti32 %v13, %v12, %v15",
            "vsltu32 %v8, %v6, %v0",
            "vsltf64 %v13, %v12, %v15",
            "vsltf64.s %s0, %v6, %v1",
            "vsltf32 %v13, %v12, %v15",
            "vsltf32.s %s0, %v6, %v1",

            "vextlsb.64 %ret, %v5",
            "vextlsb.32 %t0, %v6",
            "vextmsb.64 %ret, %v5",
            "vextmsb.32 %t0, %v6",
            "vsumi64 %t5, %v15",
            "vsumi32 %s7, %v16",
            "vsumf64 %v1, %v15",
            "vsumf32 %v0, %v16",
            "vsumi32.128 %s7, %v16",
            "vsumf32.128 %v0, %v16",
            "vshuffle.64 %v5, %v2, %s5",
            "vshuffle.32 %v16, %v7, %s1",

            "krr %t0, %cause",
            "krw %epc, %t2",
            "eret",
            "ecall",
        };

        static void Main(string[] args)
        {
            /*
            try
            {
                for (int i = 0; i < instSamples.Length; i++)
                {
                    var inst = new InstructionSyntax(instSamples[i]);
                    Console.WriteLine(inst.ToString());
                    Console.WriteLine($"\t\t0x{inst.ToBinary():x8}");
                }
            }
            catch (Exception e)
            { 
                Console.WriteLine(e.Message);
            }
            */

            // InstData.GetFormat("krr");
            // Assembler.AssembleObj("testinput.s", "testinput.o");

            List<string> inputs = new List<string>();
            string option = string.Empty;
            string output = string.Empty;

            if (args.Length == 0)
            {
                Console.WriteLine($"Usage: ./sraasm -a [sources]");
                Console.WriteLine($"Usage: ./sraasm -o output [objects]");
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-a")
                {
                    option = args[i];
                }
                else if (args[i] == "-o")
                {
                    option = args[i];
                    if (i + 1 > args.Length - 1)
                    {
                        Console.WriteLine($"Usage: ./sraasm -o output [objects]");
                        return;
                    }
                    output = args[i + 1];
                    i++;
                }
                else
                {
                    inputs.Add(args[i]);
                }
            }

            if (inputs.Count == 0)
            {
                if (option == "-a")
                {
                    Console.WriteLine($"Usage: ./sraasm -a [sources]");
                }
                else if (option == "-o")
                {
                    Console.WriteLine($"Usage: ./sraasm -o output [objects]");
                }
                else
                {
                    Console.WriteLine($"Usage: ./sraasm -a [sources]");
                    Console.WriteLine($"Usage: ./sraasm -o output [objects]");
                }

                return;
            }

            if (option == "-a")
            {
                for (int i = 0; i < inputs.Count; i++)
                {
                    try
                    {
                        string input = inputs[i];
                        int extensionPos = input.LastIndexOf('.');
                        output = input.Remove(extensionPos + 1) + "o";

                        Assembler.AssembleObj(input, output);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine($"Could not assemble file {inputs[i]}");
                        return;
                    }
                }
            }
            else if (option == "-o")
            {
                try
                {
                    Linker.Link(inputs.ToArray(), output);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
            }

            return;
        }
    }
}