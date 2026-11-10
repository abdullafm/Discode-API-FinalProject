using System.Net;
using System.Net.Http.Json;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.DTOs.Translations;
using Service.Helpers.Responses;

namespace Discode.Tests.Integration
{
    public class HttpApiTests : IClassFixture<ApiFactory>
    {
        private readonly HttpClient _client;

        public HttpApiTests(ApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Branding_is_public()
        {
            var response = await _client.GetAsync("/api/branding");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<BrandingDto>();
            Assert.NotNull(body);
            Assert.Equal(string.Empty, body.LogoUrl);
        }

        [Fact]
        public async Task Translations_are_public()
        {
            var response = await _client.GetAsync("/api/Translation");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<TranslationSetDto>();
            Assert.NotNull(body);
            Assert.Equal("en", body.Language);
        }

        [Fact]
        public async Task Profile_requires_a_token()
        {
            var response = await _client.GetAsync("/api/Account/profile");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
            Assert.NotNull(body);
            Assert.False(body.IsSuccess);
            Assert.Contains("sign in", body.Errors[0]);
        }

        [Fact]
        public async Task Register_then_login_without_the_code_is_rejected()
        {
            var email = $"http{Guid.NewGuid():N}@test.discode";
            var register = await _client.PostAsJsonAsync("/api/Account/register", new RegisterDto
            {
                Email = email,
                DisplayName = "Http User",
                UserName = "http" + Guid.NewGuid().ToString("N")[..8],
                BirthDay = new DateTime(2000, 1, 1),
                Password = TestHost.Password
            });

            Assert.Equal(HttpStatusCode.OK, register.StatusCode);
            var created = await register.Content.ReadFromJsonAsync<RegisterResponse>();
            Assert.NotNull(created);
            Assert.True(created.IsSuccess);
            Assert.True(created.RequiresEmailConfirmation);

            var login = await _client.PostAsJsonAsync("/api/Account/login", new LoginDto
            {
                Email = email,
                Password = TestHost.Password
            });

            Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
            var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(body);
            Assert.False(body.IsSuccess);
        }

        [Fact]
        public async Task Emoji_categories_require_a_token()
        {
            var response = await _client.GetAsync("/api/Emoji");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private sealed class ErrorBody
        {
            public bool IsSuccess { get; set; }
            public string[] Errors { get; set; } = Array.Empty<string>();
        }
    }
}
