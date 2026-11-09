namespace Service.Helpers.DTOs.Nitro
{
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
