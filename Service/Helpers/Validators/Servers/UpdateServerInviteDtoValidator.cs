using FluentValidation;
using Service.Helpers.DTOs.Servers;
using System.Text.RegularExpressions;

namespace Service.Helpers.Validators.Servers
{
    public class UpdateServerInviteDtoValidator : AbstractValidator<UpdateServerInviteDto>
    {
        public static readonly string[] Reserved =
        {
            "account", "api", "assets", "admin", "dm", "emailtemplates", "friend", "group", "home", "hubs",
            "login", "logout", "nitro", "notification", "register", "server", "servers", "social", "swagger", "uploads"
        };

        public UpdateServerInviteDtoValidator()
        {
            RuleFor(x => x.InviteCode)
                .Must(code => !string.IsNullOrWhiteSpace(code)).WithMessage("Enter a link name.")
                .Must(code => code is null || Regex.IsMatch(code.Trim(), "^[A-Za-z0-9_-]{3,32}$")).WithMessage("Use 3 to 32 letters, numbers, dashes or underscores.")
                .Must(code => code is null || !Reserved.Contains(code.Trim().ToLowerInvariant())).WithMessage("That link name is reserved.");
        }
    }
}
