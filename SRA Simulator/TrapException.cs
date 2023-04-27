using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRA_Simulator
{
    public enum ExcCode : uint
    {
        KernelSoftwareInt = 6 | (1U << 31),
        KernelTimerInt = 7 | (1U << 31),
        KernelExternalInt = 8 | (1U << 31),
        InstAccessFault = 0,
        LoadAccessFault = 1,
        StoreAccessFault = 2,
        InstPageFault = 3,
        LoadPageFault = 4,
        StorePageFault = 5,
        EcallApplication = 6,
        SyscallApplication = 9,
        IllegalInstruction = 12,
    }

    public class TrapException : Exception
    {
        public readonly ExcCode Code;

        public TrapException(string message, ExcCode code) : base(message)
        {
            Code = code;
        }
    }
}
