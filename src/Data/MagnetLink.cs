namespace codecrafters_bittorrent.src.Data;

public sealed record MagnetLink
{
    public string InfoHash {  get; set; }
    public string Name { get; set; }
    public string TrackerURL { get; set; }
}
