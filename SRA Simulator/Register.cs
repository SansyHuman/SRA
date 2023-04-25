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

        public ulong EPC
        {
            get => registers[1];
            internal set
            {
                registers[1] = value;
            }
        }

        public uint IE
        {
            get => (uint)registers[2];
            internal set
            {
                registers[2] = value;
            }
        }

        public uint IP
        {
            get => (uint)registers[3];
            internal set
            {
                registers[3] = value;
            }
        }

        public uint Cause
        {
            get => (uint)registers[4];
            internal set
            {
                registers[4] = value;
            }
        }

        public ulong Time
        {
            get => registers[5];
            internal set
            {
                registers[5] = value;
            }
        }

        public ulong TimeCmp
        {
            get => registers[6];
            internal set
            {
                registers[6] = value;
            }
        }
    }
}
