namespace TolerantStreamReader;

using System.IO.Hashing;
using LanguageExt;

// TODO: add check that the magic is aperiodic (e.g. not [0xBE, 0xEF, 0xBE, 0xEF])
// If it's violated, then you have to unread the magic bytes as well if the payload hashes don't match, then shift 1 byte forward, only then try searching for the next magic.
public class StreamReader(Stream stream, byte[] magic, TimeSpan delayBetweenReadRetries) : IStreamReader
{
    private readonly PushbackStream _stream = new(stream);

    public Aff<byte[]> ReadNext(CancellationToken cancellationToken) =>
        Prelude.Aff(async () =>
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var magicAndSizeBuffer = await ReadFromStreamExact(magic.Length + sizeof(int), cancellationToken);
                if (!magicAndSizeBuffer.Memory.Span[..magic.Length].SequenceEqual(magic))
                {
                    _stream.Unread(magicAndSizeBuffer.Memory.Span[magic.Length..].ToArray());
                    await ReadMagicInternal(cancellationToken);
                    _stream.Unread(magic);
                    continue;
                }
                // TODO: validate size against some maximum threshold
                var size = BitConverter.ToInt32(magicAndSizeBuffer.Memory.Span[magic.Length..]);
                using var payload = await ReadFromStreamExact(size, cancellationToken);
                using var expectedHashBuffer = await ReadFromStreamExact(sizeof(uint), cancellationToken);
                var expectedHash = BitConverter.ToUInt32(expectedHashBuffer.Memory.Span);
                var actualHash = Crc32.HashToUInt32(payload.Memory.Span);
                if (expectedHash == actualHash)
                {
                    return payload.Memory.ToArray();
                }

                _stream.Unread(expectedHashBuffer.Memory.ToArray());
                _stream.Unread(payload.Memory.ToArray());
                _stream.Unread(magicAndSizeBuffer.Memory.Span[magic.Length..].ToArray());
            }
        });

    /// <summary>
    /// Reads the specified magic byte sequence from the stream, advancing until the full sequence is matched.
    /// </summary>
    private async Task ReadMagicInternal(CancellationToken cancellationToken)
    {
        var matchedMagicBytes = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var buffer = await ReadFromStreamExact(1, cancellationToken);

            var b = buffer.Memory.Span[0];

            if (b == magic[matchedMagicBytes])
            {
                matchedMagicBytes++;
                if (matchedMagicBytes == magic.Length)
                {
                    return;
                }
            }
            else
            {
                matchedMagicBytes = b == magic[0] ? 1 : 0;
            }
        }
    }

    private async Task<ReadBuffer> ReadFromStreamExact(int size, CancellationToken cancellationToken)
    {
        var buffer = new ReadBuffer(size);
        var totalRead = 0;
        while (totalRead < size)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = await _stream.ReadAsync(buffer.Memory[totalRead..], cancellationToken);
            totalRead += read;
            if (read == 0)
            {
                await Task.Delay(delayBetweenReadRetries, cancellationToken);
            }
        }

        return buffer;
    }
}
