using Domain.Common;

namespace Domain.Entities
{
    // A server mute / deafen set by a moderator. It belongs to the person and the server, not to the membership:
    // leaving and rejoining the server, or leaving and joining a voice channel, does not lift it.
    public class ServerVoiceRestriction : BaseEntity
    {
        public int ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public bool Muted { get; set; }
        public bool Deafened { get; set; }
        // who set it last: a person who muted themselves may lift it themselves, a mute set by someone else only a moderator may lift
        public string ByUserId { get; set; } = string.Empty;
    }
}
