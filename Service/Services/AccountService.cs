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
        private readonly IValidator<LoginDto> _loginValidator;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public AccountService(
            IAccountRepository accountRepository,
            IValidator<RegisterDto> registerValidator,
            IValidator<LoginDto> loginValidator,
            ITokenService tokenService,
            IMapper mapper)
        {
            _accountRepository = accountRepository;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterDto model)
        {
            var validation = await _registerValidator.ValidateAsync(model);
            if (!validation.IsValid)
                return Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());

            var userName = model.UserName.Trim();
            if (await _accountRepository.GetByUserNameAsync(userName) is not null)
                return Fail("That username is already taken.");

            if (await _accountRepository.GetByEmailAsync(model.Email.Trim()) is not null)
                return Fail("That email is already registered.");

            var user = _mapper.Map<AppUser>(model);
            var result = await _accountRepository.RegisterAsync(user, model.Password);
            if (!result.Succeeded)
                return Fail(result.Errors.Select(e => e.Description).ToArray());

            var roleResult = await _accountRepository.AddToRoleAsync(user, Roles.Member);
            if (!roleResult.Succeeded)
                return Fail(roleResult.Errors.Select(e => e.Description).ToArray());

            return new RegisterResponse { IsSuccess = true };
        }

        public async Task<LoginResponse> LoginAsync(LoginDto model)
        {
            var validation = await _loginValidator.ValidateAsync(model);
            if (!validation.IsValid)
                return FailLogin(validation.Errors.Select(e => e.ErrorMessage).ToArray());

            var user = await _accountRepository.GetByEmailAsync(model.Email.Trim());
            if (user is null)
                return FailLogin("Invalid email or password.");

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
    }
}
