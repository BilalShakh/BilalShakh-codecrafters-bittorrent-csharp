using codecrafters_bittorrent.src.Data;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace codecrafters_bittorrent.src;

class PeerClient
{
    private TcpClient _client;
    private NetworkStream _stream;
    private string _host;
    private int _port;

    public PeerClient(string host, int port)
    {
        _host = host;
        _port = port;
        _client = new TcpClient(host, port);
        _stream = _client.GetStream();
    }

    public byte[] PerformHandshake(byte[] infoHash, byte[] peerId, bool isMagnet = false)
    {
        byte[] handshake = new byte[68];
        byte[] reserved = new byte[8];
        handshake[0] = 19; // Protocol length
        if (isMagnet)
        {
            reserved[5] = 16;
        }
        Array.Copy(Encoding.UTF8.GetBytes("BitTorrent protocol"), 0, handshake, 1, 19);
        Array.Copy(reserved, 0, handshake, 20, 8); // Reserved bytes
        Array.Copy(infoHash, 0, handshake, 28, 20);
        Array.Copy(peerId, 0, handshake, 48, 20);

        _stream.Write(handshake, 0, handshake.Length);
        byte[] response = new byte[68];
        _stream.ReadExactly(response);
        return response[(handshake.Length-20)..];
    }

    public byte[] DownloadPiece(TorrentInfoCommandResult infoCommandResult, int pieceIndex)
    {
        ReadMessage(MessageTypes.Bitfield);
        SendMessage([.. BitConverter.GetBytes(1), MessageTypes.Interested]);
        ReadMessage(MessageTypes.Unchoke);

        long pieceLength = Math.Min(infoCommandResult.Length - pieceIndex * infoCommandResult.PieceLength, infoCommandResult.PieceLength);
        byte[] pieceBytes = new byte[pieceLength];
        int blockSize = 16384; // 16 KB

        for (int i = 0; i < (double)pieceLength / blockSize; i++)
        {
            SendMessage(CreateRequestMessage(pieceIndex, i * blockSize, Math.Min(blockSize, (int)pieceLength - i * blockSize)));
            var pieceData = ReadMessage(MessageTypes.Piece);
            pieceData[8..].CopyTo(pieceBytes, i * blockSize);
        }

        if (!VerifyPieceIntegrity(pieceBytes, infoCommandResult.Pieces[pieceIndex]))
        {
            throw new InvalidDataException($"Could not verify integrity of piece at index: {pieceIndex}");
        }

        return pieceBytes;
    }

    public void Stop()
    {
        _stream.Close();
        _client.Close();
    }

    public void Start()
    {
        _client = new TcpClient(_host, _port);
        _stream = _client.GetStream();
    }

    private static byte[] CreateRequestMessage(int pieceIndex, int offset, int length)
    {
        Console.Error.WriteLine($"Requesting piece {pieceIndex} with offset {offset} and length {length}");
        List<byte> message = [];
        
        message.AddRange(BitConverter.GetBytes(13));
        message.Add(MessageTypes.Request);
        message.AddRange(BitConverter.GetBytes(pieceIndex).Reverse());
        message.AddRange(BitConverter.GetBytes(offset).Reverse());
        message.AddRange(BitConverter.GetBytes(length).Reverse());
        
        return [.. message];
    }

    private byte[] ReadMessage(byte messageId)
    {
        var messageLength = ReadMessageLength();
        var messageIdByte = _stream.ReadByte();

        if (messageIdByte != messageId)
        {
            throw new Exception($"Could not read messageId: {messageId}. Instead received {messageIdByte}");
        }

        var data = new byte[messageLength - 1];
        _stream.ReadExactly(data, 0, data.Length);
        return data;
    }

    private void SendMessage(byte[] message)
    {
        _stream.Write(message);
    }

    private int ReadMessageLength()
    {
        byte[] lengthBuffer = new byte[4];
        _stream.ReadExactly(lengthBuffer, 0, lengthBuffer.Length);
        return BitConverter.ToInt32(lengthBuffer.Reverse().ToArray(), 0);
    }

    private static bool VerifyPieceIntegrity(byte[] pieceBytes, string originalHash)
    {
        return Convert.ToHexString(SHA1.HashData(pieceBytes)).ToLower() ==
               originalHash;
    }
}
