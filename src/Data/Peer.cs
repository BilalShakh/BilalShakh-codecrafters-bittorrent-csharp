namespace codecrafters_bittorrent.src.Data;

class Peer(string ip, int port)
{
    public string IP { get; set; } = ip;
    public int Port { get; set; } = port;

    public override string ToString()
    {
        return $"{IP}:{Port}";
    }
}
