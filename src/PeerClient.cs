using System.Net.Sockets;
using System.Text;

namespace codecrafters_bittorrent.src;

class PeerClient
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;

    public PeerClient(string host, int port)
    {
        _client = new TcpClient(host, port);
        _stream = _client.GetStream();
    }

    public byte[] PerformHandshake(byte[] infoHash, byte[] peerId)
    {
        byte[] handshake = new byte[68];
        handshake[0] = 19; // Protocol length
        Array.Copy(Encoding.UTF8.GetBytes("BitTorrent protocol"), 0, handshake, 1, 19);
        Array.Copy(new byte[8], 0, handshake, 20, 8); // Reserved bytes
        Array.Copy(infoHash, 0, handshake, 28, 20);
        Array.Copy(peerId, 0, handshake, 48, 20);


        _stream.Write(handshake, 0, handshake.Length);
        byte[] response = new byte[68];
        _stream.ReadExactly(response);
        return response[(handshake.Length-20)..];
    }
}
