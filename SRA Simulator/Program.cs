[assembly: System.Reflection.AssemblyVersion("0.0.*")]

namespace SRA_Simulator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                Console.WriteLine("Usage: ./srasim input <-stack=size>");
                Console.WriteLine("size is the size of the stack in bytes. The number can have suffix K or M");
                Console.WriteLine("The default stack size is 2MB");
                return;
            }

            string input = string.Empty;
            string stackSize = string.Empty;

            ulong stack = 2UL * 1024 * 1024;

            input = args[0];
            if (args.Length == 2)
            {
                stackSize = args[1];
                if (stackSize.IndexOf("-stack=") == -1)
                {
                    Console.WriteLine("Usage: ./srasim input <-stack=size>");
                    return;
                }
                stackSize = stackSize.Substring(7).ToLower();
                if (stackSize[stackSize.Length - 1] == 'k')
                {
                    stack = Convert.ToUInt64(stackSize.Substring(0, stackSize.Length - 1), 10) * 1024;
                }
                else if (stackSize[stackSize.Length - 1] == 'm')
                {
                    stack = Convert.ToUInt64(stackSize.Substring(0, stackSize.Length - 1), 10) * 1024 * 1024;
                }
                else
                {
                    stack = Convert.ToUInt64(stackSize.Substring(0, stackSize.Length), 10);
                }
            }

            try
            {
                CPU cpu = new CPU(input, stack);

                cpu.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}