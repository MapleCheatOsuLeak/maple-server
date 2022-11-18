using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Maple_Server.Mapping.Native.Enums;
using Maple_Server.Mapping.PortableExecutable;

namespace Maple_Server.Mapping;

public class ImageMapper
{
    private Memory<byte> _imageBytes;
    private IntPtr _imageBaseAddress;
    private PeImage _peImage;

    public ImageMapper(Memory<byte> imageBytes)
    {
        if (imageBytes.IsEmpty)
        {
            throw new ArgumentException("The provided DLL bytes were empty!");
        }
        
        _imageBytes = imageBytes.ToArray();
        _peImage = new PeImage(imageBytes);
    }

    public int GetSizeOfImage() => _peImage.Headers.PEHeader.SizeOfImage;

    public List<ImageImport> GetImports()
    {
        List<ImageImport> imports = new List<ImageImport>();
        foreach (var importDescriptor in _peImage.ImportDirectory.GetImportDescriptors())
        {
            foreach (var (functionName, _, functionOrdinal) in importDescriptor.Functions)
            {
                imports.Add(new ImageImport { DescriptorName = importDescriptor.Name, FunctionNameOrOrdinal = functionName ?? functionOrdinal.ToString()});
            }
        }

        return imports;
    }

    public void SetImageBaseAddress(IntPtr imageBaseAddress) => _imageBaseAddress = imageBaseAddress;

    public void SetImports(List<ImageResolvedImport> resolvedImports)
    {
        foreach (var importDescriptor in _peImage.ImportDirectory.GetImportDescriptors())
        {
            foreach (var (functionName, functionOffset, functionOrdinal) in importDescriptor.Functions)
            {
                var resolvedImport = resolvedImports.FirstOrDefault(i => i.DescriptorName == importDescriptor.Name && i.FunctionNameOrOrdinal == (functionName ?? functionOrdinal.ToString()));
                if (resolvedImport == default)
                    continue;

                IntPtr functionAddress = (IntPtr)resolvedImport.FunctionAddress;
                MemoryMarshal.Write(_imageBytes.Span[functionOffset..], ref functionAddress);
            }
        }
    }

    public List<int> GetCallbacks()
    {
        List<int> callbacks = new();
        foreach (var callbackOffset in _peImage.TlsDirectory.GetTlsCallbacks().Select(callBack => callBack.RelativeAddress))
            callbacks.Add(callbackOffset);

        if (!((_peImage.Headers.CorHeader?.Flags.HasFlag(CorFlags.ILOnly) ?? false) || _peImage.Headers.PEHeader!.AddressOfEntryPoint == 0))
            callbacks.Add(_peImage.Headers.PEHeader!.AddressOfEntryPoint);

        return callbacks;
    }

    public List<ImageSection> MapImage()
    {
        // process relocs
        if (_peImage.Headers.PEHeader!.Magic == PEMagic.PE32)
        {
            var delta = (uint)_imageBaseAddress.ToInt32() - (uint) _peImage.Headers.PEHeader!.ImageBase;

            Parallel.ForEach(_peImage.RelocationDirectory.GetRelocations(), relocation =>
            {
                if (relocation.Type != RelocationType.HighLow)
                    return;
                
                var relocationValue = MemoryMarshal.Read<uint>(_imageBytes.Span[relocation.Offset..]) + delta;
                MemoryMarshal.Write(_imageBytes.Span[relocation.Offset..], ref relocationValue);
            });
        }
        else
        {
            var delta = (ulong)_imageBaseAddress.ToInt64() - _peImage.Headers.PEHeader!.ImageBase;

            Parallel.ForEach(_peImage.RelocationDirectory.GetRelocations(), relocation =>
            {
                if (relocation.Type != RelocationType.Dir64)
                    return;

                var relocationValue = MemoryMarshal.Read<ulong>(_imageBytes.Span[relocation.Offset..]) + delta;
                MemoryMarshal.Write(_imageBytes.Span[relocation.Offset..], ref relocationValue);
            });
        }
        
        // build all sections
        List<ImageSection> sections = new();
        var sectionHeaders = _peImage.Headers.SectionHeaders.AsEnumerable();
        if (_peImage.Headers.CorHeader is null || !_peImage.Headers.CorHeader.Flags.HasFlag(CorFlags.ILOnly))
            sectionHeaders = sectionHeaders.Where(sectionHeader => !sectionHeader.SectionCharacteristics.HasFlag(SectionCharacteristics.MemDiscardable));

        foreach (var sectionHeader in sectionHeaders)
        {
            if (sectionHeader.Name == ".reloc" || sectionHeader.Name == ".rsrc")
                continue;
            
            if (sectionHeader.SectionCharacteristics.HasFlag(SectionCharacteristics.MemDiscardable))
                continue;

            if (sectionHeader.SizeOfRawData == 0)
                continue;

            var sectionSize = sectionHeader.SizeOfRawData == 0 ? (sectionHeader.VirtualSize > 0 ? 
                sectionHeader.VirtualSize : _peImage.Headers.PEHeader.SectionAlignment) : sectionHeader.SizeOfRawData;
            
            if (sectionSize == 0)
                continue;
            
            var sectionAddress = _imageBaseAddress + sectionHeader.VirtualAddress;
            var sectionBytes = _imageBytes.Span.Slice(sectionHeader.PointerToRawData, sectionSize);

            ProtectionType sectionProtection;
            if (sectionHeader.SectionCharacteristics.HasFlag(SectionCharacteristics.MemExecute))
            {
                if (sectionHeader.SectionCharacteristics.HasFlag(SectionCharacteristics.MemWrite))
                {
                    sectionProtection = sectionHeader.SectionCharacteristics.HasFlag(SectionCharacteristics.MemRead) ? ProtectionType.ExecuteReadWrite : ProtectionType.ExecuteWriteCopy;
                }

                else
                {
                    sectionProtection = sectionHeader.SectionCharacteristics.HasFlag(SectionCharacteristics.MemRead) ? ProtectionType.ExecuteRead : ProtectionType.Execute;
                }
            }
            else if (sectionHeader.SectionCharacteristics.HasFlag(SectionCharacteristics.MemWrite))
                sectionProtection = sectionHeader.SectionCharacteristics.HasFlag(SectionCharacteristics.MemRead) ? ProtectionType.ReadWrite : ProtectionType.WriteCopy;
            else
                sectionProtection = sectionHeader.SectionCharacteristics.HasFlag(SectionCharacteristics.MemRead) ? ProtectionType.ReadOnly : ProtectionType.NoAccess;

            if (sectionHeader.SectionCharacteristics.HasFlag(SectionCharacteristics.MemNotCached))
                sectionProtection |= ProtectionType.NoCache;
            
            sections.Add(new ImageSection
            {
                Address = sectionAddress.ToInt32(),
                Data = sectionBytes.ToArray(),
                Protection = sectionProtection,
                ProtectionSize = sectionHeader.SizeOfRawData,
            });
        }

        return sections;
    }
}