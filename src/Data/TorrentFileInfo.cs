﻿using System.Text.Json.Serialization;

namespace codecrafters_bittorrent.src.Data;

public record TorrentFileInfo
{
    public long Length { get; set; }
    public string? Pieces { get; set; }
    [JsonPropertyName("piece length")]
    public int? PieceLength { get; set; }
}
