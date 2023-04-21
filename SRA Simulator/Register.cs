namespace SRA_Simulator
{
    public class Register
    {
        private ulong[] registers;
        private ulong pc;
        private ulong hi;
        private ulong lo;

        public Register()
        {
            registers = new ulong[32];
            for (int i = 0; i < registers.Length; i++)
            {
                registers[i] = 0U;
            }

            pc = 0U;
            hi = 0U;
            lo = 0U;
        }

        public ulong this[int i]
        {
            get
            {
                return registers[i];
            }

            internal set
            {
                if (i == 0) // %0 is readonly
                {
                    return;
                }

                registers[i] = value;
            }
        }

        public ulong PC
        {
            get => pc;

            internal set
            {
                pc = value;
            }
        }

        public ulong Hi
        {
            get => hi;

            internal set
            {
                hi = value;
            }
        }

        public ulong Lo
        {
            get => lo;

            internal set
            {
                lo = value;
            }
        }
    }
}
