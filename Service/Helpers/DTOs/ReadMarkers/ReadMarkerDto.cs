namespace Service.Helpers.DTOs.ReadMarkers
{
    public class ReadMarkerDto
    {
        // null: this chat has never been read here, so nothing is "new"
        public DateTime? ReadAt { get; set; }
    }
}
