using Service.Helpers.DTOs.Accounts;

namespace Service.Helpers.Responses
{
    public class UpdateProfileResponse
    {
        public bool IsSuccess { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();
        public ProfileDto? Profile { get; set; }
    }
}
