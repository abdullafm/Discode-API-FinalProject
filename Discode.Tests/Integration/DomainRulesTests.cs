using Service.Helpers.DTOs.Accounts;
using Service.Helpers.DTOs.DirectMessages;
using Service.Helpers.DTOs.Live;
using Service.Helpers.DTOs.Posts;
using Service.Helpers.DTOs.Reports;
using Service.Helpers.DTOs.Stories;
using Service.Helpers.Validators.Accounts;
using Service.Helpers.Validators.DirectMessages;
using Service.Helpers.Validators.Live;
using Service.Helpers.Validators.Posts;
using Service.Helpers.Validators.Reports;
using Service.Helpers.Validators.Stories;

namespace Discode.Tests.Integration
{
    public class DomainRulesTests
    {
        [Fact]
        public void Register_requires_age_thirteen()
        {
            var dto = ValidRegister();
            dto.BirthDay = DateTime.UtcNow.Date.AddYears(-12);
            var result = new RegisterDtoValidator().Validate(dto);

            Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("13"));
        }

        [Fact]
        public void Register_requires_a_strong_password()
        {
            var dto = ValidRegister();
            dto.Password = "password";
            var result = new RegisterDtoValidator().Validate(dto);

            Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("uppercase"));
        }

        [Fact]
        public void A_message_cannot_be_empty()
        {
            var result = new SendMessageDtoValidator().Validate(new SendMessageDto { Text = "   " });

            Assert.Contains(result.Errors, error => error.ErrorMessage == "Write a message.");
        }

        [Fact]
        public void A_poll_needs_two_answers()
        {
            var result = new CreatePollDtoValidator().Validate(new CreatePollDto
            {
                Question = "One option?",
                DurationHours = 24,
                Options = { new CreatePollOptionDto { Text = "Only" } }
            });

            Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("at least two"));
        }

        [Fact]
        public void A_story_image_must_live_under_uploads()
        {
            var result = new CreateStoryDtoValidator().Validate(new CreateStoryDto { ImageUrl = "/uploads/posts/x.jpg" });
            Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("not valid"));
        }

        [Fact]
        public void A_report_needs_a_known_reason()
        {
            var result = new CreateReportDtoValidator().Validate(new CreateReportDto
            {
                TargetType = "Post",
                TargetId = "1",
                Reason = " annoya nce"
            });

            Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("reason"));
        }

        [Fact]
        public void A_live_title_is_required()
        {
            var result = new StartLiveDtoValidator().Validate(new StartLiveDto { Title = " ", Category = "Gaming" });
            Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("title"));
        }

        [Fact]
        public void A_comment_cannot_be_blank()
        {
            var result = new CreateCommentDtoValidator().Validate(new CreateCommentDto { Text = "   " });
            Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("comment"));
        }

        private static RegisterDto ValidRegister()
        {
            return new RegisterDto
            {
                Email = "rules@test.discode",
                DisplayName = "Rules",
                UserName = "rulesuser",
                BirthDay = new DateTime(2000, 1, 1),
                Password = TestHost.Password
            };
        }
    }
}
