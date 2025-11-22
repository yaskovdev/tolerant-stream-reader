namespace TolerantStreamReader;

public interface ICorruptionTolerantReader
{
    Task<ReadResult> ReadNext(CancellationToken cancellationToken);
}
