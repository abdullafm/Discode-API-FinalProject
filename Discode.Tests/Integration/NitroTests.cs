using Microsoft.EntityFrameworkCore;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.DTOs.Nitro;
using Service.Helpers.DTOs.Servers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Discode.Tests.Integration
{
    public class NitroTests
    {
        private static async Task GiveNitroAsync(TestHost host, AppUser admin, AppUser user, int days = 30)
        {
            await host.Get<INitroService>().GrantAsync(admin.Id, new GrantNitroDto { UserName = user.UserName!, Days = days });
        }

        private static async Task<(AppUser Admin, AppUser Owner, int ServerId)> ServerWithOwnerAsync(TestHost host)
        {
            var admin = await host.AddUserAsync(Roles.SuperAdmin, "super@test.discode");
            var owner = await host.AddUserAsync(Roles.Member, "owner@test.discode");
            var created = await host.CreateServerAsync(owner);
            return (admin, owner, created.Server.Id);
        }

        // ---------- limits

        [Fact]
        public async Task A_free_account_is_limited_to_2000_characters_and_5_MB()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "free@test.discode");
            var access = host.Get<INitroAccessService>();

            await access.EnsureCanSendAsync(user.Id, new string('a', NitroRules.FreeMessageLength), new[] { NitroRules.FreeFileBytes });

            var longText = await Assert.ThrowsAsync<BadRequestException>(() =>
                access.EnsureCanSendAsync(user.Id, new string('a', NitroRules.FreeMessageLength + 1), Array.Empty<long>()));
            Assert.Contains("Nitro", longText.Message);

            var bigFile = await Assert.ThrowsAsync<BadRequestException>(() =>
                access.EnsureCanSendAsync(user.Id, "hi", new[] { NitroRules.FreeFileBytes + 1 }));
            Assert.Contains("5 MB", bigFile.Message);
        }

        [Fact]
        public async Task Nitro_allows_long_messages_and_files_up_to_2_GB()
        {
            using var host = new TestHost();
            var admin = await host.AddUserAsync(Roles.SuperAdmin, "super@test.discode");
            var user = await host.AddUserAsync(Roles.Member, "paid@test.discode");
            await GiveNitroAsync(host, admin, user);
            var access = host.Get<INitroAccessService>();

            await access.EnsureCanSendAsync(user.Id, new string('a', NitroRules.NitroMessageLength), new[] { NitroRules.NitroFileBytes });

            await Assert.ThrowsAsync<BadRequestException>(() =>
                access.EnsureCanSendAsync(user.Id, new string('a', NitroRules.NitroMessageLength + 1), Array.Empty<long>()));
            await Assert.ThrowsAsync<BadRequestException>(() =>
                access.EnsureCanSendAsync(user.Id, "hi", new[] { NitroRules.NitroFileBytes + 1 }));
        }

        [Fact]
        public async Task A_long_server_message_is_refused_for_free_and_accepted_with_Nitro()
        {
            using var host = new TestHost();
            var (admin, owner, serverId) = await ServerWithOwnerAsync(host);
            var channelId = (await host.Get<IServerChannelService>().GetAsync(owner.Id, serverId)).Categories.SelectMany(c => c.Channels).First(c => c.Kind == "text").Id;
            var messages = host.Get<IServerMessageService>();
            var text = new string('x', 3000);

            var error = await Assert.ThrowsAsync<BadRequestException>(() =>
                messages.SendAsync(owner.Id, serverId, channelId, new Service.Helpers.DTOs.ServerMessages.SendServerMessageDto { Text = text }));
            Assert.Contains("2000", error.Message);

            await GiveNitroAsync(host, admin, owner);
            var sent = await messages.SendAsync(owner.Id, serverId, channelId, new Service.Helpers.DTOs.ServerMessages.SendServerMessageDto { Text = text });
            Assert.Equal(3000, sent.Message.Text.Length);
        }

        // ---------- admin grant / revoke

        [Fact]
        public async Task An_admin_can_give_and_take_away_Nitro_and_it_is_logged()
        {
            using var host = new TestHost();
            var admin = await host.AddUserAsync(Roles.SuperAdmin, "super@test.discode");
            var user = await host.AddUserAsync(Roles.Member, "paid@test.discode");
            var nitro = host.Get<INitroService>();

            Assert.False((await nitro.GetStatusAsync(user.Id)).HasNitro);
            await nitro.GrantAsync(admin.Id, new GrantNitroDto { UserName = user.UserName!, Days = 10 });
            var status = await nitro.GetStatusAsync(user.Id);
            Assert.True(status.HasNitro);
            Assert.Equal("admin", status.Source);
            Assert.Equal(NitroRules.NitroMessageLength, status.MaxMessageLength);

            // a second grant adds to the time that is left
            await nitro.GrantAsync(admin.Id, new GrantNitroDto { UserName = user.UserName!, Days = 10 });
            Assert.True((await nitro.GetStatusAsync(user.Id)).ExpiresAt > DateTime.UtcNow.AddDays(19));

            await nitro.RevokeAsync(admin.Id, user.UserName!);
            Assert.False((await nitro.GetStatusAsync(user.Id)).HasNitro);
            await Assert.ThrowsAsync<BadRequestException>(() => nitro.RevokeAsync(admin.Id, user.UserName!));

            var page = await nitro.GetAdminPageAsync(null, false, 1);
            Assert.Equal(new[] { "RevokedByAdmin", "GrantedByAdmin", "GrantedByAdmin" }, page.Events.Select(e => e.Kind).ToArray());
            Assert.All(page.Events, e => Assert.False(string.IsNullOrEmpty(e.ActorName)));
        }

        [Fact]
        public async Task Granting_needs_a_real_user_and_a_sensible_number_of_days()
        {
            using var host = new TestHost();
            var admin = await host.AddUserAsync(Roles.SuperAdmin, "super@test.discode");
            var nitro = host.Get<INitroService>();

            await Assert.ThrowsAsync<NotFoundException>(() => nitro.GrantAsync(admin.Id, new GrantNitroDto { UserName = "nobody", Days = 5 }));
            await Assert.ThrowsAsync<BadRequestException>(() => nitro.GrantAsync(admin.Id, new GrantNitroDto { UserName = admin.UserName!, Days = 0 }));
            await Assert.ThrowsAsync<BadRequestException>(() => nitro.GrantAsync(admin.Id, new GrantNitroDto { UserName = admin.UserName!, Days = 4000 }));
        }

        [Fact]
        public async Task Nitro_ends_by_itself_when_the_time_is_over()
        {
            using var host = new TestHost();
            var admin = await host.AddUserAsync(Roles.SuperAdmin, "super@test.discode");
            var user = await host.AddUserAsync(Roles.Member, "paid@test.discode");
            await GiveNitroAsync(host, admin, user, 1);

            var row = (await host.Get<INitroSubscriptionRepository>().GetByUserAsync(user.Id))!;
            row.CurrentPeriodEnd = DateTime.UtcNow.AddMinutes(-1);
            await host.Get<INitroSubscriptionRepository>().UpdateAsync(row);
            Assert.False(await host.Get<INitroAccessService>().HasNitroAsync(user.Id));

            var change = await host.Get<INitroService>().ExpireOverdueAsync();
            Assert.Contains(user.Id, change.UserIds);
            var events = (await host.Get<INitroService>().GetAdminPageAsync(null, false, 1)).Events;
            Assert.Contains(events, e => e.Kind == "Expired");
            Assert.Empty((await host.Get<INitroService>().ExpireOverdueAsync()).UserIds);
        }

        [Fact]
        public async Task A_Stripe_renewal_whose_webhook_never_came_is_found_by_asking_Stripe()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "buyer@test.discode");
            Pays(host, user);
            await host.Get<INitroService>().ConfirmCheckoutAsync(user.Id, new ConfirmCheckoutDto { SessionId = "cs_1" });

            // the paid month is over in our database, but Stripe has renewed it
            var repo = host.Get<INitroSubscriptionRepository>();
            var row = (await repo.GetByUserAsync(user.Id))!;
            row.CurrentPeriodEnd = DateTime.UtcNow.AddMinutes(-5);
            await repo.UpdateAsync(row);
            host.Stripe.Subscriptions["sub_1"] = new StripeSubscriptionInfo("sub_1", "cus_" + user.Id, user.Id, "active", DateTime.UtcNow.AddDays(30), false);

            Assert.Empty((await host.Get<INitroService>().ExpireOverdueAsync()).UserIds);
            Assert.True(await host.Get<INitroAccessService>().HasNitroAsync(user.Id));
        }

        [Fact]
        public async Task A_Stripe_subscription_that_is_really_over_ends_when_the_time_runs_out()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "buyer@test.discode");
            Pays(host, user);
            await host.Get<INitroService>().ConfirmCheckoutAsync(user.Id, new ConfirmCheckoutDto { SessionId = "cs_1" });

            var repo = host.Get<INitroSubscriptionRepository>();
            var row = (await repo.GetByUserAsync(user.Id))!;
            row.CurrentPeriodEnd = DateTime.UtcNow.AddMinutes(-5);
            await repo.UpdateAsync(row);
            host.Stripe.Subscriptions["sub_1"] = new StripeSubscriptionInfo("sub_1", "cus_" + user.Id, user.Id, "canceled", DateTime.UtcNow.AddMinutes(-5), false);

            Assert.Contains(user.Id, (await host.Get<INitroService>().ExpireOverdueAsync()).UserIds);
            Assert.False(await host.Get<INitroAccessService>().HasNitroAsync(user.Id));
            Assert.Contains(host.Get<INitroEventRepository>().GetPageAsync(null, 1, 20).Result.Items, e => e.Kind == NitroEventKind.Expired);
        }

        // ---------- Stripe

        [Fact]
        public async Task Checkout_opens_the_payment_page_only_for_our_own_return_addresses()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "buyer@test.discode");
            var nitro = host.Get<INitroService>();

            var ok = await nitro.CreateCheckoutAsync(user.Id, new CreateCheckoutDto { SuccessUrl = "https://localhost:7208/Nitro/Index?paid=1", CancelUrl = "https://localhost:7208/Nitro/Index" });
            Assert.StartsWith("https://checkout.stripe.test", ok.Url);
            Assert.Contains("session_id={CHECKOUT_SESSION_ID}", host.Stripe.LastSuccessUrl);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                nitro.CreateCheckoutAsync(user.Id, new CreateCheckoutDto { SuccessUrl = "https://evil.example/steal", CancelUrl = "https://localhost:7208/Nitro/Index" }));
            await Assert.ThrowsAsync<BadRequestException>(() =>
                nitro.CreateCheckoutAsync(user.Id, new CreateCheckoutDto { SuccessUrl = "javascript:alert(1)", CancelUrl = "https://localhost:7208/Nitro/Index" }));
        }

        private static void Pays(TestHost host, AppUser user, string sub = "sub_1", string session = "cs_1", string status = "active", int days = 30)
        {
            host.Stripe.Checkouts[session] = new StripeCheckoutResult(session, true, user.Id, "cus_" + user.Id, sub);
            host.Stripe.Subscriptions[sub] = new StripeSubscriptionInfo(sub, "cus_" + user.Id, user.Id, status, DateTime.UtcNow.AddDays(days), false);
        }

        [Fact]
        public async Task A_paid_checkout_starts_Nitro_once_and_is_logged_with_the_price()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "buyer@test.discode");
            var nitro = host.Get<INitroService>();
            Pays(host, user);

            var first = await nitro.ConfirmCheckoutAsync(user.Id, new ConfirmCheckoutDto { SessionId = "cs_1" });
            Assert.True(first.Status.HasNitro);
            Assert.True(first.Status.CanManageBilling);
            Assert.Contains(user.Id, first.Change.UserIds);

            // opening the return page again changes nothing and does not log twice
            await nitro.ConfirmCheckoutAsync(user.Id, new ConfirmCheckoutDto { SessionId = "cs_1" });
            var events = (await nitro.GetAdminPageAsync(null, false, 1)).Events;
            var bought = Assert.Single(events);
            Assert.Equal("Subscribed", bought.Kind);
            Assert.Equal(299, bought.AmountCents);
            Assert.Equal("usd", bought.Currency);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                nitro.CreateCheckoutAsync(user.Id, new CreateCheckoutDto { SuccessUrl = "https://localhost:7208/a", CancelUrl = "https://localhost:7208/b" }));
        }

        [Fact]
        public async Task Somebody_elses_or_unpaid_checkout_does_not_give_Nitro()
        {
            using var host = new TestHost();
            var buyer = await host.AddUserAsync(Roles.Member, "buyer@test.discode");
            var thief = await host.AddUserAsync(Roles.Member, "thief@test.discode");
            var nitro = host.Get<INitroService>();
            Pays(host, buyer);

            await Assert.ThrowsAsync<ForbiddenException>(() => nitro.ConfirmCheckoutAsync(thief.Id, new ConfirmCheckoutDto { SessionId = "cs_1" }));
            await Assert.ThrowsAsync<NotFoundException>(() => nitro.ConfirmCheckoutAsync(buyer.Id, new ConfirmCheckoutDto { SessionId = "cs_nope" }));
            await Assert.ThrowsAsync<BadRequestException>(() => nitro.ConfirmCheckoutAsync(buyer.Id, new ConfirmCheckoutDto { SessionId = "" }));

            host.Stripe.Checkouts["cs_2"] = new StripeCheckoutResult("cs_2", false, buyer.Id, "cus_x", "sub_2");
            await Assert.ThrowsAsync<BadRequestException>(() => nitro.ConfirmCheckoutAsync(buyer.Id, new ConfirmCheckoutDto { SessionId = "cs_2" }));

            Assert.False((await nitro.GetStatusAsync(buyer.Id)).HasNitro);
            Assert.False((await nitro.GetStatusAsync(thief.Id)).HasNitro);
        }

        private static StripeEventInfo Event(string id, string type, AppUser user, string sub = "sub_1", string status = "active", bool cancel = false, int? amount = null, string reason = "")
        {
            return new StripeEventInfo(id, type, "cus_" + user.Id, sub, user.Id, status, DateTime.UtcNow.AddDays(30), cancel, amount, "usd", reason);
        }

        private static async Task<NitroChangeDto> Hook(TestHost host, StripeEventInfo info)
        {
            host.Stripe.Webhooks[info.Id] = info;
            return await host.Get<INitroService>().HandleWebhookAsync(info.Id, "good");
        }

        [Fact]
        public async Task A_webhook_without_a_valid_signature_is_refused()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "buyer@test.discode");
            host.Stripe.Webhooks["evt_1"] = Event("evt_1", "customer.subscription.deleted", user);

            await Assert.ThrowsAsync<BadRequestException>(() => host.Get<INitroService>().HandleWebhookAsync("evt_1", "forged"));
            await Assert.ThrowsAsync<BadRequestException>(() => host.Get<INitroService>().HandleWebhookAsync("evt_1", ""));
            await Assert.ThrowsAsync<BadRequestException>(() => host.Get<INitroService>().HandleWebhookAsync("unknown payload", "good"));
        }

        [Fact]
        public async Task The_webhook_alone_can_start_Nitro_without_the_return_page()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "buyer@test.discode");
            Pays(host, user);

            await Hook(host, Event("evt_start", "checkout.session.completed", user));
            Assert.True((await host.Get<INitroService>().GetStatusAsync(user.Id)).HasNitro);

            // the same delivery again, and the return page after it: still one start
            await Hook(host, Event("evt_start", "checkout.session.completed", user));
            await host.Get<INitroService>().ConfirmCheckoutAsync(user.Id, new ConfirmCheckoutDto { SessionId = "cs_1" });
            Assert.Single((await host.Get<INitroService>().GetAdminPageAsync(null, false, 1)).Events);
        }

        [Fact]
        public async Task Renewal_failed_payment_and_cancel_are_all_logged_and_Nitro_runs_until_the_paid_time_ends()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "buyer@test.discode");
            var nitro = host.Get<INitroService>();
            Pays(host, user);
            await nitro.ConfirmCheckoutAsync(user.Id, new ConfirmCheckoutDto { SessionId = "cs_1" });

            await Hook(host, Event("evt_renew", "invoice.paid", user, amount: 299, reason: "subscription_cycle"));
            await Hook(host, Event("evt_fail", "invoice.payment_failed", user, amount: 299));
            Assert.True((await nitro.GetStatusAsync(user.Id)).HasNitro);

            await Hook(host, Event("evt_cancel", "customer.subscription.updated", user, cancel: true));
            var status = await nitro.GetStatusAsync(user.Id);
            Assert.True(status.HasNitro);
            Assert.True(status.CancelAtPeriodEnd);

            await Hook(host, Event("evt_resume", "customer.subscription.updated", user, cancel: false));
            Assert.False((await nitro.GetStatusAsync(user.Id)).CancelAtPeriodEnd);

            var kinds = (await nitro.GetAdminPageAsync(null, false, 1)).Events.Select(e => e.Kind).OrderBy(k => k).ToArray();
            Assert.Equal(new[] { "Canceled", "PaymentFailed", "Renewed", "Resumed", "Subscribed" }, kinds);
        }

        [Fact]
        public async Task When_the_subscription_ends_Nitro_the_boosts_and_the_perks_go()
        {
            using var host = new TestHost();
            var (_, owner, serverId) = await ServerWithOwnerAsync(host);
            var nitro = host.Get<INitroService>();
            Pays(host, owner);
            await nitro.ConfirmCheckoutAsync(owner.Id, new ConfirmCheckoutDto { SessionId = "cs_1" });
            await host.Get<IBoostService>().BoostAsync(owner.Id, serverId);

            var accounts = host.Get<IAccountService>();
            var saved = await accounts.UpdateProfileAsync(owner.Id, new UpdateProfileDto { DisplayName = "Owner", AvatarUrl = "/uploads/avatars/a.gif", BannerUrl = "/uploads/banners/b.png", IsPublic = true });
            Assert.True(saved.IsSuccess);

            var change = await Hook(host, Event("evt_end", "customer.subscription.deleted", owner));
            Assert.Contains(serverId, change.ServerIds);
            Assert.False((await nitro.GetStatusAsync(owner.Id)).HasNitro);
            Assert.Equal(0, (await host.Get<IBoostService>().GetInfoAsync(owner.Id, serverId)).Boosts);

            var profile = await accounts.GetProfileAsync(owner.Id);
            Assert.Equal(string.Empty, profile.AvatarUrl);
            Assert.Equal(string.Empty, profile.BannerUrl);

            // a repeated "ended" news does nothing more
            Assert.Empty((await Hook(host, Event("evt_end2", "customer.subscription.deleted", owner))).UserIds);
        }

        [Fact]
        public async Task The_billing_portal_is_for_people_who_pay_through_Stripe()
        {
            using var host = new TestHost();
            var admin = await host.AddUserAsync(Roles.SuperAdmin, "super@test.discode");
            var user = await host.AddUserAsync(Roles.Member, "buyer@test.discode");
            var nitro = host.Get<INitroService>();

            await Assert.ThrowsAsync<BadRequestException>(() => nitro.CreatePortalAsync(user.Id, new CreatePortalDto { ReturnUrl = "https://localhost:7208/Nitro" }));

            Pays(host, user);
            await nitro.ConfirmCheckoutAsync(user.Id, new ConfirmCheckoutDto { SessionId = "cs_1" });
            var portal = await nitro.CreatePortalAsync(user.Id, new CreatePortalDto { ReturnUrl = "https://localhost:7208/Nitro" });
            Assert.StartsWith("https://billing.stripe.test", portal.Url);
            Assert.Equal("cus_" + user.Id, host.Stripe.LastPortalCustomer);

            // free days are not added on top of a paid subscription, but a super admin can cancel it: Stripe is told to stop the billing
            await Assert.ThrowsAsync<BadRequestException>(() => nitro.GrantAsync(admin.Id, new GrantNitroDto { UserName = user.UserName!, Days = 5 }));
            await nitro.RevokeAsync(admin.Id, user.UserName!);
            Assert.Contains("sub_1", host.Stripe.Cancelled);
            Assert.False((await nitro.GetStatusAsync(user.Id)).HasNitro);
        }

        [Fact]
        public async Task The_admin_list_tells_who_paid_how_much_how_often_and_until_when()
        {
            using var host = new TestHost();
            var admin = await host.AddUserAsync(Roles.SuperAdmin, "super@test.discode");
            var payer = await host.AddUserAsync(Roles.Member, "payer@test.discode");
            var gifted = await host.AddUserAsync(Roles.Member, "gifted@test.discode");
            var nitro = host.Get<INitroService>();

            Pays(host, payer);
            await nitro.ConfirmCheckoutAsync(payer.Id, new ConfirmCheckoutDto { SessionId = "cs_1" });
            await Hook(host, Event("evt_renew", "invoice.paid", payer, amount: 299, reason: "subscription_cycle"));
            PaysForGift(host, payer);
            var gift = await nitro.ConfirmGiftAsync(payer.Id, new ConfirmCheckoutDto { SessionId = "cs_gift_1" });
            await nitro.RedeemGiftAsync(gifted.Id, gift.Code);

            var page = await nitro.GetAdminPageAsync(null, false, 1);
            var row = page.Subscriptions.Single(x => x.UserName == payer.UserName);
            Assert.Equal(299 + 299 + NitroRules.GiftPriceCents, row.TotalPaidCents);
            Assert.Equal(3, row.Payments);
            Assert.NotNull(row.LastPaymentAt);
            Assert.True(row.CurrentPeriodEnd > DateTime.UtcNow);
            // somebody who only got a gift paid nothing
            var free = page.Subscriptions.Single(x => x.UserName == gifted.UserName);
            Assert.Equal(0, free.TotalPaidCents);
            Assert.Equal("gift", free.Source);
            Assert.Equal(299 + 299 + NitroRules.GiftPriceCents, page.RevenueCents);
            Assert.Equal(page.RevenueCents, page.RevenueThisMonthCents);
        }

        // ---------- status line that clears itself

        [Fact]
        public async Task A_status_with_a_clear_time_empties_itself_when_the_time_comes()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "status@test.discode");
            var accounts = host.Get<IAccountService>();

            var saved = await accounts.UpdateProfileAsync(user.Id, new UpdateProfileDto { DisplayName = "S", CustomStatus = "😡 busy", IsPublic = true, StatusClearMinutes = 30 });
            Assert.True(saved.IsSuccess);
            Assert.Equal("😡 busy", saved.Profile!.CustomStatus);
            Assert.True(saved.Profile.CustomStatusClearAt > DateTime.UtcNow.AddMinutes(29));
            Assert.Empty(await accounts.ClearExpiredStatusesAsync());

            // saving something else (no time sent) keeps the time
            var other = await accounts.UpdateProfileAsync(user.Id, new UpdateProfileDto { DisplayName = "S", CustomStatus = "😡 busy", IsPublic = true, Bio = "hi" });
            Assert.NotNull(other.Profile!.CustomStatusClearAt);

            // the time passes
            var repo = host.Get<Repository.Repositories.Interfaces.IAccountRepository>();
            var row = (await repo.GetByIdAsync(user.Id))!;
            row.CustomStatusClearAt = DateTime.UtcNow.AddMinutes(-1);
            await repo.UpdateAsync(row);
            Assert.Equal(new[] { user.Id }, await accounts.ClearExpiredStatusesAsync());
            var after = await accounts.GetProfileAsync(user.Id);
            Assert.Equal(string.Empty, after.CustomStatus);
            Assert.Null(after.CustomStatusClearAt);

            // 0 means "until I change it", and too long is refused
            var forever = await accounts.UpdateProfileAsync(user.Id, new UpdateProfileDto { DisplayName = "S", CustomStatus = "x", IsPublic = true, StatusClearMinutes = 0 });
            Assert.Null(forever.Profile!.CustomStatusClearAt);
            var tooLong = await accounts.UpdateProfileAsync(user.Id, new UpdateProfileDto { DisplayName = "S", CustomStatus = "x", IsPublic = true, StatusClearMinutes = 20000 });
            Assert.False(tooLong.IsSuccess);
        }

        [Fact]
        public async Task The_banner_colour_is_free_checked_and_kept_when_not_sent()
        {
            using var host = new TestHost();
            var user = await host.AddUserAsync(Roles.Member, "colour@test.discode");
            var accounts = host.Get<IAccountService>();

            var set = await accounts.UpdateProfileAsync(user.Id, new UpdateProfileDto { DisplayName = "C", IsPublic = true, BannerColor = "#5865F2" });
            Assert.True(set.IsSuccess);
            Assert.Equal("#5865f2", set.Profile!.BannerColor);

            var keep = await accounts.UpdateProfileAsync(user.Id, new UpdateProfileDto { DisplayName = "C", IsPublic = true });
            Assert.Equal("#5865f2", keep.Profile!.BannerColor);

            foreach (var bad in new[] { "red", "#12345", "#gggggg", "5865f2", "url(x)" })
            {
                Assert.False((await accounts.UpdateProfileAsync(user.Id, new UpdateProfileDto { DisplayName = "C", IsPublic = true, BannerColor = bad })).IsSuccess);
            }

            var cleared = await accounts.UpdateProfileAsync(user.Id, new UpdateProfileDto { DisplayName = "C", IsPublic = true, BannerColor = "" });
            Assert.Equal(string.Empty, cleared.Profile!.BannerColor);

            var seen = await accounts.GetUserProfileAsync(user.Id, user.UserName!);
            Assert.Equal(string.Empty, seen.BannerColor);
        }

        [Fact]
        public async Task A_boost_moves_to_another_server_after_a_day_and_not_before()
        {
            using var host = new TestHost();
            var (admin, owner, firstServer) = await ServerWithOwnerAsync(host);
            var second = (await host.CreateServerAsync(owner, "Second")).Server.Id;
            await GiveNitroAsync(host, admin, owner);
            var boosts = host.Get<IBoostService>();
            await boosts.BoostAsync(owner.Id, firstServer);

            var early = await Assert.ThrowsAsync<BadRequestException>(() => boosts.MoveAsync(owner.Id, new MoveBoostDto { FromServerId = firstServer, ToServerId = second }));
            Assert.Contains("can be moved after", early.Message);

            var mine = await boosts.GetMineAsync(owner.Id);
            Assert.True(mine.HasNitro);
            Assert.Equal(2, mine.Slots);
            Assert.Equal(1, mine.Used);
            Assert.Equal(1, mine.Free);
            var row = Assert.Single(mine.Boosts);
            Assert.False(row.CanMove);
            Assert.Equal(firstServer, row.ServerId);
            Assert.Contains(mine.Servers, s => s.ServerId == firstServer && s.MyBoosts == 1);

            // the day passes
            var repo = host.Get<IServerBoostRepository>();
            var placed = (await repo.GetForUserAsync(owner.Id)).Single();
            placed.CreatedAt = DateTime.UtcNow.AddHours(-25);
            await repo.UpdateAsync(placed);
            Assert.True((await boosts.GetMineAsync(owner.Id)).Boosts.Single().CanMove);

            var moved = await boosts.MoveAsync(owner.Id, new MoveBoostDto { FromServerId = firstServer, ToServerId = second });
            Assert.Equal(1, moved.Info.Boosts);
            Assert.Contains(firstServer, moved.Change.ServerIds);
            Assert.Contains(second, moved.Change.ServerIds);
            Assert.Equal(0, (await boosts.GetInfoAsync(owner.Id, firstServer)).Boosts);

            // it is a new boost on the new server: the day starts again, and the slot was never lost
            var again = await Assert.ThrowsAsync<BadRequestException>(() => boosts.MoveAsync(owner.Id, new MoveBoostDto { FromServerId = second, ToServerId = firstServer }));
            Assert.Contains("can be moved after", again.Message);
            Assert.Equal(1, (await boosts.GetMineAsync(owner.Id)).Free);
        }

        [Fact]
        public async Task A_move_needs_Nitro_membership_a_boost_there_and_a_different_server()
        {
            using var host = new TestHost();
            var (admin, owner, firstServer) = await ServerWithOwnerAsync(host);
            var stranger = await host.AddUserAsync(Roles.Member, "stranger@test.discode");
            var second = (await host.CreateServerAsync(owner, "Second")).Server.Id;
            var boosts = host.Get<IBoostService>();
            await GiveNitroAsync(host, admin, owner);
            await GiveNitroAsync(host, admin, stranger);

            await Assert.ThrowsAsync<BadRequestException>(() => boosts.MoveAsync(owner.Id, new MoveBoostDto { FromServerId = firstServer, ToServerId = firstServer }));
            await Assert.ThrowsAsync<BadRequestException>(() => boosts.MoveAsync(owner.Id, new MoveBoostDto { FromServerId = firstServer, ToServerId = second }));
            await Assert.ThrowsAsync<NotFoundException>(() => boosts.MoveAsync(stranger.Id, new MoveBoostDto { FromServerId = firstServer, ToServerId = second }));
        }

        [Fact]
        public async Task Losing_Nitro_also_forgets_the_boost_cooldowns()
        {
            using var host = new TestHost();
            var (admin, owner, serverId) = await ServerWithOwnerAsync(host);
            await GiveNitroAsync(host, admin, owner);
            var boosts = host.Get<IBoostService>();
            await boosts.BoostAsync(owner.Id, serverId);
            await boosts.UnboostAsync(owner.Id, serverId);
            Assert.Single((await boosts.GetMineAsync(owner.Id)).Cooldowns);

            await host.Get<INitroService>().RevokeAsync(admin.Id, owner.UserName!);
            await GiveNitroAsync(host, admin, owner);
            Assert.Empty((await boosts.GetMineAsync(owner.Id)).Cooldowns);
        }

        [Fact]
        public async Task A_deleted_account_loses_Nitro_and_its_boosts_and_the_Stripe_billing_stops()
        {
            using var host = new TestHost();
            var (_, owner, serverId) = await ServerWithOwnerAsync(host);
            var nitro = host.Get<INitroService>();
            Pays(host, owner);
            await nitro.ConfirmCheckoutAsync(owner.Id, new ConfirmCheckoutDto { SessionId = "cs_1" });
            await host.Get<IBoostService>().BoostAsync(owner.Id, serverId);

            var change = await nitro.EndForDeletedAccountAsync(owner.Id);

            Assert.Contains(serverId, change.ServerIds);
            Assert.Contains("sub_1", host.Stripe.Cancelled);
            Assert.False((await nitro.GetStatusAsync(owner.Id)).HasNitro);
            Assert.Equal(0, (await host.Get<IBoostService>().GetInfoAsync(owner.Id, serverId)).Boosts);
        }

        [Fact]
        public async Task A_boost_on_a_server_the_person_left_can_still_be_taken_off()
        {
            using var host = new TestHost();
            var (admin, owner, serverId) = await ServerWithOwnerAsync(host);
            var guest = await host.AddUserAsync(Roles.Member, "guest@test.discode");
            await host.Get<IServerService>().JoinAsync(guest.Id, new JoinServerDto { Invite = (await host.Get<IServerService>().GetAsync(owner.Id, serverId)).InviteCode });
            await GiveNitroAsync(host, admin, guest);
            var boosts = host.Get<IBoostService>();
            await boosts.BoostAsync(guest.Id, serverId);
            var db = host.Get<Repository.Data.AppDbContext>();
            db.ChangeTracker.Clear();
            db.ServerMembers.RemoveRange(await db.ServerMembers.Where(m => m.ServerId == serverId && m.UserId == guest.Id).ToListAsync());
            await db.SaveChangesAsync();

            await boosts.UnboostAsync(guest.Id, serverId);
            Assert.Empty((await boosts.GetMineAsync(guest.Id)).Boosts);
        }

        // ---------- gifts

        private static void PaysForGift(TestHost host, AppUser buyer, string session = "cs_gift_1")
        {
            host.Stripe.Checkouts[session] = new StripeCheckoutResult(session, true, buyer.Id, string.Empty, string.Empty, "gift", NitroRules.GiftPriceCents);
        }

        [Fact]
        public async Task A_paid_gift_makes_one_link_that_gives_the_other_person_a_month_of_Nitro()
        {
            using var host = new TestHost();
            var buyer = await host.AddUserAsync(Roles.Member, "buyer@test.discode");
            var friend = await host.AddUserAsync(Roles.Member, "friend@test.discode");
            var other = await host.AddUserAsync(Roles.Member, "other@test.discode");
            var nitro = host.Get<INitroService>();

            // the payment page is only for our own return addresses; an unpaid or somebody else's payment makes nothing
            await Assert.ThrowsAsync<BadRequestException>(() => nitro.CreateGiftCheckoutAsync(buyer.Id, new CreateCheckoutDto { SuccessUrl = "https://evil.test/x", CancelUrl = "http://localhost/y" }));
            var open = await nitro.CreateGiftCheckoutAsync(buyer.Id, new CreateCheckoutDto { SuccessUrl = "https://localhost:7208/Nitro", CancelUrl = "https://localhost:7208/Nitro" });
            Assert.Contains("gift", open.Url);
            host.Stripe.Checkouts["cs_unpaid"] = new StripeCheckoutResult("cs_unpaid", false, buyer.Id, string.Empty, string.Empty, "gift", 0);
            await Assert.ThrowsAsync<BadRequestException>(() => nitro.ConfirmGiftAsync(buyer.Id, new ConfirmCheckoutDto { SessionId = "cs_unpaid" }));
            PaysForGift(host, buyer);
            await Assert.ThrowsAsync<ForbiddenException>(() => nitro.ConfirmGiftAsync(friend.Id, new ConfirmCheckoutDto { SessionId = "cs_gift_1" }));
            Pays(host, buyer, "sub_x", "cs_sub");
            await Assert.ThrowsAsync<BadRequestException>(() => nitro.ConfirmGiftAsync(buyer.Id, new ConfirmCheckoutDto { SessionId = "cs_sub" }));

            // the return page makes the gift once, however often it is opened
            var gift = await nitro.ConfirmGiftAsync(buyer.Id, new ConfirmCheckoutDto { SessionId = "cs_gift_1" });
            var again = await nitro.ConfirmGiftAsync(buyer.Id, new ConfirmCheckoutDto { SessionId = "cs_gift_1" });
            Assert.Equal(gift.Code, again.Code);
            Assert.Equal(24, gift.Code.Length);
            Assert.EndsWith("/gift/" + gift.Code, gift.Link);
            Assert.Equal(NitroRules.GiftDays, gift.Days);
            Assert.Single(await nitro.GetMyGiftsAsync(buyer.Id));
            Assert.Empty(await nitro.GetMyGiftsAsync(friend.Id));

            // the one who opens the link sees who it is from
            var info = await nitro.GetGiftAsync(friend.Id, gift.Code);
            Assert.True(info.CanRedeem);
            Assert.Equal(buyer.UserName, info.FromUserName);
            await Assert.ThrowsAsync<NotFoundException>(() => nitro.GetGiftAsync(friend.Id, "not-a-gift"));

            // claiming gives Nitro for the days, once
            Assert.False((await nitro.GetStatusAsync(friend.Id)).HasNitro);
            var claimed = await nitro.RedeemGiftAsync(friend.Id, gift.Code);
            Assert.True(claimed.Status.HasNitro);
            Assert.Equal("gift", claimed.Status.Source);
            Assert.InRange((claimed.Status.ExpiresAt!.Value - DateTime.UtcNow).TotalDays, NitroRules.GiftDays - 0.1, NitroRules.GiftDays + 0.1);
            Assert.Contains(buyer.Id, claimed.Change.UserIds);
            var used = await Assert.ThrowsAsync<BadRequestException>(() => nitro.RedeemGiftAsync(other.Id, gift.Code));
            Assert.Contains("already claimed", used.Message);
            Assert.False((await nitro.GetGiftAsync(other.Id, gift.Code)).CanRedeem);
            Assert.Equal("redeemed", (await nitro.GetMyGiftsAsync(buyer.Id)).Single().Status);
            Assert.Equal(friend.UserName, (await nitro.GetMyGiftsAsync(buyer.Id)).Single().RedeemedByUserName);

            // a second gift on top adds time instead of replacing it
            PaysForGift(host, buyer, "cs_gift_2");
            var second = await nitro.ConfirmGiftAsync(buyer.Id, new ConfirmCheckoutDto { SessionId = "cs_gift_2" });
            Assert.NotEqual(gift.Code, second.Code);
            var stacked = await nitro.RedeemGiftAsync(friend.Id, second.Code);
            Assert.InRange((stacked.Status.ExpiresAt!.Value - DateTime.UtcNow).TotalDays, NitroRules.GiftDays * 2 - 0.1, NitroRules.GiftDays * 2 + 0.1);
        }

        [Fact]
        public async Task A_person_who_pays_through_Stripe_cannot_claim_a_gift_and_the_webhook_can_make_the_gift_too()
        {
            using var host = new TestHost();
            var buyer = await host.AddUserAsync(Roles.Member, "buyer@test.discode");
            var payer = await host.AddUserAsync(Roles.Member, "payer@test.discode");
            var nitro = host.Get<INitroService>();
            Pays(host, payer);
            await nitro.ConfirmCheckoutAsync(payer.Id, new ConfirmCheckoutDto { SessionId = "cs_1" });

            // the payment page never came back (closed tab): Stripe's own news makes the gift
            PaysForGift(host, buyer);
            host.Stripe.Webhooks["gift-hook"] = new StripeEventInfo("evt_gift", "checkout.session.completed", string.Empty, string.Empty, buyer.Id, string.Empty, null, false, null, string.Empty, string.Empty, "gift", "cs_gift_1");
            await nitro.HandleWebhookAsync("gift-hook", "good");
            var made = (await nitro.GetMyGiftsAsync(buyer.Id)).Single();
            await nitro.ConfirmGiftAsync(buyer.Id, new ConfirmCheckoutDto { SessionId = "cs_gift_1" });
            Assert.Single(await nitro.GetMyGiftsAsync(buyer.Id));

            var info = await nitro.GetGiftAsync(payer.Id, made.Code);
            Assert.False(info.CanRedeem);
            Assert.Contains("already pay", info.Reason);
            await Assert.ThrowsAsync<BadRequestException>(() => nitro.RedeemGiftAsync(payer.Id, made.Code));
        }

        [Fact]
        public async Task A_super_admin_makes_free_gifts_and_takes_back_the_ones_nobody_claimed()
        {
            using var host = new TestHost();
            var admin = await host.AddUserAsync(Roles.SuperAdmin, "super@test.discode");
            var friend = await host.AddUserAsync(Roles.Member, "friend@test.discode");
            var nitro = host.Get<INitroService>();

            await Assert.ThrowsAsync<BadRequestException>(() => nitro.CreateFreeGiftAsync(admin.Id, new CreateFreeGiftDto { Days = 0 }));
            var free = await nitro.CreateFreeGiftAsync(admin.Id, new CreateFreeGiftDto { Days = 7 });
            Assert.Equal(7, free.Days);
            // free gifts are not in "my gifts" (nobody bought them) but the admin list has them
            Assert.Empty(await nitro.GetMyGiftsAsync(admin.Id));
            var list = await nitro.GetAdminGiftsAsync(null, 1);
            Assert.Equal(free.Code, Assert.Single(list.Items).Code);
            Assert.True(list.Items[0].IsFree);

            var taken = await nitro.CreateFreeGiftAsync(admin.Id, new CreateFreeGiftDto { Days = 3 });
            await nitro.RevokeGiftAsync(admin.Id, taken.Id);
            var gone = await Assert.ThrowsAsync<BadRequestException>(() => nitro.RedeemGiftAsync(friend.Id, taken.Code));
            Assert.Contains("taken back", gone.Message);
            await Assert.ThrowsAsync<BadRequestException>(() => nitro.RevokeGiftAsync(admin.Id, taken.Id));

            var claimed = await nitro.RedeemGiftAsync(friend.Id, free.Code);
            Assert.True(claimed.Status.HasNitro);
            // a claimed gift can not be taken back, the Nitro of the person is what an admin takes away
            await Assert.ThrowsAsync<BadRequestException>(() => nitro.RevokeGiftAsync(admin.Id, free.Id));
            await nitro.RevokeAsync(admin.Id, friend.UserName!);
            Assert.False((await nitro.GetStatusAsync(friend.Id)).HasNitro);
            Assert.Contains((await nitro.GetAdminPageAsync(null, false, 1)).Events, e => e.Kind == "GiftRedeemed");
        }

        [Fact]
        public async Task A_deleted_or_banned_server_gives_the_boost_back_but_the_day_since_it_was_placed_still_counts()
        {
            using var host = new TestHost();
            var admin = await host.AddUserAsync(Roles.SuperAdmin, "super@test.discode");
            var booster = await host.AddUserAsync(Roles.Member, "booster@test.discode");
            var first = await host.CreateServerAsync(booster, "First");
            var second = await host.CreateServerAsync(booster, "Second");
            var third = await host.CreateServerAsync(booster, "Third");
            await GiveNitroAsync(host, admin, booster);
            var boosts = host.Get<IBoostService>();

            await boosts.BoostAsync(booster.Id, first.Server.Id);
            await boosts.BoostAsync(booster.Id, second.Server.Id);
            Assert.Equal(0, (await boosts.GetMineAsync(booster.Id)).Free);

            // the first server is deleted right after it was boosted: the boost is gone and its place waits out the day
            // (the in-memory test database cannot run the delete itself: the service calls exactly this just before it)
            await boosts.ReleaseServerAsync(first.Server.Id);
            var mine = await boosts.GetMineAsync(booster.Id);
            Assert.Single(mine.Boosts);
            Assert.Single(mine.Cooldowns);
            Assert.Equal(0, mine.Free);
            var waiting = await Assert.ThrowsAsync<BadRequestException>(() => boosts.BoostAsync(booster.Id, third.Server.Id));
            Assert.Contains("can be used again", waiting.Message);

            // a boost that is older than a day is free at once when its server is banned
            host.Get<Repository.Data.AppDbContext>().ChangeTracker.Clear();
            var rows = await host.Get<IServerBoostRepository>().GetForUserOnServerAsync(booster.Id, second.Server.Id);
            rows[0].CreatedAt = DateTime.UtcNow.AddHours(-30);
            await host.Get<IServerBoostRepository>().UpdateAsync(rows[0]);
            // (a ban calls this too; the in-memory test database cannot run the ban update itself)
            await boosts.ReleaseServerAsync(second.Server.Id);
            mine = await boosts.GetMineAsync(booster.Id);
            Assert.Empty(mine.Boosts);
            Assert.Single(mine.Cooldowns);
            Assert.Equal(1, mine.Free);
            Assert.Equal(1, (await boosts.BoostAsync(booster.Id, third.Server.Id)).Info.Boosts);
        }

        [Fact]
        public async Task A_super_admin_makes_the_wait_on_boosts_longer_or_shorter()
        {
            using var host = new TestHost();
            var admin = await host.AddUserAsync(Roles.SuperAdmin, "super@test.discode");
            var booster = await host.AddUserAsync(Roles.Member, "booster@test.discode");
            var created = await host.CreateServerAsync(booster, "Home");
            var other = await host.CreateServerAsync(booster, "Other");
            await GiveNitroAsync(host, admin, booster);
            var boosts = host.Get<IBoostService>();
            var nitro = host.Get<INitroService>();

            await Assert.ThrowsAsync<BadRequestException>(() => nitro.AdjustBoostTimeAsync(admin.Id, booster.UserName!, 5));
            await boosts.BoostAsync(booster.Id, created.Server.Id);
            Assert.False((await boosts.GetMineAsync(booster.Id)).Boosts.Single().CanMove);

            // 24 hours less: the boost can be moved at once
            host.Get<Repository.Data.AppDbContext>().ChangeTracker.Clear();
            await nitro.AdjustBoostTimeAsync(admin.Id, booster.UserName!, -24);
            Assert.True((await boosts.GetMineAsync(booster.Id)).Boosts.Single().CanMove);
            // 48 hours more: it has to wait again
            host.Get<Repository.Data.AppDbContext>().ChangeTracker.Clear();
            await nitro.AdjustBoostTimeAsync(admin.Id, booster.UserName!, 48);
            var after = (await boosts.GetMineAsync(booster.Id)).Boosts.Single();
            Assert.False(after.CanMove);
            Assert.InRange((after.MovableAt - DateTime.UtcNow).TotalHours, 46, 49);

            // a place that waits after a boost was taken off is changed the same way
            await boosts.UnboostAsync(booster.Id, created.Server.Id);
            host.Get<Repository.Data.AppDbContext>().ChangeTracker.Clear();
            await nitro.AdjustBoostTimeAsync(admin.Id, booster.UserName!, -24);
            var mine = await boosts.GetMineAsync(booster.Id);
            Assert.Empty(mine.Cooldowns);
            Assert.Equal(2, mine.Free);
            await Assert.ThrowsAsync<BadRequestException>(() => nitro.AdjustBoostTimeAsync(admin.Id, booster.UserName!, 0));
            Assert.Contains((await nitro.GetAdminPageAsync(null, false, 1)).Events, e => e.Kind == "BoostTimeChanged");
        }

        [Fact]
        public async Task Admins_and_super_admins_get_their_badge_by_themselves()
        {
            using var host = new TestHost();
            var super = await host.AddUserAsync(Roles.SuperAdmin, "super@test.discode");
            var admin = await host.AddUserAsync(Roles.Admin, "admin@test.discode");
            var member = await host.AddUserAsync(Roles.Member, "member@test.discode");
            var accounts = host.Get<IAccountService>();

            Assert.Equal("superadmin", (await accounts.GetProfileAsync(super.Id)).StaffRole);
            Assert.Equal("admin", (await accounts.GetProfileAsync(admin.Id)).StaffRole);
            Assert.Equal(string.Empty, (await accounts.GetProfileAsync(member.Id)).StaffRole);

            // others see it on the profile card too
            Assert.Equal("admin", (await accounts.GetUserProfileAsync(member.Id, admin.UserName!)).StaffRole);
            Assert.Equal(string.Empty, (await accounts.GetUserProfileAsync(admin.Id, member.UserName!)).StaffRole);

            // and the person sees their own badge in the Nitro state the pages load
            var nitro = host.Get<INitroService>();
            Assert.Equal("superadmin", (await nitro.GetStatusAsync(super.Id)).StaffRole);
            Assert.Equal(string.Empty, (await nitro.GetStatusAsync(member.Id)).StaffRole);

            // and in the member list of a server
            var created = await host.CreateServerAsync(member);
            var servers = host.Get<IServerService>();
            await servers.JoinAsync(admin.Id, new JoinServerDto { Invite = created.Server.InviteCode });
            var members = await servers.GetMembersAsync(member.Id, created.Server.Id);
            Assert.Equal("admin", members.Single(m => m.UserName == admin.UserName).StaffRole);
            Assert.Equal(string.Empty, members.Single(m => m.UserName == member.UserName).StaffRole);
        }

        // ---------- profile perks

        [Fact]
        public async Task A_GIF_avatar_and_a_banner_need_Nitro()
        {
            using var host = new TestHost();
            var admin = await host.AddUserAsync(Roles.SuperAdmin, "super@test.discode");
            var user = await host.AddUserAsync(Roles.Member, "member@test.discode");
            var accounts = host.Get<IAccountService>();

            var png = await accounts.UpdateProfileAsync(user.Id, new UpdateProfileDto { DisplayName = "M", AvatarUrl = "/uploads/avatars/a.png", IsPublic = true });
            Assert.True(png.IsSuccess);

            var gif = await accounts.UpdateProfileAsync(user.Id, new UpdateProfileDto { DisplayName = "M", AvatarUrl = "/uploads/avatars/a.GIF", IsPublic = true });
            Assert.False(gif.IsSuccess);
            Assert.Contains("Nitro", gif.Errors[0]);

            var banner = await accounts.UpdateProfileAsync(user.Id, new UpdateProfileDto { DisplayName = "M", AvatarUrl = "/uploads/avatars/a.png", BannerUrl = "/uploads/banners/b.png", IsPublic = true });
            Assert.False(banner.IsSuccess);

            await GiveNitroAsync(host, admin, user);
            var both = await accounts.UpdateProfileAsync(user.Id, new UpdateProfileDto { DisplayName = "M", AvatarUrl = "/uploads/avatars/a.gif", BannerUrl = "/uploads/banners/b.png", IsPublic = true });
            Assert.True(both.IsSuccess);
            Assert.True(both.Profile!.HasNitro);
            Assert.Equal("/uploads/banners/b.png", both.Profile.BannerUrl);

            // a profile saved without a banner field keeps the banner
            var keep = await accounts.UpdateProfileAsync(user.Id, new UpdateProfileDto { DisplayName = "M2", AvatarUrl = "/uploads/avatars/a.gif", IsPublic = true });
            Assert.Equal("/uploads/banners/b.png", keep.Profile!.BannerUrl);

            var seen = await accounts.GetUserProfileAsync(admin.Id, user.UserName!);
            Assert.True(seen.HasNitro);
            Assert.Equal("/uploads/banners/b.png", seen.BannerUrl);
        }

        // ---------- boosts

        [Fact]
        public async Task Boosting_needs_Nitro_membership_and_is_limited_to_two()
        {
            using var host = new TestHost();
            var (admin, owner, serverId) = await ServerWithOwnerAsync(host);
            var stranger = await host.AddUserAsync(Roles.Member, "stranger@test.discode");
            var boosts = host.Get<IBoostService>();

            var free = await Assert.ThrowsAsync<ForbiddenException>(() => boosts.BoostAsync(owner.Id, serverId));
            Assert.Contains("Nitro", free.Message);

            await GiveNitroAsync(host, admin, owner);
            await GiveNitroAsync(host, admin, stranger);
            await Assert.ThrowsAsync<NotFoundException>(() => boosts.BoostAsync(stranger.Id, serverId));

            var first = await boosts.BoostAsync(owner.Id, serverId);
            Assert.Equal(1, first.Info.Boosts);
            Assert.Equal(1, first.Info.MyBoostsLeft);
            var second = await boosts.BoostAsync(owner.Id, serverId);
            Assert.Equal(2, second.Info.Boosts);
            Assert.False(second.Info.CanBoost);
            Assert.Equal(2, Assert.Single(second.Info.Boosters).Count);

            var third = await Assert.ThrowsAsync<BadRequestException>(() => boosts.BoostAsync(owner.Id, serverId));
            Assert.Contains("both", third.Message);

            // a boost that is taken off is free again only after the cooldown
            var removed = await boosts.UnboostAsync(owner.Id, serverId);
            Assert.Equal(1, removed.Info.Boosts);
            Assert.False(removed.Info.CanBoost);
            Assert.True(removed.Info.CooldownUntil > DateTime.UtcNow.AddHours(23));
            var other = await host.CreateServerAsync(owner, "Other");
            var waiting = await Assert.ThrowsAsync<BadRequestException>(() => boosts.BoostAsync(owner.Id, other.Server.Id));
            Assert.Contains("can be used again", waiting.Message);

            // the cooldown is over: the place is free
            await host.Get<IBoostCooldownRepository>().DeleteForUserAsync(owner.Id);
            Assert.Equal(1, (await boosts.BoostAsync(owner.Id, other.Server.Id)).Info.Boosts);
            Assert.True(await boosts.IsBoosterAsync(owner.Id));
            await Assert.ThrowsAsync<BadRequestException>(() => boosts.UnboostAsync(owner.Id, serverId + 999));
        }

        [Fact]
        public async Task A_server_with_14_boosts_unlocks_the_banner_the_GIF_icon_and_a_custom_link_and_loses_them_below_14()
        {
            using var host = new TestHost();
            var (admin, owner, serverId) = await ServerWithOwnerAsync(host);
            var servers = host.Get<IServerService>();
            var boosts = host.Get<IBoostService>();

            // not yet: every perk is refused with the number that is needed
            var banner = await Assert.ThrowsAsync<ForbiddenException>(() => servers.UpdateBannerAsync(owner.Id, serverId, new UpdateServerBannerDto { BannerUrl = "/uploads/servers/b.png" }));
            Assert.Contains("14", banner.Message);
            await Assert.ThrowsAsync<ForbiddenException>(() => servers.UpdateInviteAsync(owner.Id, serverId, new UpdateServerInviteDto { InviteCode = "nexus-prime" }));
            await Assert.ThrowsAsync<ForbiddenException>(() => servers.UpdateProfileAsync(owner.Id, serverId, new UpdateServerProfileDto { Name = "Nexus", BannerColor = "ink", IconUrl = "/uploads/servers/i.gif" }));

            // 7 Nitro members with 2 boosts each
            var crowd = new List<AppUser>();
            for (var i = 0; i < 7; i++)
            {
                var member = await host.AddUserAsync(Roles.Member, "booster" + i + "@test.discode");
                await GiveNitroAsync(host, admin, member);
                await servers.JoinAsync(member.Id, new JoinServerDto { Invite = (await servers.GetAsync(owner.Id, serverId)).InviteCode });
                await boosts.BoostAsync(member.Id, serverId);
                await boosts.BoostAsync(member.Id, serverId);
                crowd.Add(member);
            }

            Assert.True(await boosts.PerksUnlockedAsync(serverId));
            await servers.UpdateBannerAsync(owner.Id, serverId, new UpdateServerBannerDto { BannerUrl = "/uploads/servers/b.png" });
            await servers.UpdateProfileAsync(owner.Id, serverId, new UpdateServerProfileDto { Name = "Nexus", BannerColor = "ink", IconUrl = "/uploads/servers/i.gif" });
            var linked = await servers.UpdateInviteAsync(owner.Id, serverId, new UpdateServerInviteDto { InviteCode = "nexus-prime" });
            Assert.Equal("nexus-prime", linked.Server.CustomLink);

            // both the random code and the custom link let people in
            var random = (await servers.GetAsync(owner.Id, serverId)).InviteCode;
            Assert.NotEqual("nexus-prime", random);
            Assert.Equal(serverId, (await servers.GetInviteAsync(owner.Id, "nexus-prime")).ServerId);
            Assert.Equal(serverId, (await servers.GetInviteAsync(owner.Id, random)).ServerId);

            // the member list shows who has Nitro and who boosts
            var members = (await servers.GetMembersAsync(owner.Id, serverId)).ToList();
            Assert.All(crowd, c => Assert.True(members.Single(m => m.Id == c.Id).IsBooster && members.Single(m => m.Id == c.Id).HasNitro));
            Assert.False(members.Single(m => m.Id == owner.Id).IsBooster);

            // the Boost badge belongs to the person: it shows in every server they are in, not only in the one they boosted
            var elsewhere = await host.CreateServerAsync(crowd[0], "Elsewhere");
            var other = (await servers.GetMembersAsync(crowd[0].Id, elsewhere.Server.Id)).Single(m => m.Id == crowd[0].Id);
            Assert.True(other.IsBooster && other.HasNitro);

            // one of them loses Nitro: 12 boosts are left and the perks are taken back
            await host.Get<INitroService>().RevokeAsync(admin.Id, crowd[0].UserName!);
            Assert.False(await boosts.PerksUnlockedAsync(serverId));
            var after = await servers.GetAsync(owner.Id, serverId);
            Assert.Equal(string.Empty, after.BannerUrl);
            Assert.Equal(string.Empty, after.IconUrl);
            Assert.Null(after.CustomLink);
            Assert.Equal(random, after.InviteCode);
            Assert.Equal(serverId, (await servers.GetInviteAsync(owner.Id, random)).ServerId);
        }

        [Fact]
        public async Task A_custom_link_is_unique_clean_and_not_reserved()
        {
            using var host = new TestHost();
            var (admin, owner, serverId) = await ServerWithOwnerAsync(host);
            var second = await host.CreateServerAsync(owner, "Second");
            var servers = host.Get<IServerService>();
            var boosts = host.Get<IServerBoostRepository>();
            foreach (var id in new[] { serverId, second.Server.Id })
            {
                for (var i = 0; i < NitroRules.BoostsForPerks; i++)
                {
                    await boosts.AddAsync(new ServerBoost { ServerId = id, UserId = owner.Id });
                }
            }

            await servers.UpdateInviteAsync(owner.Id, serverId, new UpdateServerInviteDto { InviteCode = "Nexus-Prime" });
            var taken = await Assert.ThrowsAsync<BadRequestException>(() => servers.UpdateInviteAsync(owner.Id, second.Server.Id, new UpdateServerInviteDto { InviteCode = "nexus-prime" }));
            Assert.Contains("taken", taken.Message);
            var firstCode = (await servers.GetAsync(owner.Id, serverId)).InviteCode;
            await Assert.ThrowsAsync<BadRequestException>(() => servers.UpdateInviteAsync(owner.Id, second.Server.Id, new UpdateServerInviteDto { InviteCode = firstCode }));

            foreach (var bad in new[] { "admin", "nitro", "ab", "has space", "fuck-you", "f_u_c_k", "s3x", "orospu", "p0rn-club", "x" })
            {
                await Assert.ThrowsAnyAsync<Exception>(() => servers.UpdateInviteAsync(owner.Id, second.Server.Id, new UpdateServerInviteDto { InviteCode = bad }));
            }

            foreach (var fine in new[] { "essex-club", "class-room", "assassin", "scunthorpe", "my-server-2" })
            {
                var ok = await servers.UpdateInviteAsync(owner.Id, second.Server.Id, new UpdateServerInviteDto { InviteCode = fine });
                Assert.Equal(fine, ok.Server.CustomLink);
            }

            var removed = await servers.UpdateInviteAsync(owner.Id, second.Server.Id, new UpdateServerInviteDto { InviteCode = string.Empty });
            Assert.Null(removed.Server.CustomLink);
        }
    }
}
