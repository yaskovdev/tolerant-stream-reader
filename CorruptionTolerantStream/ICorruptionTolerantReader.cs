namespace CorruptionTolerantStream;

public interface ICorruptionTolerantReader
{
    Task<ReadResult> ReadNext(CancellationToken cancellationToken);
}
