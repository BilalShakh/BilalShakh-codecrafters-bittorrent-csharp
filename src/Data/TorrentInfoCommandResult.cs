namespace codecrafters_bittorrent.src.Data;

public record TorrentInfoCommandResult
{
    public required string TrackerUrl { get; set; }
    public required long Length { get; set; }
    public required string InfoHash { get; set; }
    public required int PieceLength { get; set; }
    public required string[] Pieces { get; set; }
    public required int FileLength { get; set; }
}
