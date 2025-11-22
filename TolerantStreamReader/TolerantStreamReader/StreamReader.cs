namespace TolerantStreamReader;

using System.IO.Hashing;
using LanguageExt;

public class StreamReader : IStreamReader
{
    private readonly PushbackStream _stream;
    private readonly byte[] _magic;
    private readonly TimeSpan _delayBetweenReadRetries;
    private readonly Action<long> _onInvalidFrameHeader;
    private readonly Action<long> _onInvalidPayloadHash;
    private long _totalReadBytes;

    private readonly Range _frameHeaderMagicRange;
    private readonly Range _frameHeaderSizeRange;
    private readonly Range _frameHeaderSizeHashRange;
    private readonly int _frameHeaderSize;

    public StreamReader(Stream stream, byte[] magic, TimeSpan delayBetweenReadRetries, Action<long>? onInvalidFrameHeader = null, Action<long>? onInvalidPayloadHash = null)
    {
        _magic = magic;
        _delayBetweenReadRetries = delayBetweenReadRetries;
        _onInvalidFrameHeader = onInvalidFrameHeader ?? (_ => { });
        _onInvalidPayloadHash = onInvalidPayloadHash ?? (_ => { });
        _frameHeaderMagicRange = .._magic.Length;
        _frameHeaderSizeRange = _magic.Length..(_magic.Length + sizeof(int));
        _frameHeaderSizeHashRange = (_magic.Length + sizeof(int))..(_magic.Length + sizeof(int) + sizeof(uint));
        _frameHeaderSize = _frameHeaderSizeHashRange.End.Value - _frameHeaderMagicRange.Start.Value;
        _stream = new PushbackStream(stream);
    }

    public Aff<byte[]> ReadNext(CancellationToken cancellationToken) =>
        Prelude.Aff(async () =>
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var header = await ReadFromStreamExact(_stream, _frameHeaderSize, cancellationToken);
                if (MagicIsValidAndSizeHashMatches(header))
                {
                    var payloadSize = BitConverter.ToInt32(header.Memory.Span[_frameHeaderSizeRange]);
                    using var payloadWithHash = await ReadFromStreamExact(_stream, payloadSize + sizeof(uint), cancellationToken);
                    if (PayloadHashMatches(payloadWithHash))
                    {
                        return payloadWithHash.Memory.Span[..payloadSize].ToArray();
                    }

                    _onInvalidPayloadHash.Invoke(_totalReadBytes);
                    UnreadStream(_stream, payloadWithHash.Memory.Span);
                }
                else
                {
                    _onInvalidFrameHeader.Invoke(_totalReadBytes);
                }

                UnreadStream(_stream, header.Memory.Span[1..]);
                await ConsumeNextMagic(_stream, cancellationToken);
                UnreadStream(_stream, _magic);
            }
        });

    /// <summary>
    /// Reads the specified magic byte sequence from the stream, advancing until the full sequence is matched.
    /// </summary>
    private async Task ConsumeNextMagic(Stream stream, CancellationToken cancellationToken)
    {
        var matchedMagicBytes = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var buffer = await ReadFromStreamExact(stream, 1, cancellationToken);

            var b = buffer.Memory.Span[0];

            if (b == _magic[matchedMagicBytes])
            {
                matchedMagicBytes++;
                if (matchedMagicBytes == _magic.Length)
                {
                    return;
                }
            }
            else
            {
                matchedMagicBytes = b == _magic[0] ? 1 : 0;
            }
        }
    }

    private async Task<ReadBuffer> ReadFromStreamExact(Stream stream, int size, CancellationToken cancellationToken)
    {
        var buffer = new ReadBuffer(size);
        var totalRead = 0;
        while (totalRead < size)
        {
            var read = await stream.ReadAsync(buffer.Memory[totalRead..], cancellationToken);
            totalRead += read;
            if (read == 0)
            {
                await Task.Delay(_delayBetweenReadRetries, cancellationToken);
            }
        }

        _totalReadBytes += size;
        return buffer;
    }

    private bool MagicIsValidAndSizeHashMatches(ReadBuffer header) =>
        header.Memory.Span[_frameHeaderMagicRange].SequenceEqual(_magic)
        && Crc32.HashToUInt32(header.Memory.Span[_frameHeaderSizeRange]) == BitConverter.ToUInt32(header.Memory.Span[_frameHeaderSizeHashRange]);

    private static bool PayloadHashMatches(ReadBuffer payloadAndHash)
    {
        var payloadHash = Crc32.HashToUInt32(payloadAndHash.Memory.Span[..^sizeof(uint)]);
        var expectedHash = BitConverter.ToUInt32(payloadAndHash.Memory.Span[^sizeof(uint)..]);
        return payloadHash == expectedHash;
    }

    private void UnreadStream(PushbackStream stream, ReadOnlySpan<byte> buffer)
    {
        stream.Unread(buffer);
        _totalReadBytes -= buffer.Length;
    }
}
