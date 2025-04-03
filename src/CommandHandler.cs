using codecrafters_bittorrent.src.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace codecrafters_bittorrent.src;

class CommandHandler
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public static void HandleCommand(string[] args)
    {
        string command = args[0];
        string param = args.Length > 1 ? args[1] : string.Empty;

        switch (command)
        {
            case "decode":
                Console.Error.WriteLine($"Decoding: {param}");
                Decode.DecodeInput(param, 0, out string result);
                Console.WriteLine(result);
                break;
            case "info":
                Console.Error.WriteLine($"Getting info for: {param}");
                byte[] fileContents = File.ReadAllBytes(param);
                string fileContentsString = Encoding.ASCII.GetString(fileContents);
                Decode.DecodeInput(fileContentsString, 0, out string decodedFileContents);
                TorrentFile torrentFile = JsonSerializer.Deserialize<TorrentFile>(decodedFileContents, jsonSerializerOptions)!;

                string InfoHash = GetInfoHash(fileContentsString, fileContents);

                Console.WriteLine($"Tracker URL: {torrentFile.Announce}");
                Console.WriteLine($"Length: {torrentFile.Info.Length}");
                Console.WriteLine($"Info Hash: {InfoHash}");
                Console.WriteLine($"Piece Length: {torrentFile.Info.PieceLength}");
                Console.WriteLine("Piece Hashes:");

                string[] pieceStrings = GetPieceStrings(fileContentsString, fileContents);
                foreach (string pieceString in pieceStrings)
                {
                    Console.WriteLine(pieceString);
                }
                break;
            default:
                throw new InvalidOperationException($"Invalid command: {command}");
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
}
