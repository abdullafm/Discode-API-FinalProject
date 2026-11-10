using Domain.Constants;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.DTOs.Discover;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.DTOs.Translations;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class DiscoverPinBrandingTests
    {
        [Fact]
        public async Task Public_servers_show_up_in_discover()
        {
            using var host = new TestHost();
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var created = await host.CreateServerAsync(owner, "Galaxy");
            var discover = host.Get<IDiscoverService>();

            var list = (await discover.GetAsync(new DiscoverFilterDto { Search = "Galaxy", Take = 20 })).ToList();
            Assert.Contains(list, row => row.Id == created.Server.Id && row.InviteCode == created.Server.InviteCode);

            var item = await discover.GetItemAsync(created.Server.Id);
            Assert.NotNull(item);
            Assert.Equal("Galaxy", item!.Name);

            var mine = (await discover.GetForUserAsync(owner.Id)).ToList();
            Assert.Contains(mine, row => row.Id == created.Server.Id);

            await host.Get<IServerService>().UpdateProfileAsync(owner.Id, created.Server.Id, new UpdateServerProfileDto
            {
                Name = "Galaxy",
                BannerColor = "ink",
                IsPrivate = true
            });
            Assert.Null(await discover.GetItemAsync(created.Server.Id));
        }

        [Fact]
        public async Task Conversation_pins_toggle()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "me@test.discode");
            var pins = host.Get<IConversationPinService>();

            Assert.Empty(await pins.GetAsync(user.Id));
            var on = await pins.ToggleAsync(user.Id, "dm:alice");
            Assert.Equal("dm:alice", Assert.Single(on));
            Assert.Equal("dm:alice", Assert.Single(await pins.GetAsync(user.Id)));
        }

        [Fact]
        public async Task An_empty_pin_key_is_rejected()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "me@test.discode");

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IConversationPinService>().ToggleAsync(user.Id, "  "));

            Assert.Contains("conversation", error.Message);
        }

        [Fact]
        public async Task Branding_can_be_set_to_an_uploaded_logo()
        {
            using var host = new TestHost();
            var admin = await host.AddUserAsync(Roles.Admin, "admin@test.discode");
            var branding = host.Get<IBrandingService>();

            var empty = await branding.GetAsync();
            Assert.Equal(string.Empty, empty.LogoUrl);

            var set = await branding.SetAsync(admin.Id, BrandingSlots.Logo, new SetBrandingDto { Url = "/uploads/branding/logo.png" });
            Assert.Equal("/uploads/branding/logo.png", set.LogoUrl);

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                branding.SetAsync(admin.Id, BrandingSlots.Favicon, new SetBrandingDto { Url = "https://cdn.example/icon.png" }));
            Assert.Contains("upload", error.Message);

            var missing = await Assert.ThrowsAsync<NotFoundException>(() =>
                branding.SetAsync(admin.Id, "banner", new SetBrandingDto { Url = "/uploads/branding/logo.png" }));
            Assert.Contains("Unknown", missing.Message);
        }

        [Fact]
        public async Task Translations_default_to_english_and_accept_azerbaijani()
        {
            using var host = new TestHost();
            var translations = host.Get<ITranslationService>();

            var english = await translations.GetAsync(null, null);
            Assert.Equal("en", english.Language);

            var azerbaijani = await translations.GetAsync(null, "az");
            Assert.Equal("az", azerbaijani.Language);
        }

        [Fact]
        public async Task An_unsupported_language_is_rejected()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "me@test.discode");

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<ITranslationService>().SetLanguageAsync(user.Id, new SetLanguageDto { Language = "de" }));

            Assert.Contains("not supported", error.Message);
        }

        [Fact]
        public async Task Discover_rejects_an_invalid_page_size()
        {
            using var host = new TestHost();

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                host.Get<IDiscoverService>().GetAsync(new DiscoverFilterDto { Take = 0 }));

            Assert.Contains("between 1 and 100", error.Message);
        }

        [Fact]
        public async Task Emoji_categories_are_readable()
        {
            using var host = new TestHost();
            var categories = (await host.Get<IEmojiCategoryService>().GetAllAsync()).ToList();
            Assert.NotNull(categories);
        }

        [Fact]
        public async Task Token_service_issues_a_jwt()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "me@test.discode");

            var token = host.Get<ITokenService>().CreateToken(user, new[] { Roles.Member });
            Assert.False(string.IsNullOrWhiteSpace(token.Token));
            Assert.True(token.ExpiresAt > DateTime.UtcNow);
        }
    }
}
