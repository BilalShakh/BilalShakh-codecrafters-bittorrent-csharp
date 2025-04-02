using codecrafters_bittorrent.src.Data;
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
                Console.WriteLine($"Tracker URL: {torrentFile.Announce}");
                Console.WriteLine($"Length: {torrentFile.Info.Length}");
                break;
            default:
                throw new InvalidOperationException($"Invalid command: {command}");
        }
    }
}
