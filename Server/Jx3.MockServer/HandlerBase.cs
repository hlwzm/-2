using System.Diagnostics;

namespace Jx3.MockServer;

public abstract class HandlerBase
{
    protected static readonly Random _rng = new();
    protected static void SimulateLatency()
    {
        var ms = _rng.Next(0, 51);
        if (ms > 0) Thread.Sleep(ms);
    }

    protected static byte[] BuildResponse(uint reqMsgId, uint seq, Action<BinaryWriter> writeBody)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((ushort)0x9A7B);
        writer.Write(reqMsgId + 1);
        writer.Write(seq);
        var bodyLenPos = ms.Position;
        writer.Write(0);
        writer.Write((ushort)0);
        if (writeBody != null)
        {
            var bodyStartPos = ms.Position;
            writeBody(writer);
            var bodyLen = ms.Position - bodyStartPos;
            ms.Seek(bodyLenPos, SeekOrigin.Begin);
            writer.Write((int)bodyLen);
            ms.Seek(0, SeekOrigin.End);
        }
        return ms.ToArray();
    }

    protected static byte[] BuildResponse(uint reqMsgId, uint seq, byte[] body)
    {
        return BuildResponse(reqMsgId, seq, w =>
        {
            if (body != null && body.Length > 0)
                w.Write(body);
        });
    }

    protected static byte[] Error(uint reqMsgId, uint seq, int code, string msg)
    {
        return BuildResponse(reqMsgId, seq, w =>
        {
            w.Write(code);
            w.Write(msg);
        });
    }
}
