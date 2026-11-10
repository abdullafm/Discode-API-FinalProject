using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Discode.Tests.Integration
{
    public class ApiExceptionHandlerTests : IClassFixture<ApiFactory>
    {
        private readonly HttpClient _client;

        public ApiExceptionHandlerTests(ApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task A_missing_route_answers_with_the_shared_error_shape()
        {
            var response = await _client.GetAsync("/api/does-not-exist");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
            Assert.NotNull(body);
            Assert.False(body.IsSuccess);
            Assert.Equal("Not found.", body.Errors[0]);
            Assert.False(string.IsNullOrWhiteSpace(body.TraceId));
        }

        [Fact]
        public async Task Broken_json_is_rejected_without_parser_noise()
        {
            var response = await _client.PostAsync("/api/Account/register", new StringContent("{", Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
            Assert.NotNull(body);
            Assert.False(body.IsSuccess);
            Assert.Contains(body.Errors, error => error.Contains("not valid"));
        }

        private sealed class ErrorBody
        {
            public bool IsSuccess { get; set; }
            public string[] Errors { get; set; } = Array.Empty<string>();
            public string? TraceId { get; set; }
        }
    }
}
