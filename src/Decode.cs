using System.Text;
using System.Text.Json;

namespace codecrafters_bittorrent.src;

class Decode
{
    public static int DecodeInput(string data, int start, out string result)
    {
        Console.Error.WriteLine($"Decoding input: {data} at position {start}");
        switch (data[start])
        {
            case 'i':
                return DecodeInt(data, start, out result);
            case 'l':
                return DecodeList(data, start, out result);
            case 'd':
                return DecodeDictionary(data, start, out result);
            default:
                if (char.IsDigit(data[start]))
                {
                    return DecodeString(data, start, out result);
                }
                else
                {
                    throw new Exception("Invalid data");
                }
        }
    }

    private static int DecodeDictionary(string data, int start, out string result)
    {
        Console.Error.WriteLine($"Decoding dictionary: {data} at position {start}");
        Dictionary<string, string> resultDict = [];
        start++;

        while (data[start] != 'e')
        {
            start = DecodeString(data, start, out string key);
            start = DecodeInput(data, start, out string value);
            resultDict.Add(key, value);
        }

        result = "{" + string.Join(',', resultDict.Select(kv => $"{kv.Key}:{kv.Value}")) + "}";
        return start + 1;
    }

    private static int DecodeList(string data, int start, out string result)
    {
        List<string> resultList = [];
        start++;
        
        while (data[start] != 'e')
        {
            string decodedResult;
            start = DecodeInput(data, start, out decodedResult);
            resultList.Add(decodedResult);
        }

        result = string.Format("[{0}]", string.Join(',', resultList));
        return start + 1;
    }

    private static int DecodeString(string data, int start, out string result)
    {
        StringBuilder lengthSB = new();
        int i = start;

        while (char.IsDigit(data[i])) {
            lengthSB.Append(data[i++]);
        }

        int length = int.Parse(lengthSB.ToString());
        result = JsonSerializer.Serialize(data.Substring(start + lengthSB.Length + 1, length));
        return start + length + lengthSB.Length + 1;
    }

    private static int DecodeInt(string data, int start, out string result)
    {
        StringBuilder sb = new();
        
        for (int i = start+1; i < data.Length; i++)
        {
            if (data[i] == 'e')
            {
                break;
            }
            sb.Append(data[i]);
        }

        result = sb.ToString();

        return start + sb.Length + 2;
    }
}
