using System.Text;
using System.Text.Json;

namespace codecrafters_bittorrent.src;

class Decode
{
    public static int DecodeInput(string data, int start, out string result)
    {
        if (data[start] == 'i')
        {
            return DecodeInt(data, start, out result);
        }
        else if (char.IsDigit(data[start]))
        {
            return DecodeString(data, start, out result);
        }
        else if (data[start] == 'l')
        {
            return DecodeList(data, start, out result);
        }
        else
        {
            throw new Exception("Invalid data");
        }
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
        return start + length + 2;
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
