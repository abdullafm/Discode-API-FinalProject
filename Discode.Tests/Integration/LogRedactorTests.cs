using Discode_API_FinalProject.Helpers;

namespace Discode.Tests.Integration
{
    public class LogRedactorTests
    {
        private class Sample
        {
            public string UserName { get; set; } = "gunel";
            public string Password { get; set; } = "Secret123!";
            public string VerificationCode { get; set; } = "1234";
            public int Days { get; set; } = 30;
        }

        [Fact]
        public void Secrets_are_hidden_and_the_rest_is_shown()
        {
            var text = LogRedactor.Describe(new Sample());
            Assert.Contains("UserName=\"gunel\"", text);
            Assert.Contains("Days=30", text);
            Assert.Contains("Password=***", text);
            Assert.Contains("VerificationCode=***", text);
            Assert.DoesNotContain("Secret123", text);
            Assert.DoesNotContain("1234", text);
        }

        [Fact]
        public void Long_text_and_lists_are_shortened()
        {
            Assert.EndsWith("...\"", LogRedactor.Describe(new string('a', 500)));
            Assert.Equal("list(3)", LogRedactor.Describe(new[] { 1, 2, 3 }));
            Assert.Equal("null", LogRedactor.Describe(null));
        }
    }
}
