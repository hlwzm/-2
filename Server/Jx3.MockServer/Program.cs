using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Jx3.MockServer;

class Program
{
    static void Main()
    {
        Console.WriteLine("MockServer starting on port 9000...");
        var listener = new TcpListener(IPAddress.Any, 9000);
        listener.Start();
        uint sid = 0;
        while (true)
        {
            var client = listener.AcceptTcpClient();
            Console.WriteLine($"[{++sid}] Connect from {client.Client.RemoteEndPoint}");
            System.Threading.ThreadPool.QueueUserWorkItem(_ => Handle(sid, client));
        }
    }

    static void Handle(uint sid, TcpClient client)
    {
        try
        {
            var s = client.GetStream();
            var buf = new byte[65536];
            
            while (client.Connected)
            {
                var n = s.Read(buf, 0, 4);
                if (n < 4) break;
                int pktLen = (int)BitConverter.ToUInt32(buf, 0);
                if (pktLen <= 0 || pktLen > 65536) break;
                
                int total = 0;
                while (total < pktLen)
                {
                    n = s.Read(buf, total, pktLen - total);
                    if (n == 0) break;
                    total += n;
                }
                if (total < pktLen) break;
                
                // Parse packet (data starts at buf[0])
                var ms = new MemoryStream(buf, 0, pktLen);
                var br = new BinaryReader(ms);
                var magic = br.ReadUInt16();
                if (magic != 0x9A7B) { Console.WriteLine($"  Bad magic: 0x{magic:X4}"); continue; }
                var msgId = br.ReadUInt32();
                var seq = br.ReadUInt32();
                var bodyLen = br.ReadInt32();
                br.ReadUInt16(); // flag
                var body = bodyLen > 0 ? br.ReadBytes(bodyLen) : Array.Empty<byte>();
                
                Console.WriteLine($"  Rx MsgId={msgId} Seq={seq} Body={bodyLen}B");
                
                // Process
                byte[]? resp = null;
                var rms = new MemoryStream();
                var w = new BinaryWriter(rms);
                
                switch (msgId)
                {
                    case 1001: // CSLoginAuth
                    {
                        var rb = new BinaryReader(new MemoryStream(body));
                        var phone = rb.ReadString();
                        var pwd = rb.ReadString();
                        Console.WriteLine($"  -> Login phone={phone}");
                        w.Write(0); // code=0 OK
                        w.Write("tok_" + Guid.NewGuid().ToString("N")[..8]);
                        w.Write(10001UL);
                        resp = rms.ToArray();
                        break;
                    }
                    case 1007: // CSLoginEnterGame
                    {
                        var rb = new BinaryReader(new MemoryStream(body));
                        var pid = rb.ReadUInt64();
                        var token = rb.ReadString();
                        Console.WriteLine($"  -> EnterGame pid={pid}");
                        w.Write("侠客_" + pid);
                        w.Write(20);
                        w.Write(1001u);
                        w.Write(12580UL);
                        w.Write(368UL);
                        resp = rms.ToArray();
                        break;
                    }
                    default:
                        Console.WriteLine($"  -> Unknown msgId={msgId}");
                        break;
                }
                
                if (resp != null)
                {
                    // Build response packet
                    var pktMs = new MemoryStream();
                    var pw = new BinaryWriter(pktMs);
                    pw.Write((ushort)0x9A7B);
                    pw.Write(msgId + 1);
                    pw.Write(seq);
                    pw.Write(resp.Length);
                    pw.Write((ushort)0);
                    if (resp.Length > 0) pw.Write(resp);
                    var respPkt = pktMs.ToArray();
                    
                    // Send (4B len prefix + packet)
                    var lenBytes = BitConverter.GetBytes((uint)respPkt.Length);
                    s.Write(lenBytes, 0, 4);
                    s.Write(respPkt, 0, respPkt.Length);
                    Console.WriteLine($"  Tx MsgId={msgId + 1} Body={resp.Length}B");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine($"  [{sid}] Disconnected");
        }
    }
}