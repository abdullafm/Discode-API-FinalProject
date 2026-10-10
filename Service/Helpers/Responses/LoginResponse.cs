namespace Service.Helpers.Responses
{
    public class LoginResponse
    {
        public bool IsSuccess { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();
        public string? Token { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? DisplayName { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
    }
}
