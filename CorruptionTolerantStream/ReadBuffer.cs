namespace TolerantStreamReader;

using System.Buffers;

public class ReadBuffer(int size) : IDisposable
{
    private readonly IMemoryOwner<byte> _buffer = MemoryPool<byte>.Shared.Rent(size);

    public Memory<byte> Memory => _buffer.Memory[..size];

    public void Dispose()
    {
        _buffer.Dispose();
    }
}
