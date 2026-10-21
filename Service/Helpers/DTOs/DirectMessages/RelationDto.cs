namespace Service.Helpers.DTOs.DirectMessages
{
    public class RelationDto
    {
        public string Relation { get; set; } = "none";
        public int? FriendRequestId { get; set; }
        public int? BlockId { get; set; }
    }
}
