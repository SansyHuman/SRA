﻿using SRA_Assembler;

using System.Net.Mail;
using System;
using System.Runtime.Intrinsics;
using System.Text;

namespace SRA_Simulator
{
    [Flags]
    public enum MemoryAccess : uint
    {
        None = 0,
        Execute = ProgramHeader.PF_X,
        Write = ProgramHeader.PF_W,
        Read = ProgramHeader.PF_R,
    }

    // little-endian virtual memory
    public class VirtualMemory
    {
        public class Segment
        {
            private List<byte> memory;

            public readonly ulong MinAddress;
            private ulong maxAddress;
            public readonly MemoryAccess Access;

            private byte[] tmpMem = new byte[32];

            public IReadOnlyList<byte> Memory => memory;

            public ulong MaxAddress
            {
                get => maxAddress;
                internal set => maxAddress = value;
            }

            public ulong Size
            {
                get => MaxAddress - MinAddress + 1;
            }

            public Segment(ulong startAddress, int maxSize, int initialSize, MemoryAccess access)
            {
                if (maxSize < 0)
                {
                    throw new ArgumentException("Negative max size", nameof(maxSize));
                }

                if (initialSize > maxSize)
                {
                    initialSize = maxSize;
                }

                memory = new List<byte>(new byte[initialSize]);

                MinAddress = startAddress;
                MaxAddress = MinAddress + (ulong)maxSize - 1;
                Access = access;
            }

            public Segment(ulong startAddress, int maxSize, byte[] initialData, MemoryAccess access)
            {
                if (maxSize < initialData.Length)
                {
                    throw new ArgumentException("Less max size than initial data size", nameof(maxSize));
                }

                memory = new List<byte>(initialData);

                MinAddress = startAddress;
                MaxAddress = MinAddress + (ulong)maxSize - 1;
                Access = access;
            }

            // Set the minimal size of allocated memory to be larger than the capacity
            public void SetCapacity(int capacity)
            {
                if (memory.Count < capacity)
                {
                    if (memory.Capacity < capacity)
                    {
                        memory.Capacity = capacity * 2;
                    }
                    int additionalCapacity = capacity - memory.Count;
                    memory.AddRange(new byte[additionalCapacity]);
                }
            }

            public byte this[ulong address]
            {
                get
                {
                    if (!Access.HasFlag(MemoryAccess.Read))
                    {
                        throw new Exception("Segmentation fault: cannot read this segment");
                    }

                    if (address < MinAddress | address > MaxAddress)
                    {
                        throw new Exception("Segmentation fault: memory address out of range");
                    }

                    int realAddr = (int)(address - MinAddress);
                    SetCapacity(realAddr + 1);

                    return memory[realAddr];
                }
                set
                {
                    if (!Access.HasFlag(MemoryAccess.Write))
                    {
                        throw new Exception("Segmentation fault: cannot write to this segment");
                    }

                    if (address < MinAddress | address > MaxAddress)
                    {
                        throw new Exception("Segmentation fault: memory address out of range");
                    }

                    int realAddr = (int)(address - MinAddress);
                    SetCapacity(realAddr + 1);

                    memory[realAddr] = value;
                }
            }

            public ulong GetDword(ulong address)
            {
                if (!Access.HasFlag(MemoryAccess.Read))
                {
                    throw new Exception("Segmentation fault: cannot read this segment");
                }

                if (address < MinAddress | address + 7 > MaxAddress)
                {
                    throw new Exception("Segmentation fault: memory address out of range");
                }

                int realAddr = (int)(address - MinAddress);
                SetCapacity(realAddr + 8);

                memory.CopyTo(realAddr, tmpMem, 0, 8);
                return BitConverter.ToUInt64(tmpMem, 0);
            }

            public void SetDword(ulong address, ulong value)
            {
                if (!Access.HasFlag(MemoryAccess.Write))
                {
                    throw new Exception("Segmentation fault: cannot write this segment");
                }

                if (address < MinAddress | address + 7 > MaxAddress)
                {
                    throw new Exception("Segmentation fault: memory address out of range");
                }

                int realAddr = (int)(address - MinAddress);
                SetCapacity(realAddr + 8);

                byte[] bin = BitConverter.GetBytes(value);
                for (int i = 0; i < 8; i++)
                {
                    memory[realAddr + i] = bin[i];
                }
            }

            public uint GetWord(ulong address)
            {
                if (!Access.HasFlag(MemoryAccess.Read))
                {
                    throw new Exception("Segmentation fault: cannot read this segment");
                }

                if (address < MinAddress | address + 3 > MaxAddress)
                {
                    throw new Exception("Segmentation fault: memory address out of range");
                }

                int realAddr = (int)(address - MinAddress);
                SetCapacity(realAddr + 4);

                memory.CopyTo(realAddr, tmpMem, 0, 4);
                return BitConverter.ToUInt32(tmpMem, 0);
            }

            public void SetWord(ulong address, uint value)
            {
                if (!Access.HasFlag(MemoryAccess.Write))
                {
                    throw new Exception("Segmentation fault: cannot write this segment");
                }

                if (address < MinAddress | address + 3 > MaxAddress)
                {
                    throw new Exception("Segmentation fault: memory address out of range");
                }

                int realAddr = (int)(address - MinAddress);
                SetCapacity(realAddr + 4);

                byte[] bin = BitConverter.GetBytes(value);
                for (int i = 0; i < 4; i++)
                {
                    memory[realAddr + i] = bin[i];
                }
            }

            public ushort GetHalf(ulong address)
            {
                if (!Access.HasFlag(MemoryAccess.Read))
                {
                    throw new Exception("Segmentation fault: cannot read this segment");
                }

                if (address < MinAddress | address + 1 > MaxAddress)
                {
                    throw new Exception("Segmentation fault: memory address out of range");
                }

                int realAddr = (int)(address - MinAddress);
                SetCapacity(realAddr + 2);

                memory.CopyTo(realAddr, tmpMem, 0, 2);
                return BitConverter.ToUInt16(tmpMem, 0);
            }

            public void SetHalf(ulong address, ushort value)
            {
                if (!Access.HasFlag(MemoryAccess.Write))
                {
                    throw new Exception("Segmentation fault: cannot write this segment");
                }

                if (address < MinAddress | address + 1 > MaxAddress)
                {
                    throw new Exception("Segmentation fault: memory address out of range");
                }

                int realAddr = (int)(address - MinAddress);
                SetCapacity(realAddr + 2);

                byte[] bin = BitConverter.GetBytes(value);
                for (int i = 0; i < 2; i++)
                {
                    memory[realAddr + i] = bin[i];
                }
            }

            public uint GetInstruction(ulong pc)
            {
                if (!Access.HasFlag(MemoryAccess.Execute))
                {
                    throw new Exception("Segmentation fault: cannot execute this segment");
                }

                return GetWord(pc);
            }

            public Vector256<T> GetVector256<T>(ulong address) where T : struct
            {
                if (!Access.HasFlag(MemoryAccess.Read))
                {
                    throw new Exception("Segmentation fault: cannot read this segment");
                }

                if (address < MinAddress | address + 31 > MaxAddress)
                {
                    throw new Exception("Segmentation fault: memory address out of range");
                }

                int realAddr = (int)(address - MinAddress);
                SetCapacity(realAddr + 32);

                memory.CopyTo(realAddr, tmpMem, 0, 32);
                return Vector256.Create(tmpMem, 0).As<byte, T>();
            }

            public void SetVector256<T>(ulong address, Vector256<T> value) where T : struct
            {
                if (!Access.HasFlag(MemoryAccess.Write))
                {
                    throw new Exception("Segmentation fault: cannot write this segment");
                }

                if (address < MinAddress | address + 31 > MaxAddress)
                {
                    throw new Exception("Segmentation fault: memory address out of range");
                }

                int realAddr = (int)(address - MinAddress);
                SetCapacity(realAddr + 32);

                value.AsByte().CopyTo(tmpMem, 0);
                for (int i = 0; i < 32; i++)
                {
                    memory[realAddr + i] = tmpMem[i];
                }
            }

            public Vector128<T> GetVector128<T>(ulong address) where T : struct
            {
                if (!Access.HasFlag(MemoryAccess.Read))
                {
                    throw new Exception("Segmentation fault: cannot read this segment");
                }

                if (address < MinAddress | address + 15 > MaxAddress)
                {
                    throw new Exception("Segmentation fault: memory address out of range");
                }

                int realAddr = (int)(address - MinAddress);
                SetCapacity(realAddr + 16);

                memory.CopyTo(realAddr, tmpMem, 0, 16);
                return Vector128.Create(tmpMem, 0).As<byte, T>();
            }

            public void SetVector128<T>(ulong address, Vector128<T> value) where T : struct
            {
                if (!Access.HasFlag(MemoryAccess.Write))
                {
                    throw new Exception("Segmentation fault: cannot write this segment");
                }

                if (address < MinAddress | address + 15 > MaxAddress)
                {
                    throw new Exception("Segmentation fault: memory address out of range");
                }

                int realAddr = (int)(address - MinAddress);
                SetCapacity(realAddr + 16);

                value.AsByte().CopyTo(tmpMem, 0);
                for (int i = 0; i < 16; i++)
                {
                    memory[realAddr + i] = tmpMem[i];
                }
            }

            public void Copy(byte[] dst, ulong address)
            {
                if (!Access.HasFlag(MemoryAccess.Read))
                {
                    throw new Exception("Segmentation fault: cannot read this segment");
                }

                if (address < MinAddress | address + (ulong)(long)dst.Length - 1 > MaxAddress)
                {
                    throw new Exception("Segmentation fault: memory address out of range");
                }

                int realAddr = (int)(address - MinAddress);
                SetCapacity(realAddr + dst.Length);

                memory.CopyTo(realAddr, dst, 0, dst.Length);
            }
        }

        internal readonly Segment[] segments;

        public VirtualMemory(BinaryReader program, params ProgramHeader[] segmentInfos)
        {
            segments = new Segment[segmentInfos.Length];

            for (int i = 0; i < segmentInfos.Length; i++)
            {
                ref ProgramHeader header = ref segmentInfos[i];

                if (header.p_align > 0)
                {
                    if (header.p_vaddr % header.p_align != 0)
                    {
                        throw new Exception("The start address of the segment is not aligned.");
                    }
                }

                if (header.p_filesz > 0)
                {
                    program.BaseStream.Position = (long)header.p_offset;
                    byte[] bin = program.ReadBytes((int)header.p_filesz);

                    segments[i] = new Segment(header.p_vaddr, (int)header.p_memsz, bin, (MemoryAccess)header.p_flags);
                }
                else
                {
                    segments[i] = new Segment(header.p_vaddr, (int)header.p_memsz, 0, (MemoryAccess)header.p_flags);
                }
            }

            Array.Sort(segments, (s1, s2) =>
            {
                if (s1.MinAddress < s2.MinAddress)
                    return -1;
                else if (s1.MinAddress == s2.MinAddress)
                    return 0;
                else
                    return 1;
            });

            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (segments[i].MaxAddress >= segments[i + 1].MinAddress)
                {
                    throw new Exception("Two segments' virtual address overlap");
                }
            }
        }

        public Segment GetSegment(ulong vaddr)
        {
            for (int i = 0; i < segments.Length; i++)
            {
                Segment seg = segments[i];
                if (vaddr >= seg.MinAddress && vaddr <= seg.MaxAddress)
                    return seg;
            }

            throw new Exception("Segmentation fault: out of segment address range");
        }

        private Segment GetSegmentUnsafe(ulong vaddr)
        {
            for (int i = 0; i < segments.Length; i++)
            {
                Segment seg = segments[i];
                if (vaddr >= seg.MinAddress && vaddr <= seg.MaxAddress)
                    return seg;
            }

            return null;
        }

        private bool GetByteUnsafe(ulong vaddr, out byte value)
        {
            Segment segment = GetSegmentUnsafe(vaddr);
            if (segment == null)
            {
                value = 0;
                return false;
            }

            value = segment[vaddr];
            return true;
        }

        public string GetMemoryString(ulong startAddr, ulong row, ulong column)
        {
            StringBuilder str = new StringBuilder();

            for (ulong i = 0; i < row; i++)
            {
                str.Append($"0x{startAddr + i * column:x16}\t");
                for (ulong j = 0; j < column; j++)
                {
                    ulong addr = startAddr + i * column + j;
                    bool valid = GetByteUnsafe(addr, out byte value);
                    str.Append(valid ? $"{value:x2} " : "?? ");
                }
                str.AppendLine();
            }

            return str.ToString();
        }

        public byte this[ulong vaddr]
        {
            get
            {
                return GetSegment(vaddr)[vaddr];
            }
            set
            {
                GetSegment(vaddr)[vaddr] = value;
            }
        }

        public ulong GetDword(ulong vaddr)
        {
            return GetSegment(vaddr).GetDword(vaddr);
        }

        public void SetDword(ulong vaddr, ulong value)
        {
            GetSegment(vaddr).SetDword(vaddr, value);
        }

        public uint GetWord(ulong vaddr)
        {
            return GetSegment(vaddr).GetWord(vaddr);
        }

        public void SetWord(ulong vaddr, uint value)
        {
            GetSegment(vaddr).SetWord(vaddr, value);
        }

        public ushort GetHalf(ulong vaddr)
        {
            return GetSegment(vaddr).GetHalf(vaddr);
        }

        public void SetHalf(ulong vaddr, ushort value)
        {
            GetSegment(vaddr).SetHalf(vaddr, value);
        }

        public uint GetInstruction(ulong pc)
        {
            return GetSegment(pc).GetInstruction(pc);
        }

        public Vector256<T> GetVector256<T>(ulong vaddr) where T : struct
        {
            return GetSegment(vaddr).GetVector256<T>(vaddr);
        }

        public void SetVector256<T>(ulong vaddr, Vector256<T> value) where T : struct
        {
            GetSegment(vaddr).SetVector256(vaddr, value);
        }

        public Vector128<T> GetVector128<T>(ulong vaddr) where T : struct
        {
            return GetSegment(vaddr).GetVector128<T>(vaddr);
        }

        public void SetVector128<T>(ulong vaddr, Vector128<T> value) where T : struct
        {
            GetSegment(vaddr).SetVector128(vaddr, value);
        }

        public void Copy(byte[] dst, ulong vaddr)
        {
            GetSegment(vaddr).Copy(dst, vaddr);
        }
    }
}
