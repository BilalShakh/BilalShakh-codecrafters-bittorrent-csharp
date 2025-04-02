namespace codecrafters_bittorrent.src.Data
{
    public record TorrentFile
    {
        public required string Announce { get; set; }
        public required TorrentFileInfo Info { get; set; }
    }
}
