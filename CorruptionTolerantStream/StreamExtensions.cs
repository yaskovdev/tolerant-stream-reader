namespace TolerantStreamReader;

using System.IO.Hashing;

public static class StreamExtensions
{
    /// <summary>
    /// Size overhead per payload is <c>8 + 4 + 4 = 16 bytes</c>.
    /// </summary>
    public static async Task WriteFramed(this Stream stream, byte[] payload, CancellationToken cancellationToken = default) =>
        await stream.WriteFramed(Constants.Magic, payload, cancellationToken);

    public static async Task WriteFramed(this Stream stream, byte[] magic, byte[] payload, CancellationToken cancellationToken)
    {
        var size = BitConverter.GetBytes(payload.Length);
        var sizeHash = Crc32.HashToUInt32(size);
        await stream.WriteAsync(magic, cancellationToken);
        await stream.WriteAsync(size, cancellationToken);
        await stream.WriteAsync(BitConverter.GetBytes(sizeHash), cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
        var payloadHash = Crc32.HashToUInt32(payload);
        await stream.WriteAsync(BitConverter.GetBytes(payloadHash), cancellationToken);
    }
}
