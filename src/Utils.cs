using System.Text;

namespace codecrafters_bittorrent.src;

class Utils
{
    public static string Generate20DigitRandomNumber()
    {
        Random random = new();
        StringBuilder sb = new StringBuilder(20);

        for (int i = 0; i < 20; i++)
        {
            sb.Append(random.Next(0, 10)); // Generate a random digit between 0 and 9
        }

        return sb.ToString();
    }
}
