namespace codecrafters_bittorrent.src.Data;

public record TorrentFileInfo
{
    public long Length { get; set; }
    public string? Name { get; set; }
    public string? Pieces { get; set; }
}
