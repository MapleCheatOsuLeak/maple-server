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

    public int GetEntryPointOffset() => _peImage.Headers.PEHeader.AddressOfEntryPoint;

    public byte[] MapImage()
    {
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

        byte[] result = new byte[_imageBytes.Length];
        Array.Copy(_imageBytes.ToArray(), _peImage.Headers.PEHeader.SizeOfHeaders, result, _peImage.Headers.PEHeader.SizeOfHeaders, _imageBytes.Length - _peImage.Headers.PEHeader.SizeOfHeaders);
        return result;
    }
}