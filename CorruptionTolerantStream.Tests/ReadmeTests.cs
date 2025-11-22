namespace TolerantStreamReader.Tests;

[TestClass]
public class ReadmeTests
{
    [TestMethod]
    public async Task ShouldReadmeExampleWork()
    {
        // Writing a payload
        var payload = new byte[] { 1, 2, 3 };
        using var stream = new MemoryStream();
        await stream.WriteFramed(payload);
        stream.Position = 0; // rewind for reading

        // Reading a payload
        var reader = new CorruptionTolerantReader(stream);
        var result = await reader.ReadNext(CancellationToken.None);
        if (result.ReadStatus == ReadStatus.Success)
        {
            result.Payload.ShouldBe(payload);
        }
    }
}
