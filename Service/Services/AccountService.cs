using AutoMapper;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Exceptions;
using Service.Helpers.Responses;
using Service.Helpers.Settings;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IValidator<RegisterDto> _registerValidator;
        private readonly IValidator<VerifyEmailDto> _verifyValidator;
        private readonly IValidator<LoginDto> _loginValidator;
        private readonly IValidator<ChangeUsernameDto> _changeUsernameValidator;
        private readonly IValidator<ChangePasswordDto> _changePasswordValidator;
        private readonly IValidator<ForgotPasswordDto> _forgotPasswordValidator;
        private readonly IValidator<ResetPasswordDto> _resetPasswordValidator;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly AppSettings _appSettings;

        private const string InvalidCredentials = "Invalid email or password.";

        public AccountService(IAccountRepository accountRepository,
                              IValidator<RegisterDto> registerValidator,
                              IValidator<VerifyEmailDto> verifyValidator,
                              IValidator<LoginDto> loginValidator,
                              IValidator<ChangeUsernameDto> changeUsernameValidator,
                              IValidator<ChangePasswordDto> changePasswordValidator,
                              IValidator<ForgotPasswordDto> forgotPasswordValidator,
                              IValidator<ResetPasswordDto> resetPasswordValidator,
                              ITokenService tokenService,
                              IEmailService emailService,
                              IMapper mapper,
                              IOptions<AppSettings> appSettings)
        {
            _accountRepository = accountRepository;
            _registerValidator = registerValidator;
            _verifyValidator = verifyValidator;
            _loginValidator = loginValidator;
            _changeUsernameValidator = changeUsernameValidator;
            _changePasswordValidator = changePasswordValidator;
            _forgotPasswordValidator = forgotPasswordValidator;
            _resetPasswordValidator = resetPasswordValidator;
            _tokenService = tokenService;
            _emailService = emailService;
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterDto model)
        {
            var validation = await _registerValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var userName = model.UserName.Trim();
            var email = model.Email.Trim();

            var existingEmail = await _accountRepository.GetByEmailAsync(email);
            if (existingEmail is not null)
            {
                if (existingEmail.EmailConfirmed)
                {
                    return Fail("That email is already registered.");
                }

                await SendVerificationCodeAsync(existingEmail);
                return new RegisterResponse
                {
                    IsSuccess = true,
                    RequiresEmailConfirmation = true,
                    Email = existingEmail.Email ?? email
                };
            }

            if (await _accountRepository.GetByUserNameAsync(userName) is not null)
            {
                return Fail("That username is already taken.");
            }

            var user = _mapper.Map<AppUser>(model);

            var result = await _accountRepository.RegisterAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return Fail(result.Errors.Select(e => e.Description).ToArray());
            }

            var roleResult = await _accountRepository.AddToRoleAsync(user, Roles.Member);
            if (!roleResult.Succeeded)
            {
                return Fail(roleResult.Errors.Select(e => e.Description).ToArray());
            }

            var sent = await SendVerificationCodeAsync(user);
            if (!sent)
            {
                return Fail("Account created, but the verification email could not be sent. Try resending the code.");
            }

            return new RegisterResponse
            {
                IsSuccess = true,
                RequiresEmailConfirmation = true,
                Email = user.Email ?? email
            };
        }

        public async Task<RegisterResponse> VerifyEmailAsync(VerifyEmailDto model)
        {
            var validation = await _verifyValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var user = await _accountRepository.GetByEmailAsync(model.Email.Trim());
            if (user is null)
            {
                return Fail("Invalid verification code.");
            }

            if (user.EmailConfirmed)
            {
                return new RegisterResponse
                {
                    IsSuccess = true,
                    Email = user.Email ?? model.Email.Trim()
                };
            }

            if (string.IsNullOrEmpty(user.EmailVerificationCode) ||
                user.EmailVerificationExpiresAt is null ||
                user.EmailVerificationExpiresAt < DateTime.UtcNow)
            {
                return Fail("That code has expired. Request a new one.");
            }

            if (!string.Equals(user.EmailVerificationCode, model.Code.Trim(), StringComparison.Ordinal))
            {
                return Fail("That code is incorrect.");
            }

            user.EmailConfirmed = true;
            user.EmailVerificationCode = null;
            user.EmailVerificationExpiresAt = null;
            var result = await _accountRepository.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return Fail(result.Errors.Select(e => e.Description).ToArray());
            }

            return new RegisterResponse
            {
                IsSuccess = true,
                Email = user.Email ?? model.Email.Trim()
            };
        }

        public async Task<RegisterResponse> ResendCodeAsync(ResendCodeDto model)
        {
            var email = model.Email?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email))
            {
                return Fail("Enter your email.");
            }

            var user = await _accountRepository.GetByEmailAsync(email);
            if (user is null)
            {
                return Fail("If this email is registered, a new code was sent.");
            }

            if (user.EmailConfirmed)
            {
                return Fail("This email is already confirmed. You can sign in.");
            }

            var sent = await SendVerificationCodeAsync(user);
            if (!sent)
            {
                return Fail("Could not send the verification email. Try again in a moment.");
            }

            return new RegisterResponse
            {
                IsSuccess = true,
                RequiresEmailConfirmation = true,
                Email = user.Email ?? email
            };
        }

        public async Task<RegisterResponse> ForgotPasswordAsync(ForgotPasswordDto model)
        {
            var validation = await _forgotPasswordValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var email = model.Email.Trim();
            var user = await _accountRepository.GetByEmailAsync(email);
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
            {
                return new RegisterResponse
                {
                    IsSuccess = true,
                    Email = email
                };
            }

            var token = await _accountRepository.GeneratePasswordResetTokenAsync(user);
            var link = BuildResetLink(model.ResetPageUrl, user.Email, token);

            try
            {
                await _emailService.SendPasswordResetAsync(user.Email, link);
            }
            catch
            {
                return Fail("Could not send the reset email. Try again in a moment.");
            }

            return new RegisterResponse
            {
                IsSuccess = true,
                Email = user.Email
            };
        }

        public async Task<RegisterResponse> ResetPasswordAsync(ResetPasswordDto model)
        {
            var validation = await _resetPasswordValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var user = await _accountRepository.GetByEmailAsync(model.Email.Trim());
            if (user is null)
            {
                return Fail("This reset link is invalid or has expired.");
            }

            if (await _accountRepository.CheckPasswordAsync(user, model.NewPassword))
            {
                return Fail("Choose a different password.");
            }

            var result = await _accountRepository.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (!result.Succeeded)
            {
                return Fail(result.Errors.Select(DescribeResetError).ToArray());
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                user.EmailVerificationCode = null;
                user.EmailVerificationExpiresAt = null;
                await _accountRepository.UpdateAsync(user);
            }

            return new RegisterResponse
            {
                IsSuccess = true,
                Email = user.Email ?? model.Email.Trim()
            };
        }

        public async Task<LoginResponse> LoginAsync(LoginDto model)
        {
            var validation = await _loginValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return FailLogin(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var user = await _accountRepository.GetByEmailAsync(model.Email.Trim());
            if (user is null)
            {
                return FailLogin(InvalidCredentials);
            }

            if (!user.EmailConfirmed)
            {
                return FailLogin("Confirm your email first. Enter the 4-digit code we sent you.");
            }

            if (await _accountRepository.IsLockedOutAsync(user))
            {
                return FailLogin("Too many failed attempts. Try again in a few minutes.");
            }

            if (!await _accountRepository.CheckPasswordAsync(user, model.Password))
            {
                await _accountRepository.RegisterFailedAttemptAsync(user);
                return FailLogin(InvalidCredentials);
            }

            await _accountRepository.ResetFailedAttemptsAsync(user);
            var roles = await _accountRepository.GetRolesAsync(user);
            var (token, expiresAt) = _tokenService.CreateToken(user, roles);

            var response = _mapper.Map<LoginResponse>(user);
            response.Token = token;
            response.ExpiresAt = expiresAt;
            response.Roles = roles.ToArray();
            return response;
        }

        public async Task<ProfileDto> GetProfileAsync(string userId)
        {
            var user = await _accountRepository.GetByIdAsync(userId);
            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            return _mapper.Map<ProfileDto>(user);
        }

        public async Task<UpdateProfileResponse> UpdateProfileAsync(string userId, UpdateProfileDto model)
        {
            var user = await _accountRepository.GetByIdAsync(userId);
            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            var displayName = string.IsNullOrWhiteSpace(model.DisplayName) ? user.DisplayName : model.DisplayName.Trim();

            user.DisplayName = displayName;
            user.AvatarUrl = (model.AvatarUrl ?? string.Empty).Trim();
            user.CustomStatus = (model.CustomStatus ?? string.Empty).Trim();
            user.Pronouns = (model.Pronouns ?? string.Empty).Trim();
            user.Bio = (model.Bio ?? string.Empty).Trim();
            user.IsPublic = model.IsPublic;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _accountRepository.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return new UpdateProfileResponse
                {
                    IsSuccess = false,
                    Errors = result.Errors.Select(e => e.Description).ToArray()
                };
            }

            return new UpdateProfileResponse
            {
                IsSuccess = true,
                Profile = _mapper.Map<ProfileDto>(user)
            };
        }

        public async Task<UpdateProfileResponse> ChangeUsernameAsync(string userId, ChangeUsernameDto model)
        {
            var validation = await _changeUsernameValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return FailProfile(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var user = await _accountRepository.GetByIdAsync(userId);
            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            if (!await _accountRepository.CheckPasswordAsync(user, model.CurrentPassword))
            {
                return FailProfile("Current password is incorrect.");
            }

            var userName = model.UserName.Trim();
            var existing = await _accountRepository.GetByUserNameAsync(userName);
            if (existing is not null && existing.Id != user.Id)
            {
                return FailProfile("That username is already taken.");
            }

            user.UserName = userName;
            user.NormalizedUserName = userName.ToUpperInvariant();
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _accountRepository.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return FailProfile(result.Errors.Select(e => e.Description).ToArray());
            }

            return new UpdateProfileResponse
            {
                IsSuccess = true,
                Profile = _mapper.Map<ProfileDto>(user)
            };
        }

        public async Task<UpdateProfileResponse> ChangePasswordAsync(string userId, ChangePasswordDto model)
        {
            var validation = await _changePasswordValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return FailProfile(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var user = await _accountRepository.GetByIdAsync(userId);
            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            if (!await _accountRepository.CheckPasswordAsync(user, model.CurrentPassword))
            {
                return FailProfile("Current password is incorrect.");
            }

            if (await _accountRepository.CheckPasswordAsync(user, model.NewPassword))
            {
                return FailProfile("Choose a different password.");
            }

            var result = await _accountRepository.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                return FailProfile(result.Errors.Select(e => e.Description).ToArray());
            }

            return new UpdateProfileResponse
            {
                IsSuccess = true
            };
        }

        public async Task<IEnumerable<UserSearchDto>> SearchUserAsync(string searchName, string currentUserId)
        {
            var users = await _accountRepository.SearchUserAsync(searchName);
            return _mapper.Map<IEnumerable<UserSearchDto>>(users.Where(u => u.Id != currentUserId));
        }

        public async Task<UserProfileDto> GetUserProfileAsync(string viewerId, string userName)
        {
            var result = await _accountRepository.GetUserProfileAsync(viewerId, userName.Trim());
            if (result is null)
            {
                throw new NotFoundException("User not found.");
            }

            var (user, request) = result.Value;
            var profile = _mapper.Map<UserProfileDto>(user);
            profile.Relation = GetRelation(viewerId, user.Id, request);
            profile.RequestId = profile.Relation == "pending-received" ? request!.Id : null;
            profile.IsPrivate = !user.IsPublic && profile.Relation != "self" && profile.Relation != "friend";

            if (profile.IsPrivate)
            {
                profile.Bio = string.Empty;
                profile.Pronouns = string.Empty;
                profile.CustomStatus = string.Empty;
            }

            return profile;
        }

        private async Task<bool> SendVerificationCodeAsync(AppUser user)
        {
            var code = Random.Shared.Next(1000, 10000).ToString();
            user.EmailVerificationCode = code;
            user.EmailVerificationExpiresAt = DateTime.UtcNow.AddMinutes(10);

            var updated = await _accountRepository.UpdateAsync(user);
            if (!updated.Succeeded)
            {
                return false;
            }

            try
            {
                await _emailService.SendVerificationCodeAsync(user.Email!, code);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string BuildResetLink(string? requestedPageUrl, string email, string token)
        {
            var page = IsAllowedResetPage(requestedPageUrl)
                ? requestedPageUrl!.Split('?')[0].TrimEnd('/')
                : $"{_appSettings.ClientUrl.TrimEnd('/')}/Account/ResetPassword";

            return $"{page}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        }

        private bool IsAllowedResetPage(string? url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            if (!uri.AbsolutePath.EndsWith("/Account/ResetPassword", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host == "127.0.0.1")
            {
                return true;
            }

            return Uri.TryCreate(_appSettings.ClientUrl, UriKind.Absolute, out var allowed)
                && uri.Host.Equals(allowed.Host, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetRelation(string viewerId, string userId, FriendRequest? request)
        {
            if (viewerId == userId)
            {
                return "self";
            }

            if (request is null)
            {
                return "none";
            }

            if (request.Status == FriendRequestStatus.Accepted)
            {
                return "friend";
            }

            return request.SenderId == viewerId ? "pending-sent" : "pending-received";
        }

        private static string DescribeResetError(IdentityError error)
        {
            return error.Code == "InvalidToken"
                ? "This reset link is invalid or has expired."
                : error.Description;
        }

        private static RegisterResponse Fail(params string[] errors)
        {
            return new()
            {
                IsSuccess = false,
                Errors = errors
            };
        }

        private static LoginResponse FailLogin(params string[] errors)
        {
            return new()
            {
                IsSuccess = false,
                Errors = errors
            };
        }

        private static UpdateProfileResponse FailProfile(params string[] errors)
        {
            return new()
            {
                IsSuccess = false,
                Errors = errors
            };
        }
    }
}
