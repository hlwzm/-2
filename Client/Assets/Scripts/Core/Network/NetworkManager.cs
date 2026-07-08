using System.Net.Sockets;
using System.Threading.Tasks;

namespace Jx3.Core.Network
{
    public class NetworkManager
    {
        private TcpClient _client = new();

        public async Task ConnectAsync(string host, int port)
        {
            await _client.ConnectAsync(host, port);
            UnityEngine.Debug.Log($"Connected to {host}:{port}");
        }

        public async Task SendAsync(byte[] data)
        {
            var stream = _client.GetStream();
            await stream.WriteAsync(data);
        }

        public void Disconnect()
        {
            _client?.Close();
        }
    }
}
