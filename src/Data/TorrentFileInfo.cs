namespace codecrafters_bittorrent.src.Data;

public record TorrentFileInfo
{
    public long Length { get; set; }
    public string? Pieces { get; set; }
    public string? PieceLength { get; set; }
}
