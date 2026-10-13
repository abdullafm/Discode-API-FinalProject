using AutoMapper;
using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Exceptions;
using Service.Helpers.Responses;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IValidator<RegisterDto> _registerValidator;
        private readonly IValidator<VerifyEmailDto> _verifyValidator;
        private readonly IValidator<LoginDto> _loginValidator;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;

        public AccountService(
            IAccountRepository accountRepository,
            IValidator<RegisterDto> registerValidator,
            IValidator<VerifyEmailDto> verifyValidator,
            IValidator<LoginDto> loginValidator,
            ITokenService tokenService,
            IEmailService emailService,
            IMapper mapper)
        {
            _accountRepository = accountRepository;
            _registerValidator = registerValidator;
            _verifyValidator = verifyValidator;
            _loginValidator = loginValidator;
            _tokenService = tokenService;
            _emailService = emailService;
            _mapper = mapper;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterDto model)
        {
            var validation = await _registerValidator.ValidateAsync(model);
            if (!validation.IsValid)
                return Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());

            var userName = model.UserName.Trim();
            var email = model.Email.Trim();

            var existingEmail = await _accountRepository.GetByEmailAsync(email);
            if (existingEmail is not null)
            {
                if (existingEmail.EmailConfirmed)
                    return Fail("That email is already registered.");

                await SendVerificationCodeAsync(existingEmail);
                return new RegisterResponse
                {
                    IsSuccess = true,
                    RequiresEmailConfirmation = true,
                    Email = existingEmail.Email ?? email
                };
            }

            if (await _accountRepository.GetByUserNameAsync(userName) is not null)
                return Fail("That username is already taken.");

            var user = _mapper.Map<AppUser>(model);
            var result = await _accountRepository.RegisterAsync(user, model.Password);
            if (!result.Succeeded)
                return Fail(result.Errors.Select(e => e.Description).ToArray());

            var roleResult = await _accountRepository.AddToRoleAsync(user, Roles.Member);
            if (!roleResult.Succeeded)
                return Fail(roleResult.Errors.Select(e => e.Description).ToArray());

            var sent = await SendVerificationCodeAsync(user);
            if (!sent)
                return Fail("Account created, but the verification email could not be sent. Try resending the code.");

            return new RegisterResponse
            {
                IsSuccess = true,
                RequiresEmailConfirmation = true,
                Email = user.Email ?? model.Email.Trim()
            };
        }

        public async Task<RegisterResponse> VerifyEmailAsync(VerifyEmailDto model)
        {
            model.Code = DigitsOnly(model.Code);
            var validation = await _verifyValidator.ValidateAsync(model);
            if (!validation.IsValid)
                return Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());

            var user = await _accountRepository.GetByEmailAsync(model.Email.Trim());
            if (user is null)
                return Fail("Invalid verification code.");

            if (user.EmailConfirmed)
                return new RegisterResponse { IsSuccess = true, Email = user.Email ?? model.Email.Trim() };

            if (string.IsNullOrEmpty(user.EmailVerificationCode) ||
                user.EmailVerificationExpiresAt is null ||
                user.EmailVerificationExpiresAt < DateTime.UtcNow)
                return Fail("That code has expired. Request a new one.");

            if (!string.Equals(user.EmailVerificationCode, model.Code.Trim(), StringComparison.Ordinal))
                return Fail("That code is incorrect.");

            user.EmailConfirmed = true;
            user.EmailVerificationCode = null;
            user.EmailVerificationExpiresAt = null;
            var result = await _accountRepository.UpdateAsync(user);
            if (!result.Succeeded)
                return Fail(result.Errors.Select(e => e.Description).ToArray());

            return new RegisterResponse { IsSuccess = true, Email = user.Email ?? model.Email.Trim() };
        }

        public async Task<RegisterResponse> ResendCodeAsync(ResendCodeDto model)
        {
            var email = model.Email?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email))
                return Fail("Enter your email.");

            var user = await _accountRepository.GetByEmailAsync(email);
            if (user is null)
                return Fail("If this email is registered, a new code was sent.");

            if (user.EmailConfirmed)
                return Fail("This email is already confirmed. You can sign in.");

            var sent = await SendVerificationCodeAsync(user);
            if (!sent)
                return Fail("Could not send the verification email. Try again in a moment.");

            return new RegisterResponse
            {
                IsSuccess = true,
                RequiresEmailConfirmation = true,
                Email = user.Email ?? email
            };
        }

        public async Task<LoginResponse> LoginAsync(LoginDto model)
        {
            var validation = await _loginValidator.ValidateAsync(model);
            if (!validation.IsValid)
                return FailLogin(validation.Errors.Select(e => e.ErrorMessage).ToArray());

            var user = await _accountRepository.GetByEmailAsync(model.Email.Trim());
            if (user is null)
                return FailLogin("Invalid email or password.");

            if (!user.EmailConfirmed)
                return FailLogin("Confirm your email first. Enter the 4-digit code we sent you.");

            if (await _accountRepository.IsLockedOutAsync(user))
                return FailLogin("Too many failed attempts. Try again in a few minutes.");

            if (!await _accountRepository.CheckPasswordAsync(user, model.Password))
            {
                await _accountRepository.RegisterFailedAttemptAsync(user);
                return FailLogin("Invalid email or password.");
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

        public async Task<ProfileDto?> GetProfileAsync(string userId)
        {
            var user = await _accountRepository.GetByIdAsync(userId);
            return user is null ? null : _mapper.Map<ProfileDto>(user);
        }

        private static string DigitsOnly(string? value)
            => new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

        private async Task<bool> SendVerificationCodeAsync(AppUser user)
        {
            var code = Random.Shared.Next(1000, 10000).ToString();
            user.EmailVerificationCode = code;
            user.EmailVerificationExpiresAt = DateTime.UtcNow.AddMinutes(10);

            var updated = await _accountRepository.UpdateAsync(user);
            if (!updated.Succeeded)
                return false;

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

        private static RegisterResponse Fail(params string[] errors)
            => new() { IsSuccess = false, Errors = errors };

        private static LoginResponse FailLogin(params string[] errors)
            => new() { IsSuccess = false, Errors = errors };

        public async Task<UpdateProfileResponse> UpdateProfileAsync(string userId, UpdateProfileDto model)
        {
            var user = await _accountRepository.GetByIdAsync(userId);
            if (user is null)
                throw new NotFoundException("User not found.");

            var displayName = string.IsNullOrWhiteSpace(model.DisplayName) ? user.DisplayName : model.DisplayName.Trim();
            var userName = string.IsNullOrWhiteSpace(model.UserName) ? user.UserName ?? string.Empty : model.UserName.Trim();

            var existing = await _accountRepository.GetByUserNameAsync(userName);
            if (existing is not null && existing.Id != user.Id)
                return new UpdateProfileResponse
                {
                    IsSuccess = false,
                    Errors = new[] { "That username is already taken." }
                };

            user.DisplayName = displayName;
            user.UserName = userName;
            user.NormalizedUserName = userName.ToUpperInvariant();
            user.AvatarUrl = (model.AvatarUrl ?? string.Empty).Trim();
            user.CustomStatus = (model.CustomStatus ?? string.Empty).Trim();
            user.Pronouns = (model.Pronouns ?? string.Empty).Trim();
            user.Bio = (model.Bio ?? string.Empty).Trim();
            user.IsPublic = model.IsPublic;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _accountRepository.UpdateAsync(user);
            if (!result.Succeeded)
                return new UpdateProfileResponse
                {
                    IsSuccess = false,
                    Errors = result.Errors.Select(e => e.Description).ToArray()
                };

            return new UpdateProfileResponse
            {
                IsSuccess = true,
                Profile = _mapper.Map<ProfileDto>(user)
            };
        }

        public async Task<IEnumerable<UserSearchDto>> SearchUserAsync(string searchName, string currentUserId)
        {
            var getallUsers = await _accountRepository.SearchUserAsync(searchName);
            return getallUsers
                .Where(u => u.Id != currentUserId)
                .Select(u => _mapper.Map<UserSearchDto>(u));
        }
    }
}
