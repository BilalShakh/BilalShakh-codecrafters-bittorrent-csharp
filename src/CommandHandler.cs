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

                string infoMarker = "4:infod";
                int hashingStartIndex = fileContentsString.IndexOf(infoMarker) + infoMarker.Length - 1;
                byte[] fileContentsToHash = fileContents[hashingStartIndex..^1];
                byte[] hashedFileContents = SHA1.HashData(fileContentsToHash);
                string InfoHash = Convert.ToHexString(hashedFileContents).ToLower();

                Console.WriteLine($"Tracker URL: {torrentFile.Announce}");
                Console.WriteLine($"Length: {torrentFile.Info.Length}");
                Console.WriteLine($"Info Hash: {InfoHash}");
                break;
            default:
                throw new InvalidOperationException($"Invalid command: {command}");
        }
    }
}
