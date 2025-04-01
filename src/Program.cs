using codecrafters_bittorrent.src;

// Parse arguments
var (command, param) = args.Length switch
{
    0 => throw new InvalidOperationException("Usage: your_bittorrent.sh <command> <param>"),
    1 => throw new InvalidOperationException("Usage: your_bittorrent.sh <command> <param>"),
    _ => (args[0], args[1])
};

// Parse command and act accordingly
if (command == "decode")
{
    // You can use print statements as follows for debugging, they'll be visible when running tests.
    string result = string.Empty;
    Decode.DecodeInput(param, 0, out result);
    Console.WriteLine(result);
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
