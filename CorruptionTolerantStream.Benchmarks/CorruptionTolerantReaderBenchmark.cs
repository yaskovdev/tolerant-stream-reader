namespace CorruptionTolerantStream.Benchmarks;

using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;

public class CorruptionTolerantReaderBenchmark
{
    private readonly IImmutableList<byte[]> _payloads = CreatePayloads().ToImmutableList();
    private byte[] _readBuffer = [];
    private byte[] _buffer = [];
    private byte[] _framedBuffer = [];

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _buffer = await CreateBuffer(_payloads);
        var frames = await Task.WhenAll(_payloads.Select(async payload => await CreateFrame(payload)));;
        _framedBuffer = await CreateBuffer([..frames]);
        _readBuffer = new byte[_payloads.Select(it => it.Length).Max()];
    }

    [Benchmark]
    public async Task WriteNotFramed()
    {
        using var stream = new MemoryStream();
        foreach (var payload in _payloads)
        {
            await stream.WriteAsync(payload);
        }
    }

    [Benchmark]
    public async Task WriteFramed()
    {
        using var stream = new MemoryStream();
        foreach (var payload in _payloads)
        {
            await stream.WritePayload(payload);
        }
    }

    [Benchmark]
    public async Task ReadNotFramed()
    {
        using var stream = new MemoryStream(_buffer);
        foreach (var payload in _payloads)
        {
            _ = await stream.ReadAsync(_readBuffer.AsMemory(0, payload.Length));
        }
    }

    [Benchmark]
    public async Task ReadFramed()
    {
        using var stream = new MemoryStream(_framedBuffer);
        var instanceUnderTest = new CorruptionTolerantReader(stream);
        while (true)
        {
            var res = await instanceUnderTest.ReadPayload(CancellationToken.None);
            if (res.ReadStatus == ReadStatus.EndOfStream)
            {
                break;
            }
        }
    }

    private static IEnumerable<byte[]> CreatePayloads()
    {
        for (var i = 0; i < 700; i++)
        {
            yield return Enumerable.Range(0, 1024 * 1024).Select(x => (byte)x).ToArray();
        }
    }

    private static async Task<byte[]> CreateBuffer(IImmutableList<byte[]> frames)
    {
        using var stream = new MemoryStream();
        foreach (var frame in frames)
        {
            await stream.WriteAsync(frame);
        }
        return stream.ToArray();
    }

    private static async Task<byte[]> CreateFrame(byte[] payload)
    {
        using var stream = new MemoryStream();
        await stream.WritePayload(payload);
        return stream.ToArray();
    }
}
