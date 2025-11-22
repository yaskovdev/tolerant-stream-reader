namespace TolerantStreamReader;

public static class Constants
{
    // The probability of this magic occurring randomly in a byte stream is 1 / 256^8.
    public static readonly byte[] Magic = [0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE, 0xFA, 0xCE];
}
