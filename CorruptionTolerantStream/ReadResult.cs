namespace TolerantStreamReader;

public enum ReadStatus
{
    Success,
    EndOfStream
}

public record ReadResult(byte[] Payload, ReadStatus ReadStatus);
