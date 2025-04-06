using codecrafters_bittorrent.src.Data;
using System.Collections.Concurrent;
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
                await HandleTorrentFileDownloadPiece(args);
                break;
            case "download":
                await HandleTorrentFileDownload(args);
                break;
            case "magnet_parse":
                HandleMagnetParse(args);
                break;
            case "magnet_handshake":
                await HandleMagnetHandshake(args);
                break;
            case "magnet_info":
                await HandleMagnetInfo(args);
                break;
            case "magnet_download_piece":
                await HandleMagnetDownloadPiece(args);
                break;
            case "magnet_download":
                await HandleMagnetDownload(args);
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

        Console.WriteLine($"Peer ID: {Convert.ToHexString(response[(response.Length - 20)..]).ToLower()}");
    }

    private static async Task HandleTorrentFileDownloadPiece(string[] args)
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

    private static async Task HandleTorrentFileDownload(string[] args)
    {
        string downloadPath = args[2];
        string torrentFileName = args[3];

        List<Peer> peers = await GetPeers(torrentFileName);
        TorrentInfoCommandResult torrentInfo = GetTorrentInfo(torrentFileName);

        await DownloadFile(downloadPath, peers, torrentInfo);

        Console.WriteLine($"Download completed and saved to {downloadPath}");
    }

    private static async Task HandleMagnetDownloadPiece(string[] args)
    {
        string downloadPath = args[2];
        string magnetLink = args[3];
        int pieceIndex = int.Parse(args[4]);
        MagnetLink parsedMagnetLink = ParseMagnetLink(magnetLink);
        List<Peer> peers = await GetMagnetPeers(parsedMagnetLink);
        TorrentInfoCommandResult torrentInfo = await GetMagnetInfo(magnetLink);

        PeerClient peerClient = new(peers[0].IP, peers[0].Port);
        byte[] infoHash = Convert.FromHexString(parsedMagnetLink.InfoHash);
        byte[] peerId = Encoding.ASCII.GetBytes(Utils.Generate20DigitRandomNumber());
        peerClient.PerformHandshake(infoHash, peerId);

        byte[] pieceBytes = peerClient.DownloadPiece(torrentInfo, pieceIndex);
        File.WriteAllBytes(downloadPath, pieceBytes);
        Console.WriteLine($"Piece {pieceIndex} downloaded to {downloadPath}");
    }

    private static async Task HandleMagnetDownload(string[] args)
    {
        string downloadPath = args[2];
        string magnetLink = args[3];
        MagnetLink parsedMagnetLink = ParseMagnetLink(magnetLink);
        List<Peer> peers = await GetMagnetPeers(parsedMagnetLink);
        TorrentInfoCommandResult torrentInfo = await GetMagnetInfo(magnetLink);

        await DownloadFile(downloadPath, peers, torrentInfo);

        Console.WriteLine($"Download completed and saved to {downloadPath}");
    }

    private static void HandleMagnetParse(string[] args)
    {
        string magnetLink = args[1];
        MagnetLink parsedMagnetLink = ParseMagnetLink(magnetLink);

        Console.WriteLine($"Tracker URL: {parsedMagnetLink.TrackerURL}");
        Console.WriteLine($"Info Hash: {parsedMagnetLink.InfoHash}");
    }

    private static async Task HandleMagnetHandshake(string[] args)
    {
        string magnetLink = args[1];
        var (peerId, supportsExtensions, handshakeResponseString, _) = await PerformMagnetLinkHandshake(magnetLink);

        Console.WriteLine($"Peer ID: {peerId}");

        if (supportsExtensions)
        {
            // Deserialize the JSON string into a nested dictionary
            Decode.DecodeInput(handshakeResponseString, 2, out string decodedHandshakeResponse);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var decodedDict = JsonSerializer.Deserialize<Dictionary<string, object>>(decodedHandshakeResponse, options);

            if (decodedDict != null && decodedDict.TryGetValue("m", out var mValue) && mValue is JsonElement mElement)
            {
                var mDict = JsonSerializer.Deserialize<Dictionary<string, int>>(mElement.GetRawText(), options);
                if (mDict != null && mDict.TryGetValue("ut_metadata", out int utMetadata))
                {
                    Console.WriteLine($"Peer Metadata Extension ID: {utMetadata}");
                }
            }
        }
    }

    private static async Task HandleMagnetInfo(string[] args)
    {
        string magnetLink = args[1];
        TorrentInfoCommandResult info = await GetMagnetInfo(magnetLink);

        Console.WriteLine($"Tracker URL: {info.TrackerUrl}");
        Console.WriteLine($"Length: {info.Length}");
        Console.WriteLine($"Info Hash: {info.InfoHash}");
        Console.WriteLine($"Piece Length: {info.PieceLength}");
        Console.WriteLine("Pieces:");
        foreach (string piece in info.Pieces)
        {
            Console.WriteLine($"  {piece}");
        }
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

    private static async Task<TorrentInfoCommandResult> GetMagnetInfo(string magnetLink)
    {
        string piecesMarker = "6:pieces";
        MagnetLink parsedMagnetLink = ParseMagnetLink(magnetLink);
        var (peerId, supportsExtensions, handshakeResponseString, peerClient) = await PerformMagnetLinkHandshake(magnetLink);
        TorrentFileInfo? metaDataDict = null;
        string[] pieces = [];
        byte[] metaDataDictBytes = [];
        
        if (supportsExtensions)
        {
            Decode.DecodeInput(handshakeResponseString, 2, out string decodedHandshakeResponse);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var decodedDict = JsonSerializer.Deserialize<Dictionary<string, object>>(decodedHandshakeResponse, options);

            if (decodedDict != null && decodedDict.TryGetValue("m", out var mValue) && mValue is JsonElement mElement)
            {
                var mDict = JsonSerializer.Deserialize<Dictionary<string, int>>(mElement.GetRawText(), options);
                if (mDict != null && mDict.TryGetValue("ut_metadata", out int utMetadata))
                {
                    int metadataId = utMetadata;
                    metaDataDictBytes = peerClient.MakeMetadataRequest(0, metadataId);
                    string metaDataDictString = Encoding.ASCII.GetString(metaDataDictBytes);
                    int piecesIndex = metaDataDictString.IndexOf(piecesMarker);
                    Decode.DecodeInput(metaDataDictString[..piecesIndex] + "e", 0, out string decodedMetaDataDict);

                    metaDataDict = JsonSerializer.Deserialize<TorrentFileInfo>(decodedMetaDataDict, options);
                    pieces = GetPieceStrings(metaDataDictString, metaDataDictBytes);
                }
            }
        }

        return new TorrentInfoCommandResult
        {
            TrackerUrl = parsedMagnetLink.TrackerURL,
            Length = metaDataDict.Length,
            InfoHash = parsedMagnetLink.InfoHash,
            PieceLength = metaDataDict.PieceLength,
            Pieces = pieces,
            FileLength = metaDataDictBytes.Length
        };
    }

    private static TorrentInfoCommandResult GetTorrentInfo(string fileName)
    {
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

    private static MagnetLink ParseMagnetLink(string magnetLink)
    {
        if (magnetLink[magnetLink.Length - 1] != '&')
        {
            magnetLink += "&";
        }
        string infoHash = Utils.GetContainedSubstring(magnetLink, "xt=urn:btih:", "&dn=");
        string Name = Utils.GetContainedSubstring(magnetLink, "dn=", "&tr=");
        string TrackerURL = Utils.GetContainedSubstring(magnetLink, "tr=", "&");
        return new MagnetLink
        {
            InfoHash = infoHash,
            Name = Name,
            TrackerURL = Uri.UnescapeDataString(TrackerURL)
        };
    }

    private static async Task<List<Peer>> GetMagnetPeers(MagnetLink magnetLink)
    {
        byte[] infoHashBytes = Convert.FromHexString(magnetLink.InfoHash);
        string infoHash = HttpUtility.UrlEncode(infoHashBytes);
        string peerId = Utils.Generate20DigitRandomNumber();
        int port = 6881;
        int uploaded = 0;
        int downloaded = 0;
        long left = 999;
        int compact = 1;

        string query = $"?info_hash={infoHash}&peer_id={peerId}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&compact={compact}";
        string url = $"{magnetLink.TrackerURL}{query}";

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

    private static async Task<(string peerId, bool supportsExtensions, string handshakeResponseString, PeerClient peerClient)> PerformMagnetLinkHandshake(string magnetLink)
    {
        MagnetLink parsedMagnetLink = ParseMagnetLink(magnetLink);
        List<Peer> peers = await GetMagnetPeers(parsedMagnetLink);

        PeerClient peerClient = new(peers[0].IP, peers[0].Port);
        byte[] infoHash = Convert.FromHexString(parsedMagnetLink.InfoHash);
        byte[] peerIdBytes = Encoding.ASCII.GetBytes(Utils.Generate20DigitRandomNumber());
        byte[] response = peerClient.PerformHandshake(infoHash, peerIdBytes, true);

        string responseString = Convert.ToHexString(response);
        string extensionsString = responseString[40..56];
        bool supportsExtensions = extensionsString[10] == '1';
        string peerId = Convert.ToHexString(response[(response.Length - 20)..]).ToLower();

        peerClient.ReadMessageType(MessageTypes.Bitfield);
        string handshakeResponseString = string.Empty;

        if (supportsExtensions)
        {
            byte[] handshakeResponse = peerClient.PerformExtensionHandshake();
            handshakeResponseString = Encoding.UTF8.GetString(handshakeResponse);
        }

        return (peerId, supportsExtensions, handshakeResponseString, peerClient);
    }

    private static async Task DownloadFile(string downloadPath, List<Peer> peers, TorrentInfoCommandResult torrentInfo)
    {
        ConcurrentQueue<Peer> peerQueue = new(peers);

        byte[] infoHash = Convert.FromHexString(torrentInfo.InfoHash);
        byte[] peerId = Encoding.ASCII.GetBytes(Utils.Generate20DigitRandomNumber());

        List<byte[]> piecesBytes = [.. new byte[torrentInfo.Pieces.Length][]];
        List<Task> downloadTasks = [];

        for (int i = 0; i < torrentInfo.Pieces.Length; i++)
        {
            int pieceIndex = i;
            downloadTasks.Add(Task.Run(async () =>
            {
                while (true)
                {
                    if (peerQueue.TryDequeue(out Peer peer))
                    {
                        try
                        {
                            PeerClient peerClient = new(peer.IP, peer.Port);
                            peerClient.PerformHandshake(infoHash, peerId);
                            byte[] pieceBytes = peerClient.DownloadPiece(torrentInfo, pieceIndex);
                            piecesBytes[pieceIndex] = pieceBytes;
                            peerQueue.Enqueue(peer);
                            peerClient.Stop();
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Error downloading piece {pieceIndex} from {peer.IP}:{peer.Port} - {ex.Message}");
                            peerQueue.Enqueue(peer);
                        }
                    }
                    else
                    {
                        await Task.Delay(100); // Wait for a peer to become available
                    }
                }
            }));
        }

        await Task.WhenAll(downloadTasks);

        byte[] fileBytes = piecesBytes.SelectMany(x => x).ToArray();
        File.WriteAllBytes(downloadPath, fileBytes);
    }
}
