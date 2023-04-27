using System.Runtime.InteropServices;
using System.Text;

namespace SRA_Assembler
{
    public struct ObjFile
    {
        public byte[] Text;
        public byte[] Data;
        public byte[] KText;
        public byte[] KData;
        public bool IsKTextStart;
        public ulong KTextStartAddr;
        public List<RelocationTableElement> RelocTable;
        public Dictionary<string, SymbolTableElement> SymbolTable;
        public HashSet<string> GlobalSymbols;
        public ulong Entrypoint;
    }

    public static class Linker
    {
        static unsafe void CheckELFHeader(in ELFHeader header, string inputPath)
        {
            if (!header.CheckMagicNumber())
            {
                throw new Exception($"Error: input file {inputPath} is not an ELF file.");
            }

            if (header.e_ident[ELFHeader.EI_CLASS] != ELFHeader.ELFCLASS64)
            {
                throw new Exception($"Error: input file {inputPath} is not a 64-bit object file.");
            }

            if (header.e_ident[ELFHeader.EI_DATA] != ELFHeader.ELFDATA2LSB)
            {
                throw new Exception($"Error: input file {inputPath} is not a little endian object file.");
            }

            if (header.e_ident[ELFHeader.EI_VERSION] != 1 ||
                header.e_version != 1)
            {
                throw new Exception($"Error: input file {inputPath} has wrong ELF header version.");
            }

            if (header.e_type != ELFHeader.ET_REL)
            {
                throw new Exception($"Error: input file {inputPath} is not an object file.");
            }

            if (header.e_phnum != 6)
            {
                throw new Exception($"Error: input file {inputPath} has wrong number of segments.");
            }

            if (header.e_ehsize != (ushort)Marshal.SizeOf<ELFHeader>())
            {
                throw new Exception($"Error: input file {inputPath} has wrong ELF header size.");
            }

            if (header.e_phentsize != (ushort)Marshal.SizeOf<ProgramHeader>())
            {
                throw new Exception($"Error: input file {inputPath} has wrong program header size.");
            }
        }

        public static ObjFile LoadObject(string path)
        {
            ObjFile objFile = new ObjFile();

            int elfHeaderSize = Marshal.SizeOf<ELFHeader>();
            int programHeaderSize = Marshal.SizeOf<ProgramHeader>();

            using (FileStream fin = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fin))
                {
                    byte[] elfBin = br.ReadBytes(elfHeaderSize);
                    ELFHeader elfHeader = HeaderUtils.FromBytes<ELFHeader>(elfBin);
                    CheckELFHeader(elfHeader, path);

                    objFile.Entrypoint = elfHeader.e_entry;

                    ulong programHeaderStart = elfHeader.e_phoff;

                    ProgramHeader? textHeader = null;
                    ProgramHeader? dataHeader = null;
                    ProgramHeader? ktextHeader = null;
                    ProgramHeader? kdataHeader = null;
                    ProgramHeader? relocHeader = null;
                    ProgramHeader? symHeader = null;

                    bool isKTextStart = false;

                    byte[] phBin = null;
                    fin.Position = (long)programHeaderStart;
                    for (int i = 0; i < elfHeader.e_phnum; i++)
                    {
                        phBin = br.ReadBytes(programHeaderSize);
                        ProgramHeader pHeader = HeaderUtils.FromBytes<ProgramHeader>(phBin);
                        if (pHeader.p_type == ProgramHeader.PT_LOAD)
                        {
                            if (pHeader.p_flags == (ProgramHeader.PF_X | ProgramHeader.PF_R))
                            {
                                if (textHeader.HasValue)
                                {
                                    throw new Exception($"Error: input file {path} has multiple text segment.");
                                }
                                textHeader = pHeader;
                            }
                            else if (pHeader.p_flags == (ProgramHeader.PF_R | ProgramHeader.PF_W))
                            {
                                if (dataHeader.HasValue)
                                {
                                    throw new Exception($"Error: input file {path} has multiple data segment.");
                                }
                                dataHeader = pHeader;
                            }
                            else
                            {
                                throw new Exception($"Error: input file {path} has wrong loadable segment flags.");
                            }
                        }
                        else if (pHeader.p_type == ProgramHeader.PT_KLOAD)
                        {
                            if (pHeader.p_flags == (ProgramHeader.PF_X | ProgramHeader.PF_R))
                            {
                                if (ktextHeader.HasValue)
                                {
                                    throw new Exception($"Error: input file {path} has multiple ktext segment.");
                                }
                                ktextHeader = pHeader;
                            }
                            else if (pHeader.p_flags == (ProgramHeader.PF_R | ProgramHeader.PF_W))
                            {
                                if (kdataHeader.HasValue)
                                {
                                    throw new Exception($"Error: input file {path} has multiple kdata segment.");
                                }
                                kdataHeader = pHeader;
                            }
                            else
                            {
                                throw new Exception($"Error: input file {path} has wrong loadable segment flags.");
                            }
                        }
                        else if (pHeader.p_type == ProgramHeader.PT_KLOADSTART)
                        {
                            if (pHeader.p_flags != (ProgramHeader.PF_X | ProgramHeader.PF_R))
                            {
                                throw new Exception($"Error: input file {path} has wrong ktext segment flag.");
                            }

                            if (ktextHeader.HasValue)
                            {
                                throw new Exception($"Error: input file {path} has multiple ktext segment.");
                            }
                            ktextHeader = pHeader;
                            isKTextStart = true;
                        }
                        else if (pHeader.p_type == ProgramHeader.PT_RELOCTABLE)
                        {
                            if (relocHeader.HasValue)
                            {
                                throw new Exception($"Error: input file {path} has multiple relocation segment.");
                            }
                            relocHeader = pHeader;
                        }
                        else if (pHeader.p_type == ProgramHeader.PT_SYMBOLTABLE)
                        {
                            if (symHeader.HasValue)
                            {
                                throw new Exception($"Error: input file {path} has multiple symbol segment.");
                            }
                            symHeader = pHeader;
                        }
                        else
                        {
                            throw new Exception($"Error: input file {path} has unknown type of segment.");
                        }
                    }

                    ulong textStart = textHeader.Value.p_offset;
                    ulong textSize = textHeader.Value.p_filesz;

                    fin.Position = (long)textStart;
                    objFile.Text = br.ReadBytes((int)textSize);

                    ulong dataStart = dataHeader.Value.p_offset;
                    ulong dataSize = dataHeader.Value.p_filesz;

                    fin.Position = (long)dataStart;
                    objFile.Data = br.ReadBytes((int)dataSize);

                    ulong ktextStart = ktextHeader.Value.p_offset;
                    ulong ktextSize = ktextHeader.Value.p_filesz;

                    fin.Position = (long)ktextStart;
                    objFile.KText = br.ReadBytes((int)ktextSize);

                    ulong kdataStart = kdataHeader.Value.p_offset;
                    ulong kdataSize = kdataHeader.Value.p_filesz;

                    fin.Position = (long)kdataStart;
                    objFile.KData = br.ReadBytes((int)kdataSize);

                    objFile.IsKTextStart = isKTextStart;
                    objFile.KTextStartAddr = ktextHeader.Value.p_vaddr;

                    ulong relocStart = relocHeader.Value.p_offset;

                    fin.Position = (long)relocStart;
                    ulong numRelocEntries = br.ReadUInt64();
                    objFile.RelocTable = new List<RelocationTableElement>((int)numRelocEntries);
                    for (ulong i = 0; i < numRelocEntries; i++)
                    {
                        ulong pos = br.ReadUInt64();
                        uint strlen = br.ReadUInt32();
                        byte[] strbin = br.ReadBytes((int)strlen);
                        ushort relocType = br.ReadUInt16();
                        ushort labelLoc = br.ReadUInt16();

                        RelocationTableElement relocEntry = new RelocationTableElement(
                            pos,
                            Encoding.UTF8.GetString(strbin),
                            (RelocationType)relocType,
                            (SymbolScope)labelLoc);
                        objFile.RelocTable.Add(relocEntry);
                    }

                    ulong symbolStart = symHeader.Value.p_offset;

                    fin.Position = (long)symbolStart;
                    ulong numSymbolEntries = br.ReadUInt64();
                    objFile.SymbolTable = new Dictionary<string, SymbolTableElement>();
                    objFile.GlobalSymbols = new HashSet<string>();
                    for (ulong i = 0; i < numSymbolEntries; i++)
                    {
                        uint strlen = br.ReadUInt32();
                        byte[] strbin = br.ReadBytes((int)strlen);
                        ushort symbolType = br.ReadUInt16();
                        ushort symbolScope = br.ReadUInt16();
                        ulong pos = br.ReadUInt64();

                        SymbolTableElement symbolEntry = new SymbolTableElement(
                            Encoding.UTF8.GetString(strbin),
                            (SymbolType)symbolType,
                            (SymbolScope)symbolScope,
                            pos);

                        if (symbolEntry.Scope == SymbolScope.Global)
                        {
                            objFile.GlobalSymbols.Add(symbolEntry.Label);
                        }
                        objFile.SymbolTable[symbolEntry.Label] = symbolEntry;
                    }
                }
            }

            return objFile;
        }

        static bool CheckSignedRange(long data, int bitLen)
        {
            int iMax = (int)((1U << (bitLen - 1)) - 1U);
            int iMin = -iMax - 1;

            return data >= iMin && data <= iMax;
        }

        static bool CheckUnsignedRange(ulong data, int bitLen)
        {
            uint uiMax = (1U << bitLen) - 1U;
            return data <= uiMax;
        }

        static bool CheckJumpRange(ulong caller, ulong callee)
        {
            ulong callerPC = (caller >> 2) & (~0x1FFFFFFU);
            ulong calleePC = (callee >> 2) & (~0x1FFFFFFU);
            return callerPC == calleePC;
        }

        public static void Link(string[] inputs, string output)
        {
            List<ObjFile> objFiles = new List<ObjFile>(inputs.Length);
            for (int i = 0; i < inputs.Length; i++)
            {
                objFiles.Add(LoadObject(inputs[i]));
            }

            Dictionary<string, int> globalSymbolLocation = new Dictionary<string, int>();
            for (int i = 0; i < objFiles.Count; i++)
            {
                foreach (string globalSymbol in objFiles[i].GlobalSymbols)
                {
                    if (globalSymbolLocation.ContainsKey(globalSymbol))
                    {
                        throw new Exception($"Error: global symbol {globalSymbol} collides in {inputs[i]} and {inputs[globalSymbolLocation[globalSymbol]]}");
                    }
                    globalSymbolLocation[globalSymbol] = i;
                }
            }

            ulong entrypoint = 0U;
            int entrypointObj = 0;
            for (int i = 0; i < objFiles.Count; i++)
            {
                if (objFiles[i].Entrypoint != 0)
                {
                    if (entrypoint != 0)
                    {
                        throw new Exception($"Error: multiple entrypoints in {inputs[i]} and {inputs[entrypointObj]}");
                    }
                    entrypoint = objFiles[i].Entrypoint;
                    entrypointObj = i;
                }
            }

            if (entrypoint == 0)
            {
                throw new Exception($"Error: there is no entrypoint");
            }

            ulong[] dataStart = new ulong[objFiles.Count];
            ulong[] textStart = new ulong[objFiles.Count];
            ulong[] kdataStart = new ulong[objFiles.Count];
            ulong[] ktextStart = new ulong[objFiles.Count];

            ulong dataSize = 0U;
            ulong textSize = 0U;
            ulong kdataSize = 0U;
            ulong ktextSize = 0U;

            int ktextStartIndex = -1;

            for (int i = 0; i < objFiles.Count; i++)
            {
                if (objFiles[i].IsKTextStart)
                {
                    ktextStartIndex = i;
                    break;
                }
            }

            dataStart[0] = Assembler.DATA_START;
            textStart[0] = Assembler.TEXT_START;
            kdataStart[0] = Assembler.KDATA_START;
            dataSize += (ulong)objFiles[0].Data.Length;
            textSize += (ulong)objFiles[0].Text.Length;
            kdataSize += (ulong)objFiles[0].KData.Length;

            for (int i = 1; i < objFiles.Count; i++)
            {
                dataStart[i] = dataStart[i - 1] + (ulong)objFiles[i - 1].Data.Length;
                textStart[i] = textStart[i - 1] + (ulong)objFiles[i - 1].Text.Length;
                kdataStart[i] = kdataStart[i - 1] + (ulong)objFiles[i - 1].KData.Length;

                // align data segments to 32-byte boundary
                if (dataStart[i] % 32 != 0)
                {
                    ulong alignedDataStart = 32 * (dataStart[i] / 32 + 1);
                    dataSize += (alignedDataStart - dataStart[i]);
                    dataStart[i] = alignedDataStart;
                }
                if (kdataStart[i] % 32 != 0)
                {
                    ulong alignedDataStart = 32 * (kdataStart[i] / 32 + 1);
                    kdataSize += (alignedDataStart - kdataStart[i]);
                    kdataStart[i] = alignedDataStart;
                }
                dataSize += (ulong)objFiles[i].Data.Length;
                textSize += (ulong)objFiles[i].Text.Length;
                kdataSize += (ulong)objFiles[i].KData.Length;
            }

            if (ktextStartIndex >= 0)
            {
                ktextStart[ktextStartIndex] = objFiles[ktextStartIndex].KTextStartAddr;
                ktextSize += (ulong)objFiles[ktextStartIndex].KText.Length;

                for (int i = 0; i < objFiles.Count; i++)
                {
                    if (i == ktextStartIndex)
                    {
                        continue;
                    }

                    ktextStart[i] = ktextSize + ktextStart[ktextStartIndex];
                    ktextSize += (ulong)objFiles[i].KText.Length;
                }
            }
            else
            {
                ktextStart[0] = Assembler.KTEXT_START;
                ktextSize += (ulong)objFiles[0].KText.Length;

                for (int i = 1; i < objFiles.Count; i++)
                {
                    ktextStart[i] = ktextStart[i - 1] + (ulong)objFiles[i - 1].KText.Length;

                    ktextSize += (ulong)objFiles[i].KText.Length;
                }
            }

            for (int i = 0; i < objFiles.Count; i++)
            {
                byte[] text = objFiles[i].Text;
                byte[] ktext = objFiles[i].KText;

                MemoryStream textStream = new MemoryStream(text, true);
                MemoryStream ktextStream = new MemoryStream(ktext, true);

                BinaryReader textReader = new BinaryReader(textStream);
                BinaryWriter textWriter = new BinaryWriter(textStream);
                BinaryReader ktextReader = new BinaryReader(ktextStream);
                BinaryWriter ktextWriter = new BinaryWriter(ktextStream);

                var relocTable = objFiles[i].RelocTable;
                foreach (var relocEntry in relocTable)
                {
                    ulong pos = relocEntry.Position;
                    string label = relocEntry.Label;

                    uint instruction = 0U;

                    switch (relocEntry.Type)
                    {
                        case RelocationType.IFormatImm:
                        case RelocationType.JFormatAddr:
                        case RelocationType.EIFormatImm:
                        case RelocationType.LAAddress:
                            textStream.Position = (long)pos;
                            instruction = textReader.ReadUInt32();
                            break;
                        case RelocationType.KIFormatImm:
                        case RelocationType.KJFormatAddr:
                        case RelocationType.KEIFormatImm:
                        case RelocationType.KLAAddress:
                            ktextStream.Position = (long)pos;
                            instruction = ktextReader.ReadUInt32();
                            break;
                    }

                    ulong relocAddr = 0U;
                    switch (relocEntry.LabelSearchLocation)
                    {
                        case SymbolScope.Internal:
                            {
                                var symbolTable = objFiles[i].SymbolTable;
                                if (!symbolTable.ContainsKey(label))
                                {
                                    throw new Exception($"Error: no such internal label {label} in {inputs[i]}");
                                }

                                var symbolEntry = symbolTable[label];
                                relocAddr = symbolEntry.Position;
                                if (symbolEntry.Type == SymbolType.Data)
                                {
                                    relocAddr += dataStart[i];
                                }
                                else if (symbolEntry.Type == SymbolType.Text)
                                {
                                    relocAddr += textStart[i];
                                }
                                else if (symbolEntry.Type == SymbolType.KData)
                                {
                                    relocAddr += kdataStart[i];
                                }
                                else if (symbolEntry.Type == SymbolType.KText)
                                {
                                    relocAddr += ktextStart[i];
                                }
                                else
                                {
                                    throw new Exception($"Error: unknown symbol type in {inputs[i]} with label {label}");
                                }
                            }
                            break;
                        case SymbolScope.Global:
                            {
                                if (!globalSymbolLocation.ContainsKey(label))
                                {
                                    throw new Exception($"Error: no such global label {label} in {inputs[i]}");
                                }

                                int symbolLocation = globalSymbolLocation[label];
                                var symbolEntry = objFiles[symbolLocation].SymbolTable[label];
                                relocAddr = symbolEntry.Position;
                                if (symbolEntry.Type == SymbolType.Data)
                                {
                                    relocAddr += dataStart[symbolLocation];
                                }
                                else if (symbolEntry.Type == SymbolType.Text)
                                {
                                    relocAddr += textStart[symbolLocation];
                                }
                                else if (symbolEntry.Type == SymbolType.KData)
                                {
                                    relocAddr += kdataStart[symbolLocation];
                                }
                                else if (symbolEntry.Type == SymbolType.KText)
                                {
                                    relocAddr += ktextStart[symbolLocation];
                                }
                                else
                                {
                                    throw new Exception($"Error: unknown symbol type in {inputs[i]} with label {label}");
                                }
                            }
                            break;
                        default:
                            throw new Exception($"Error: unknown relocation scope in {inputs[i]} with label {label}");
                    }

                    switch (relocEntry.Type)
                    {
                        case RelocationType.IFormatImm: // branch target
                            {
                                long distance = (long)relocAddr - ((long)pos + (long)textStart[i] + 4); // distance from next inst
                                distance /= 4;
                                if (!CheckSignedRange(distance, 15))
                                {
                                    throw new Exception($"Error: distance of label {label} is out of range in {inputs[i]}");
                                }
                                ulong distanceu = (ulong)distance;
                                instruction |= (uint)(distanceu & 0b111_1111_1111_1111U);

                                textStream.Position = (long)pos;
                                textWriter.Write(instruction);
                                break;
                            }
                        case RelocationType.KIFormatImm:
                            {
                                long distance = (long)relocAddr - ((long)pos + (long)ktextStart[i] + 4); // distance from next inst
                                distance /= 4;
                                if (!CheckSignedRange(distance, 15))
                                {
                                    throw new Exception($"Error: distance of label {label} is out of range in {inputs[i]}");
                                }
                                ulong distanceu = (ulong)distance;
                                instruction |= (uint)(distanceu & 0b111_1111_1111_1111U);

                                ktextStream.Position = (long)pos;
                                ktextWriter.Write(instruction);
                                break;
                            }
                        case RelocationType.JFormatAddr: // jump target
                            {
                                ulong addr = relocAddr >> 2;
                                if (!CheckJumpRange(pos + textStart[i], relocAddr))
                                {
                                    throw new Exception($"Error: position of jump label {label} is out of range in {inputs[i]}");
                                }
                                instruction |= (uint)(addr & 0x1FFFFFFU);

                                textStream.Position = (long)pos;
                                textWriter.Write(instruction);
                                break;
                            }
                        case RelocationType.KJFormatAddr:
                            {
                                ulong addr = relocAddr >> 2;
                                if (!CheckJumpRange(pos + ktextStart[i], relocAddr))
                                {
                                    throw new Exception($"Error: position of jump label {label} is out of range in {inputs[i]}");
                                }
                                instruction |= (uint)(addr & 0x1FFFFFFU);

                                ktextStream.Position = (long)pos;
                                ktextWriter.Write(instruction);
                                break;
                            }
                        case RelocationType.LAAddress:
                            {
                                for (int n = 0; n < 4; n++)
                                {
                                    textStream.Position = (long)pos + 4 * n;
                                    instruction = textReader.ReadUInt32();

                                    uint imm = (uint)((relocAddr >> n * 16) & 0xFFFFU);
                                    instruction |= imm;

                                    textStream.Position = (long)pos + 4 * n;
                                    textWriter.Write(instruction);
                                }
                            }
                            break;
                        case RelocationType.KLAAddress:
                            {
                                for (int n = 0; n < 4; n++)
                                {
                                    ktextStream.Position = (long)pos + 4 * n;
                                    instruction = ktextReader.ReadUInt32();

                                    uint imm = (uint)((relocAddr >> n * 16) & 0xFFFFU);
                                    instruction |= imm;

                                    ktextStream.Position = (long)pos + 4 * n;
                                    ktextWriter.Write(instruction);
                                }
                            }
                            break;
                        default:
                            throw new Exception($"Error: unknown relocation type in {inputs[i]} with label {label}");
                    }
                }
            }

            ELFHeader elfHeader = new ELFHeader();
            elfHeader.e_type = ELFHeader.ET_EXEC;
            elfHeader.e_entry = entrypoint - Assembler.TEXT_START + textStart[entrypointObj];
            elfHeader.e_phoff = elfHeader.e_ehsize;
            elfHeader.e_phnum = 4; // text, data, ktext, kdata

            ProgramHeader textHeader = new ProgramHeader();
            textHeader.p_type = ProgramHeader.PT_LOAD;
            textHeader.p_offset = elfHeader.e_ehsize + (ulong)elfHeader.e_phentsize * elfHeader.e_phnum;
            textHeader.p_vaddr = Assembler.TEXT_START;
            textHeader.p_paddr = 0;
            textHeader.p_filesz = textSize;
            textHeader.p_memsz = textSize;
            textHeader.p_flags = ProgramHeader.PF_X | ProgramHeader.PF_R;
            textHeader.p_align = 4;

            ProgramHeader dataHeader = new ProgramHeader();
            dataHeader.p_type = ProgramHeader.PT_LOAD;
            dataHeader.p_offset = textHeader.p_offset + textHeader.p_filesz;
            dataHeader.p_vaddr = Assembler.DATA_START;
            dataHeader.p_paddr = 0;
            dataHeader.p_filesz = dataSize;
            dataHeader.p_memsz = dataSize > Assembler.MIN_STATIC_SIZE ? dataSize + Assembler.MIN_STATIC_SIZE / 2 : Assembler.MIN_STATIC_SIZE;
            dataHeader.p_flags = ProgramHeader.PF_R | ProgramHeader.PF_W;
            dataHeader.p_align = 32;

            ProgramHeader ktextHeader = new ProgramHeader();
            ktextHeader.p_type = ProgramHeader.PT_KLOAD;
            ktextHeader.p_offset = dataHeader.p_offset + dataHeader.p_filesz;
            ktextHeader.p_vaddr = ktextStartIndex >= 0 ? ktextStart[ktextStartIndex] : Assembler.KTEXT_START;
            ktextHeader.p_paddr = 0;
            ktextHeader.p_filesz = ktextSize;
            ktextHeader.p_memsz = ktextSize;
            ktextHeader.p_flags = ProgramHeader.PF_X | ProgramHeader.PF_R;
            ktextHeader.p_align = 4;

            ProgramHeader kdataHeader = new ProgramHeader();
            kdataHeader.p_type = ProgramHeader.PT_KLOAD;
            kdataHeader.p_offset = ktextHeader.p_offset + ktextHeader.p_filesz;
            kdataHeader.p_vaddr = Assembler.KDATA_START;
            kdataHeader.p_paddr = 0;
            kdataHeader.p_filesz = kdataSize;
            kdataHeader.p_memsz = kdataSize;
            kdataHeader.p_flags = ProgramHeader.PF_R | ProgramHeader.PF_W;
            kdataHeader.p_align = 32;

            if (dataHeader.p_memsz % 32 != 0)
            {
                dataHeader.p_memsz = 32 * (dataHeader.p_memsz / 32 + 1);
            }

            using (FileStream fout = new FileStream(output, FileMode.Create))
            {
                using (BinaryWriter fwrite = new BinaryWriter(fout))
                {
                    fwrite.Write(HeaderUtils.GetBytes(elfHeader));
                    fwrite.Write(HeaderUtils.GetBytes(textHeader));
                    fwrite.Write(HeaderUtils.GetBytes(dataHeader));
                    fwrite.Write(HeaderUtils.GetBytes(ktextHeader));
                    fwrite.Write(HeaderUtils.GetBytes(kdataHeader));

                    for (int i = 0; i < objFiles.Count; i++)
                    {
                        fout.Position = (long)(textStart[i] - textStart[0] + textHeader.p_offset);
                        fwrite.Write(objFiles[i].Text);
                    }

                    for (int i = 0; i < objFiles.Count; i++)
                    {
                        fout.Position = (long)(dataStart[i] - dataStart[0] + dataHeader.p_offset);
                        fwrite.Write(objFiles[i].Data);
                    }

                    ulong ktextStartAddr = Assembler.KTEXT_START;
                    if (ktextStartIndex >= 0)
                    {
                        ktextStartAddr = ktextStart[ktextStartIndex];
                    }

                    for (int i = 0; i < objFiles.Count; i++)
                    {
                        fout.Position = (long)(ktextStart[i] - ktextStartAddr + ktextHeader.p_offset);
                        fwrite.Write(objFiles[i].KText);
                    }

                    for (int i = 0; i < objFiles.Count; i++)
                    {
                        fout.Position = (long)(kdataStart[i] - kdataStart[0] + kdataHeader.p_offset);
                        fwrite.Write(objFiles[i].KData);
                    }

                    if (fout.Length < fout.Position)
                    {
                        long cnt = fout.Position - fout.Length;
                        fout.Seek(0, SeekOrigin.End);
                        fwrite.Write(new byte[cnt]);
                    }
                }
            }
        }
    }
}
