namespace Jx3.Common.Network;

/// <summary>
/// 消息帧结构: Magic(2) + MsgID(4) + Seq(4) + BodyLen(4) + Flag(2) + Body(n)
/// </summary>
public class MessagePacket
{
    public const ushort Magic = 0x9A7B;
    public const int HeaderSize = 16;

    public uint MsgId { get; set; }
    public uint Seq { get; set; }
    public ushort Flag { get; set; }
    public byte[] Body { get; set; } = [];

    public byte[] Encode()
    {
        using var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        writer.Write(Magic);
        writer.Write(MsgId);
        writer.Write(Seq);
        writer.Write(Body.Length);
        writer.Write(Flag);
        writer.Write(Body);
        return ms.ToArray();
    }

    public static MessagePacket? Decode(byte[] data)
    {
        if (data.Length < HeaderSize) return null;
        using var ms = new MemoryStream(data);
        var reader = new BinaryReader(ms);
        var magic = reader.ReadUInt16();
        if (magic != Magic) return null;
        return new MessagePacket
        {
            MsgId = reader.ReadUInt32(),
            Seq = reader.ReadUInt32(),
            Flag = reader.ReadUInt16(),
            Body = reader.ReadBytes(data.Length - HeaderSize)
        };
    }
}