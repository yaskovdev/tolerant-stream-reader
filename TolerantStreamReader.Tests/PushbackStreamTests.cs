namespace TolerantStreamReader.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

[TestClass]
public class PushbackStreamTests
{
    [TestMethod]
    public async Task ShouldSupportUnread()
    {
        using var inner = new MemoryStream(Enumerable.Range(0, 6).Select(i => (byte)i).ToArray());
        await using var instanceUnderTest = new PushbackStream(inner);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 3, [0, 1, 2]);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 1, [3]);
        instanceUnderTest.Unread([2, 3]);
        instanceUnderTest.Unread([0, 1]);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 0, []);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 3, [0, 1, 2]);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 2, [3, 4]);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 2, [5]);
    }

    [TestMethod]
    public async Task Should2()
    {
        using var inner = new MemoryStream(Enumerable.Range(0, 24).Select(i => (byte)i).ToArray());
        await using var instanceUnderTest = new PushbackStream(inner);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 12, [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11]);
        instanceUnderTest.Unread([8, 9, 10, 11]);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 1, [8]);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 1, [9]);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 1, [10]);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 1, [11]);

        await ReadAndAssertExpectedBytes(instanceUnderTest, 1, [12]);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 1, [13]);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 1, [14]);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 1, [15]);
        
        await ReadAndAssertExpectedBytes(instanceUnderTest, 1, [16]);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 1, [17]);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 1, [18]);
        await ReadAndAssertExpectedBytes(instanceUnderTest, 1, [19]);

        instanceUnderTest.Unread([16, 17, 18, 19]);
        
        await ReadAndAssertExpectedBytes(instanceUnderTest, 8, [16, 17, 18, 19, 20, 21, 22, 23]);
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
