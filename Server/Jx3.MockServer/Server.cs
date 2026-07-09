using System.Net;
using System.Net.Sockets;
using Jx3.Common.Protocol;
using Jx3.MockServer.Handlers;

namespace Jx3.MockServer;

public class Server
{
    private readonly Dictionary<uint, IHandler> _handlers = new();
    private readonly TcpListener _listener;
    private uint _sid;

    public Server(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        RegisterHandlers();
    }

    void RegisterHandlers()
    {
        var login = new LoginHandler();
        var hero = new HeroHandler();
        var recruit = new RecruitHandler();
        var combat = new CombatHandler();
        var dungeon = new DungeonHandler();
        var trade = new TradeHandler();
        var chat = new ChatHandler();
        var team = new TeamHandler();
        var friend = new FriendHandler();
        var guild = new GuildHandler();
        var shop = new ShopHandler();
        var quest = new QuestHandler();
        var pvp = new PVPHandler();

        for (uint id = 1001; id <= 1009; id++) _handlers[id] = login;
        for (uint id = 1101; id <= 1111; id++) _handlers[id] = hero;
        for (uint id = 1201; id <= 1206; id++) _handlers[id] = recruit;
        for (uint id = 2001; id <= 2018; id++) _handlers[id] = combat;
        for (uint id = 3001; id <= 3015; id++) _handlers[id] = dungeon;
        for (uint id = 4001; id <= 4015; id++) _handlers[id] = trade;
        for (uint id = 5001; id <= 5011; id++) _handlers[id] = chat;
        for (uint id = 6001; id <= 6025; id++) _handlers[id] = team;
        for (uint id = 7001; id <= 7009; id++) _handlers[id] = friend;
        for (uint id = 7010; id <= 7018; id++) _handlers[id] = guild;
        for (uint id = 8001; id <= 8011; id++) _handlers[id] = shop;
        for (uint id = 9001; id <= 9011; id++) _handlers[id] = quest;
        for (uint id = 10001; id <= 10010; id++) _handlers[id] = pvp;
    }

    public void Start()
    {
        Console.WriteLine("MockServer starting on port 9000...");
        Console.WriteLine("  Registered: " + _handlers.Count + " message types");
        _listener.Start();

        while (true)
        {
            var client = _listener.AcceptTcpClient();
            var sid = ++_sid;
            Console.WriteLine($"[{sid}] Connect from {client.Client.RemoteEndPoint}");
            ThreadPool.QueueUserWorkItem(_ => HandleClient(sid, client));
        }
    }

    void HandleClient(uint sid, TcpClient client)
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

                using var ms = new MemoryStream(buf, 0, pktLen);
                using var br = new BinaryReader(ms);
                var magic = br.ReadUInt16();
                if (magic != 0x9A7B) { Console.WriteLine($"  Bad magic: 0x{magic:X4}"); continue; }
                var msgId = br.ReadUInt32();
                var seq = br.ReadUInt32();
                var bodyLen = br.ReadInt32();
                br.ReadUInt16();
                var body = bodyLen > 0 ? br.ReadBytes(bodyLen) : [];

                Console.WriteLine($"  Rx MsgId={msgId} Seq={seq} Body={bodyLen}B");

                byte[]? resp = null;
                if (_handlers.TryGetValue(msgId, out var handler))
                {
                    try
                    {
                        resp = handler.Handle(msgId, seq, body);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Handler error: {ex.Message}");
                        resp = BuildErrorPacket(msgId, seq, -1, ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine($"  -> Unknown msgId={msgId}");
                    continue;
                }

                if (resp != null && resp.Length > 0)
                {
                    var lenBytes = BitConverter.GetBytes((uint)resp.Length);
                    s.Write(lenBytes, 0, 4);
                    s.Write(resp, 0, resp.Length);
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

    static byte[] BuildErrorPacket(uint msgId, uint seq, int code, string msg)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((ushort)0x9A7B);
        w.Write(msgId + 1);
        w.Write(seq);
        using var bodyMs = new MemoryStream();
        using var bw = new BinaryWriter(bodyMs);
        bw.Write(code);
        bw.Write(msg);
        var body = bodyMs.ToArray();
        w.Write(body.Length);
        w.Write((ushort)0);
        w.Write(body);
        return ms.ToArray();
    }
}
