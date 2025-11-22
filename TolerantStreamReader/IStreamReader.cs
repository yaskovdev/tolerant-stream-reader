namespace TolerantStreamReader;

using LanguageExt;

public interface IStreamReader
{
    Aff<byte[]> ReadNext(CancellationToken cancellationToken);
}
