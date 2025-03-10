using System.Text.Json;

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
    Console.Error.WriteLine("Logs from your program will appear here!");

    // Uncomment this line to pass the first stage
    var encodedValue = param;
    int encodeValueLength = encodedValue.Length;
    if (Char.IsDigit(encodedValue[0]))
    {
       // Example: "5:hello" -> "hello"
       var colonIndex = encodedValue.IndexOf(':');
       if (colonIndex != -1)
       {
           var strLength = int.Parse(encodedValue[..colonIndex]);
           var strValue = encodedValue.Substring(colonIndex + 1, strLength);
           Console.WriteLine(JsonSerializer.Serialize(strValue));
       }
       else
       {
           throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
       }
    }
    else if (encodedValue[0] == 'i' && encodedValue[encodeValueLength - 1] == 'e')
    {
        var strValue = encodedValue.Substring(1, encodeValueLength-2);
        var numVal = long.Parse(strValue);
        Console.WriteLine($"{numVal}");
    }
    else
    {
       throw new InvalidOperationException("Unhandled encoded value: " + encodedValue);
    }
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
