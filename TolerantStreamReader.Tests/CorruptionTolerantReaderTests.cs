namespace TolerantStreamReader.Tests;

using System.Collections.Immutable;

[TestClass]
public class CorruptionTolerantReaderTests
{
    [TestMethod]
    public async Task ShouldReadPayloads_WhenNotCorrupted()
    {
        IImmutableList<byte[]> frames = ImmutableList.Create(await CreateFrame([1, 1, 1]), await CreateFrame([2, 2]));
        var buffer = await CreateBuffer(frames);
        using var stream = new MemoryStream(buffer);
        var instanceUnderTest = new CorruptionTolerantReader(stream);
        (await instanceUnderTest.ReadNext(CancellationToken.None))
            .Payload
            .ShouldBe([1, 1, 1]);
        (await instanceUnderTest.ReadNext(CancellationToken.None))
            .Payload
            .ShouldBe([2, 2]);
        (await instanceUnderTest.ReadNext(CancellationToken.None))
            .ReadStatus
            .ShouldBe(ReadStatus.EndOfStream);
    }

    [TestMethod]
    public async Task ShouldReadPayloads_WhenExtraByte()
    {
        IImmutableList<byte[]> frames = ImmutableList.Create<byte[]>(await CreateFrame([1, 1, 1]));
        var buffer = await CreateBuffer(frames);
        using var stream = new MemoryStream(new byte[] { 0 }.Concat(buffer).ToArray());
        var instanceUnderTest = new CorruptionTolerantReader(stream);
        (await instanceUnderTest.ReadNext(CancellationToken.None))
            .Payload
            .ShouldBe([1, 1, 1]);
        (await instanceUnderTest.ReadNext(CancellationToken.None))
            .ReadStatus
            .ShouldBe(ReadStatus.EndOfStream);
    }

    [TestMethod]
    public async Task ShouldReadPayloads_WhenMagicCorrupted()
    {
        IImmutableList<byte[]> frames = ImmutableList.Create(await CreateFrame([1, 1, 1]), await CreateFrame([2, 2]));
        var buffer = await CreateBuffer(frames);

        // corrupt the magic of the first frame
        buffer[0] = 0x00;

        using var stream = new MemoryStream(buffer);
        var instanceUnderTest = new CorruptionTolerantReader(stream);
        (await instanceUnderTest.ReadNext(CancellationToken.None))
            .Payload
            .ShouldBe([2, 2]);
        (await instanceUnderTest.ReadNext(CancellationToken.None))
            .ReadStatus
            .ShouldBe(ReadStatus.EndOfStream);
    }

    [TestMethod]
    public async Task ShouldReadPayloads_WhenBodyCorrupted()
    {
        IImmutableList<byte[]> frames = ImmutableList.Create(await CreateFrame([1, 1, 1]), await CreateFrame([2, 2]), await CreateFrame([3]));
        var buffer = await CreateBuffer(frames);

        // corrupt the body of the second frame
        var indexOfFirstByteOfSecondPayload = frames[0].Length + Constants.Magic.Length + sizeof(int) + sizeof(uint);
        buffer[indexOfFirstByteOfSecondPayload].ShouldBe((byte)2);
        buffer[indexOfFirstByteOfSecondPayload + 1].ShouldBe((byte)2);
        buffer[indexOfFirstByteOfSecondPayload + 1] = 0xFF;

        using var stream = new MemoryStream(buffer);
        var instanceUnderTest = new CorruptionTolerantReader(stream);
        (await instanceUnderTest.ReadNext(CancellationToken.None))
            .Payload
            .ShouldBe([1, 1, 1]);
        (await instanceUnderTest.ReadNext(CancellationToken.None))
            .Payload
            .ShouldBe([3]);
        (await instanceUnderTest.ReadNext(CancellationToken.None))
            .ReadStatus
            .ShouldBe(ReadStatus.EndOfStream);
    }

    private static async Task<byte[]> CreateFrame(byte[] payload)
    {
        using var stream = new MemoryStream();
        await stream.WriteFramed(payload);
        return stream.ToArray();
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
}
