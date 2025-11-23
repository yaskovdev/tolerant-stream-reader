namespace CorruptionTolerantStream.Tests;

[TestClass]
public class PushbackStreamTests
{
    [TestMethod]
    public async Task ShouldReadFromPushbackThenFromInnerStream()
    {
        using var inner = new MemoryStream(Enumerable.Range(0, 2).Select(i => (byte)i).ToArray());
        await using var instanceUnderTest = new PushbackStream(inner);

        await ReadAndAssertExpectedBytes(instanceUnderTest, 1, [0]);
        instanceUnderTest.Unread([0]);

        await ReadAndAssertExpectedBytes(instanceUnderTest, 2, [0, 1]);
    }

    [TestMethod]
    public async Task ShouldReadZeroBytes()
    {
        using var inner = new MemoryStream([]);
        await using var instanceUnderTest = new PushbackStream(inner);

        await ReadAndAssertExpectedBytes(instanceUnderTest, 0, []);
    }

    [TestMethod]
    public async Task ShouldSupportUnread_WhenPushbackStreamIsPartiallyRead()
    {
        using var inner = new MemoryStream(Enumerable.Range(0, 2).Select(i => (byte)i).ToArray());
        await using var instanceUnderTest = new PushbackStream(inner);

        // Create the pushback stream
        await ReadAndAssertExpectedBytes(instanceUnderTest, 2, [0, 1]);
        instanceUnderTest.Unread([0, 1]);

        // Read from the pushback stream partially
        await ReadAndAssertExpectedBytes(instanceUnderTest, 1, [0]);

        // Unread to partially read pushback stream
        instanceUnderTest.Unread([0]);

        // Assert that we can read all bytes again
        await ReadAndAssertExpectedBytes(instanceUnderTest, 2, [0, 1]);
    }

    private static async Task ReadAndAssertExpectedBytes(Stream stream, int bytesToRead, byte[] expected)
    {
        var buffer = new Memory<byte>(new byte[bytesToRead]);
        var count = await stream.ReadAsync(buffer);
        count.ShouldBe(expected.Length);
        var array = buffer.Span[..count].ToArray();
        array.ShouldBe(expected);
    }
}
