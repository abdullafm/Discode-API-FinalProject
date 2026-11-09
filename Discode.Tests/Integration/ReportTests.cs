using Domain.Constants;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.DTOs.Posts;
using Service.Helpers.DTOs.Reports;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class ReportTests
    {
        [Fact]
        public async Task A_member_can_report_someone_elses_post()
        {
            using var host = new TestHost();
            var author = await host.AddUserAsync(Roles.Member, "author@test.discode");
            var reporter = await host.AddUserAsync(Roles.Member, "reporter@test.discode");
            var admin = await host.AddUserAsync(Roles.Admin, "admin@test.discode");
            var post = await host.Get<IPostService>().CreateAsync(author.Id, new CreatePostDto { Text = "questionable" });

            await host.Get<IReportService>().CreateAsync(reporter.Id, new CreateReportDto
            {
                TargetType = "Post",
                TargetId = post.Id.ToString(),
                Reason = "Spam",
                Details = "looks like ads"
            });

            var list = await host.Get<IAdminReportService>().GetAsync("Open", null, 1, 20);
            var item = Assert.Single(list.Items);
            Assert.Equal("Spam", item.Reason);
            Assert.Equal(reporter.Id, item.ReporterId);

            await host.Get<IAdminReportService>().SetStatusAsync(admin.Id, item.Id, new SetReportStatusDto { Status = "Reviewed" });
            var reviewed = await host.Get<IAdminReportService>().GetAsync("Reviewed", null, 1, 20);
            Assert.Equal("Reviewed", Assert.Single(reviewed.Items).Status);
        }

        [Fact]
        public async Task You_cannot_report_your_own_post()
        {
            using var host = new TestHost();
            var author = await host.AddUserAsync(Roles.Member, "author@test.discode");
            var post = await host.Get<IPostService>().CreateAsync(author.Id, new CreatePostDto { Text = "mine" });

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IReportService>().CreateAsync(author.Id, new CreateReportDto
                {
                    TargetType = "Post",
                    TargetId = post.Id.ToString(),
                    Reason = "Spam"
                }));

            Assert.Contains("your own content", error.Message);
        }
    }
}
