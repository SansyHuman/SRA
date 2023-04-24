using System.Runtime.InteropServices;

namespace SRA_Assembler
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ELFHeader
    {
        // e_ident indices
        public const int EI_MAG0 = 0;
        public const int EI_MAG1 = 1;
        public const int EI_MAG2 = 2;
        public const int EI_MAG3 = 3;
        public const int EI_CLASS = 4;
        public const int EI_DATA = 5;
        public const int EI_VERSION = 6;
        public const int EI_OSABI = 7;
        public const int EI_ABIVERSION = 8;
        public const int EI_PAD = 9;
        public const int EI_NIDENT = 16;

        public const byte ELFCLASS32 = 1;
        public const byte ELFCLASS64 = 2;

        public const byte ELFDATA2LSB = 1;
        public const byte ELFDATA2MSB = 2;

        // e_type values
        public const ushort ET_NONE = 0;
        public const ushort ET_REL = 1;
        public const ushort ET_EXEC = 2;
        public const ushort ET_DYN = 3;
        public const ushort ET_CORE = 4;
        public const ushort ET_LOOS = 0xfe00;
        public const ushort ET_HIOS = 0xfeff;
        public const ushort ET_LOPROC = 0xff00;
        public const ushort ET_HIPROC = 0xffff;

        public fixed byte e_ident[EI_NIDENT];
        public ushort e_type;
        public ushort e_machine;
        public uint e_version;
        public ulong e_entry;
        public ulong e_phoff;
        public ulong e_shoff;
        public uint e_flags;
        public ushort e_ehsize;
        public ushort e_phentsize;
        public ushort e_phnum;
        public ushort e_shentsize;
        public ushort e_shnum;
        public ushort e_shstrndx;

        public ELFHeader()
        {
            e_ident[EI_MAG0] = 0x7f;
            e_ident[EI_MAG1] = (byte)'E';
            e_ident[EI_MAG2] = (byte)'L';
            e_ident[EI_MAG3] = (byte)'F';
            e_ident[EI_CLASS] = ELFCLASS64;
            e_ident[EI_DATA] = ELFDATA2LSB;
            e_ident[EI_VERSION] = 1;
            e_ident[EI_OSABI] = 0;
            e_ident[EI_ABIVERSION] = 0;
            for (int i = EI_PAD; i < EI_NIDENT; i++)
            {
                e_ident[i] = 0;
            }

            e_type = 0;
            e_machine = 0;
            e_version = 1;
            e_entry = 0;
            e_phoff = 0;
            e_shoff = 0;
            e_flags = 0;
            e_ehsize = (ushort)Marshal.SizeOf<ELFHeader>();
            e_phentsize = (ushort)Marshal.SizeOf<ProgramHeader>();
            e_phnum = 0;
            e_shentsize = 0;
            e_shnum = 0;
            e_shstrndx = 0;
        }

        public bool CheckMagicNumber()
        {
            return e_ident[EI_MAG0] == 0x7f &&
            e_ident[EI_MAG1] == (byte)'E' &&
            e_ident[EI_MAG2] == (byte)'L' &&
            e_ident[EI_MAG3] == (byte)'F';
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ProgramHeader
    {
        // p_type values
        public const uint PT_NULL = 0;
        public const uint PT_LOAD = 1;
        public const uint PT_DYNAMIC = 2;
        public const uint PT_INTERP = 3;
        public const uint PT_NOTE = 4;
        public const uint PT_SHLIB = 5;
        public const uint PT_PHDR = 6;
        public const uint PT_LOOS = 0x60000000;
        public const uint PT_HIOS = 0x6fffffff;
        public const uint PT_LOPROC = 0x70000000;
        public const uint PT_HIPROC = 0x7fffffff;
        public const uint PT_RELOCTABLE = PT_LOPROC;
        public const uint PT_SYMBOLTABLE = PT_RELOCTABLE + 1;
        public const uint PT_KLOADSTART = PT_SYMBOLTABLE + 1;
        public const uint PT_KLOAD = PT_KLOADSTART + 1;

        // p_flags flags
        public const uint PF_X = 0x1;
        public const uint PF_W = 0x2;
        public const uint PF_R = 0x4;

        public uint p_type;
        public uint p_flags;
        public ulong p_offset;
        public ulong p_vaddr;
        public ulong p_paddr;
        public ulong p_filesz;
        public ulong p_memsz;
        public ulong p_align;
    }

    public static class HeaderUtils
    {
        public static byte[] GetBytes<T>(T header) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            byte[] arr = new byte[size];

            IntPtr ptr = IntPtr.Zero;

            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(header, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return arr;
        }

        public static T FromBytes<T>(byte[] arr) where T : unmanaged
        {
            T header = new();

            int size = Marshal.SizeOf<T>();
            IntPtr ptr = IntPtr.Zero;

            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(arr, 0, ptr, size);
                header = Marshal.PtrToStructure<T>(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return header;
        }
    }
}
