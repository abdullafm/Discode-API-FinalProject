namespace Service.Helpers.Responses
{
    public class RegisterResponse
    {
        public bool IsSuccess { get; set; }
        public bool RequiresEmailConfirmation { get; set; }
        public string Email { get; set; } = string.Empty;
        public string[] Errors { get; set; } = Array.Empty<string>();
    }
}
