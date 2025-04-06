using System.Text;

namespace codecrafters_bittorrent.src
{
    class Encode
    {
        public static string EncodeDictionary(Dictionary<string, string> dict)
        {
            StringBuilder sb = new StringBuilder("d");
            foreach (var kvp in dict)
            {
                sb.Append(EncodeString(kvp.Key));
                if (IsEncoded(kvp.Value))
                {
                    sb.Append(kvp.Value);
                }
                else
                {
                    if (int.TryParse(kvp.Value, out int res))
                    {
                        sb.Append(EncodeInt(res));
                    }
                    else
                    {
                        sb.Append(EncodeString(kvp.Value));
                    }
                }
            }
            sb.Append("e");
            return sb.ToString();
        }

        public static string EncodeString(string str)
        {
            return $"{str.Length}:{str}";
        }

        public static string EncodeInt(int value)
        {
            return $"i{value}e";
        }

        private static bool IsEncoded(string str)
        {
            if (str.Length < 2)
                return false;
            if (char.IsDigit(str[0]))
            {
                int colonIndex = str.IndexOf(':');
                if (colonIndex == -1)
                    return false;
                int length = int.Parse(str.Substring(0, colonIndex));
                return str.Length == length + colonIndex + 1;
            }
            else if (str[0] == 'd' || str[0] == 'l')
            {
                return str.EndsWith("e");
            }
            return false;
        }
    }
}
