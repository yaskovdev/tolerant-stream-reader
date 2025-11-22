# Corruption-Tolerant Frame Protocol for .NET  [![Build Status](https://github.com/yaskovdev/corruption-tolerant-stream/actions/workflows/dotnet.yml/badge.svg)](https://github.com/yaskovdev/corruption-tolerant-stream/actions/workflows/dotnet.yml)

**Because streams fail.**

This library implements a small binary framing protocol and provides both a
self-resynchronizing **reader** and a lightweight **writer** for .NET streams.

When streams are lossy, truncated, or corrupted mid-write, ordinary readers fall
out of sync and can't recover. This library doesn't: it uses a magic header,
length prefix, and CRC checks to find the next valid frame and continue parsing
even after arbitrary corruption.

## Features

- **Magic header** to locate frame boundaries
- **Length prefix** to know how much to read
- **CRC checks** on both size and payload to detect corruption
- **Automatic recovery** from truncated or damaged frames

Suitable for unreliable transports, log pipelines, diagnostic channels, embedded
systems, and any use case where a typical stream reader is too fragile.

## Minimal Usage Example

```csharp
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CorruptionTolerantStream;

// Writing a payload
var payload = new byte[] { 1, 2, 3 };
using var stream = new MemoryStream();
await stream.WritePayload(payload);
stream.Position = 0; // rewind for reading

// Reading a payload
var reader = new CorruptionTolerantReader(stream);
var result = await reader.ReadPayload(CancellationToken.None);
if (result.ReadStatus == ReadStatus.Success)
{
    // Use result.Payload
}
```

## Package Publishing

```shell
./publish.sh "ghp_ge8Q9Vr7Kcv1N8DLSDmjTG06sqVjrt2MustL"
```

Where `ghp_ge8Q9Vr7Kcv1N8DLSDmjTG06sqVjrt2MustL` is a GitHub personal access token.

Check the package in [GitHub Packages](https://github.com/yaskovdev?tab=packages).

Link it to the repository, if necessary.
