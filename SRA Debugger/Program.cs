using SRA_Simulator;

[assembly: System.Reflection.AssemblyVersion("0.0.*")]

namespace SRA_Debugger
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            /*
            CPU cpu = new CPU("./test/testprogram", 1024);
            string[] asm = Disassembler.Disassemble(cpu.TextBinary, cpu.TextSegmentStart);
            string textMem = cpu.GetMemoryString(cpu.TextSegmentStart, 4, 16);
            string dataMem = cpu.GetMemoryString(0x10_0000_0000, 4, 16);
            string invalidMem = cpu.GetMemoryString(cpu.TextSegmentStart - 24, 4, 16);
            */

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}