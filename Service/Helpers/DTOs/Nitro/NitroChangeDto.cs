namespace Service.Helpers.DTOs.Nitro
{
    // Who has to be told (live) that something about Nitro or a boost changed
    public class NitroChangeDto
    {
        public List<string> UserIds { get; set; } = new();
        public List<int> ServerIds { get; set; } = new();

        public void Merge(NitroChangeDto other)
        {
            UserIds.AddRange(other.UserIds.Where(id => !UserIds.Contains(id)));
            ServerIds.AddRange(other.ServerIds.Where(id => !ServerIds.Contains(id)));
        }
    }
}
