using codecrafters_bittorrent.src.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace codecrafters_bittorrent.src;

class CommandHandler
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly HttpClient httpClient = new();

    public static async Task HandleCommand(string[] args)
    {
        string command = args[0];

        switch (command)
        {
            case "decode":
                HandleDecode(args);
                break;
            case "info":
                HandleInfo(args);
                break;
            case "peers":
                await HandlePeers(args);
                break;
            case "handshake":
                HandleHandshake(args);
                break;
            case "download_piece":
                await HandleDownloadPiece(args);
                break;
            default:
                throw new InvalidOperationException($"Invalid command: {command}");
        }
    }

    private static void HandleDecode(string[] args)
    {
        string param = args[1];
        Console.Error.WriteLine($"Decoding: {param}");
        Decode.DecodeInput(param, 0, out string result);
        Console.WriteLine(result);
    }

    private static void HandleInfo(string[] args)
    {
        string param = args[1];
        TorrentInfoCommandResult torrentInfo = GetTorrentInfo(param);
        Console.WriteLine($"Tracker URL: {torrentInfo.TrackerUrl}");
        Console.WriteLine($"Length: {torrentInfo.Length}");
        Console.WriteLine($"Info Hash: {torrentInfo.InfoHash}");
        Console.WriteLine($"Piece Length: {torrentInfo.PieceLength}");
        Console.WriteLine("Pieces:");
        foreach (string piece in torrentInfo.Pieces)
        {
            Console.WriteLine($"  {piece}");
        }
    }

    private static async Task HandlePeers(string[] args)
    {
        List<Peer> peers = await GetPeers(args[1]);
        foreach (Peer peer in peers)
        {
            Console.WriteLine(peer.ToString());
        }
    }

    private static void HandleHandshake(string[] args)
    {
        string param = args[1];
        string peerDetails = args[2];
        string[] peerParts = peerDetails.Split(':');
        string ip = peerParts[0];
        int port = int.Parse(peerParts[1]);
        TorrentInfoCommandResult torrentInfo = GetTorrentInfo(param);

        PeerClient peerClient = new(ip, port);
        byte[] infoHash = Convert.FromHexString(torrentInfo.InfoHash);
        byte[] peerId = Encoding.ASCII.GetBytes(Utils.Generate20DigitRandomNumber());
        byte[] response = peerClient.PerformHandshake(infoHash, peerId);

        Console.WriteLine($"Peer ID: {Convert.ToHexString(response).ToLower()}");
    }

    private static async Task HandleDownloadPiece(string[] args)
    {
        string downloadPath = args[2];
        string torrentFileName = args[3];
        int pieceIndex = int.Parse(args[4]);

        List<Peer> peers = await GetPeers(torrentFileName);
        TorrentInfoCommandResult torrentInfo = GetTorrentInfo(torrentFileName);
        PeerClient peerClient = new(peers[0].IP, peers[0].Port);

        byte[] infoHash = Convert.FromHexString(torrentInfo.InfoHash);
        byte[] peerId = Encoding.ASCII.GetBytes(Utils.Generate20DigitRandomNumber());
        peerClient.PerformHandshake(infoHash, peerId);

        byte[] pieceBytes = peerClient.DownloadPiece(torrentInfo, pieceIndex);
        File.WriteAllBytes(downloadPath, pieceBytes);
        
        Console.WriteLine($"Piece {pieceIndex} downloaded to {downloadPath}");
    }

    private static string GetInfoHash(string fileContentsString, byte[] fileContents)
    {
        string infoMarker = "4:infod";
        int hashingStartIndex = fileContentsString.IndexOf(infoMarker) + infoMarker.Length - 1;
        byte[] fileContentsToHash = fileContents[hashingStartIndex..^1];
        byte[] hashedFileContents = SHA1.HashData(fileContentsToHash);
        return Convert.ToHexString(hashedFileContents).ToLower();
    }

    private static string[] GetPieceStrings(string fileContentsString, byte[] fileContents)
    {
        List<string> pieceStrings = new();
        string piecesMarker = "6:pieces";
        int piecesStartIndex = fileContentsString.IndexOf(piecesMarker) + piecesMarker.Length;
        int lengthStartIndex = piecesStartIndex;
        while (char.IsDigit(fileContentsString[lengthStartIndex]))
        {
            lengthStartIndex++;
        }
        
        int piecesLength = int.Parse(fileContentsString[piecesStartIndex..lengthStartIndex]);
        int piecesDataStartIndex = lengthStartIndex + 1; // Skip the ':' character
        byte[] pieces = fileContents[piecesDataStartIndex..(piecesDataStartIndex + piecesLength)];

        for (int i = 0; i < pieces.Length; i += 20)
        {
            byte[] pieceHash = pieces[i..(i + 20)];
            pieceStrings.Add(Convert.ToHexString(pieceHash).ToLower());
        }

        return pieceStrings.ToArray();
    }

    private static TorrentInfoCommandResult GetTorrentInfo(string fileName)
    {
        Console.Error.WriteLine($"Getting info for: {fileName}");
        byte[] fileContents = File.ReadAllBytes(fileName);
        string fileContentsString = Encoding.ASCII.GetString(fileContents);
        Decode.DecodeInput(fileContentsString, 0, out string decodedFileContents);
        TorrentFile torrentFile = JsonSerializer.Deserialize<TorrentFile>(decodedFileContents, jsonSerializerOptions)!;

        string InfoHash = GetInfoHash(fileContentsString, fileContents);

        string[] pieceStrings = GetPieceStrings(fileContentsString, fileContents);

        return new TorrentInfoCommandResult
        {
            TrackerUrl = torrentFile.Announce,
            Length = torrentFile.Info.Length,
            InfoHash = InfoHash,
            PieceLength = torrentFile.Info.PieceLength,
            Pieces = pieceStrings,
            FileLength = fileContents.Length
        };
    }

    private static async Task<List<Peer>> GetPeers(string fileName)
    {
        TorrentInfoCommandResult torrentInfo = GetTorrentInfo(fileName);

        byte[] infoHashBytes = Convert.FromHexString(torrentInfo.InfoHash);
        string infoHash = HttpUtility.UrlEncode(infoHashBytes);
        string peerId = Utils.Generate20DigitRandomNumber();
        int port = 6881;
        int uploaded = 0;
        int downloaded = 0;
        long left = torrentInfo.Length;
        int compact = 1;

        string query = $"?info_hash={infoHash}&peer_id={peerId}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&compact={compact}";
        string url = $"{torrentInfo.TrackerUrl}{query}";

        HttpResponseMessage response = await httpClient.GetAsync(url);
        byte[] responseBody = await response.Content.ReadAsByteArrayAsync();
        string responseString = Encoding.ASCII.GetString(responseBody);

        string peersMarker = "5:peers";
        int peersStartIndex = responseString.IndexOf(peersMarker) + peersMarker.Length;
        int lengthStartIndex = peersStartIndex;
        while (char.IsDigit(responseString[lengthStartIndex]))
        {
            lengthStartIndex++;
        }

        int peersLength = int.Parse(responseString[peersStartIndex..lengthStartIndex]);
        int peersDataStartIndex = lengthStartIndex + 1; // Skip the ':' character
        byte[] peers = responseBody[peersDataStartIndex..(peersDataStartIndex + peersLength)];

        List<byte[]> peerList = [];
        for (int i = 0; i < peers.Length; i += 6)
        {
            byte[] peer = new byte[6];
            Array.Copy(peers, i, peer, 0, 6);
            peerList.Add(peer);
        }

        List<Peer> peersList = [];
        foreach (byte[] peer in peerList)
        {
            string ip = $"{peer[0]}.{peer[1]}.{peer[2]}.{peer[3]}";
            int peerPort = (peer[4] << 8) + peer[5];
            peersList.Add(new Peer(ip, peerPort));
        }

        return peersList;
    }
}
