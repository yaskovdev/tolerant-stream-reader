namespace TolerantStreamReader.Tests;

using System.Collections.Immutable;
using System.IO.Hashing;
using LanguageExt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

[TestClass]
public class StreamReaderTests
{
    // The probability of this magic occurring randomly in a byte stream is 1 / 256^8.
    private static readonly byte[] Magic = [0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE, 0xFA, 0xCE];

    [TestMethod]
    public async Task ShouldReadPayloads_WhenNotCorrupted()
    {
        IImmutableList<byte[]> frames = ImmutableList.Create(await CreateFrame([1, 1, 1]), await CreateFrame([2, 2]));
        var buffer = await CreateBuffer(frames);
        using var stream = new MemoryStream(buffer);
        var instanceUnderTest = new StreamReader(stream, Magic, TimeSpan.FromSeconds(10));
        (await instanceUnderTest.ReadNext(CancellationToken.None).Run())
            .ToOption()
            .ShouldBe(Prelude.Optional<byte[]>([1, 1, 1]));
        (await instanceUnderTest.ReadNext(CancellationToken.None).Run())
            .ToOption()
            .ShouldBe(Prelude.Optional<byte[]>([2, 2]));
    }

    [TestMethod]
    public async Task ShouldReadPayloads_WhenMagicCorrupted()
    {
        IImmutableList<byte[]> frames = ImmutableList.Create(await CreateFrame([1, 1, 1]), await CreateFrame([2, 2]));
        var buffer = await CreateBuffer(frames);

        // corrupt the magic of the first frame
        buffer[0] = 0x00;

        using var stream = new MemoryStream(buffer);
        var instanceUnderTest = new StreamReader(stream, Magic, TimeSpan.FromSeconds(10));
        (await instanceUnderTest.ReadNext(CancellationToken.None).Run())
            .ToOption()
            .ShouldBe(Prelude.Optional<byte[]>([2, 2]));
    }

    [TestMethod]
    public async Task ShouldReadPayloads_WhenBodyCorrupted()
    {
        IImmutableList<byte[]> frames = ImmutableList.Create(await CreateFrame([1, 1, 1]), await CreateFrame([2, 2]), await CreateFrame([3]));
        var buffer = await CreateBuffer(frames);

        // corrupt the body of the second frame
        var indexOfFirstByteOfSecondPayload = frames[0].Length + Magic.Length + sizeof(int);
        buffer[indexOfFirstByteOfSecondPayload].ShouldBe((byte)2);
        buffer[indexOfFirstByteOfSecondPayload + 1].ShouldBe((byte)2);
        buffer[indexOfFirstByteOfSecondPayload + 1] = 0xFF;

        using var stream = new MemoryStream(buffer);
        var instanceUnderTest = new StreamReader(stream, Magic, TimeSpan.FromSeconds(10));
        (await instanceUnderTest.ReadNext(CancellationToken.None).Run())
            .ToOption()
            .ShouldBe(Prelude.Optional<byte[]>([1, 1, 1]));
        (await instanceUnderTest.ReadNext(CancellationToken.None).Run())
            .ToOption()
            .ShouldBe(Prelude.Optional<byte[]>([3]));
    }

    private static async Task<byte[]> CreateFrame(byte[] payload)
    {
        var size = BitConverter.GetBytes(payload.Length);
        var hash = Crc32.HashToUInt32(payload);
        using var stream = new MemoryStream();
        await stream.WriteAsync(Magic);
        await stream.WriteAsync(size);
        await stream.WriteAsync(payload);
        await stream.WriteAsync(BitConverter.GetBytes(hash));
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
