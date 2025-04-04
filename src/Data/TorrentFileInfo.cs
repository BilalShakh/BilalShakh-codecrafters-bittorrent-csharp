using System.Text.Json.Serialization;

namespace codecrafters_bittorrent.src.Data;

public record TorrentFileInfo
{
    public long Length { get; set; }
    [JsonPropertyName("piece length")]
    public required int PieceLength { get; set; }
}
