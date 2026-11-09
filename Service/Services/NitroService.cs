using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Options;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Nitro;
using Service.Helpers.Exceptions;
using Service.Helpers.Settings;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class NitroService : INitroService
    {
        // Stripe can send the same news by two roads at once (the webhook and the page we return to): one at a time
        private static readonly SemaphoreSlim Gate = new(1, 1);

        private readonly INitroSubscriptionRepository _subscriptions;
        private readonly INitroEventRepository _events;
        private readonly INitroGiftRepository _gifts;
        private readonly IAccountRepository _accounts;
        private readonly IServerBoostRepository _boostRows;
        private readonly INitroAccessService _access;
        private readonly IBoostService _boosts;
        private readonly IStripeGateway _stripe;
        private readonly AppSettings _app;

        public NitroService(INitroSubscriptionRepository subscriptions,
                            INitroEventRepository events,
                            INitroGiftRepository gifts,
                            IAccountRepository accounts,
                            IServerBoostRepository boostRows,
                            INitroAccessService access,
                            IBoostService boosts,
                            IStripeGateway stripe,
                            IOptions<AppSettings> app)
        {
            _subscriptions = subscriptions;
            _events = events;
            _gifts = gifts;
            _accounts = accounts;
            _boostRows = boostRows;
            _access = access;
            _boosts = boosts;
            _stripe = stripe;
            _app = app.Value;
        }

        public async Task<NitroStatusDto> GetStatusAsync(string userId)
        {
            var found = await _subscriptions.GetByUserAsync(userId);
            var has = found is not null && found.IsActive(DateTime.UtcNow);
            var limits = await _access.GetLimitsAsync(userId);
            var used = await _boostRows.CountForUserAsync(userId);
            return new NitroStatusDto
            {
                HasNitro = has,
                Source = has ? SourceName(found!.Source) : string.Empty,
                ExpiresAt = has ? Utc(found!.CurrentPeriodEnd) : null,
                CancelAtPeriodEnd = has && found!.CancelAtPeriodEnd,
                CanManageBilling = found is not null && found.Source == NitroSource.Stripe && !string.IsNullOrEmpty(found.StripeCustomerId),
                IsBooster = used > 0,
                StaffRole = (await _accounts.GetStaffRolesAsync(new[] { userId })).TryGetValue(userId, out var staff) ? staff : string.Empty,
                BoostSlots = has ? NitroRules.BoostsPerNitro : 0,
                BoostsUsed = used,
                MaxMessageLength = limits.MaxMessageLength,
                MaxFileBytes = limits.MaxFileBytes,
                PriceCents = NitroRules.MonthlyPriceCents,
                Currency = NitroRules.Currency,
                GiftPriceCents = NitroRules.GiftPriceCents,
                GiftDays = NitroRules.GiftDays
            };
        }

        public async Task<CheckoutDto> CreateCheckoutAsync(string userId, CreateCheckoutDto model)
        {
            var successUrl = EnsureOwnUrl(model?.SuccessUrl, "success");
            var cancelUrl = EnsureOwnUrl(model?.CancelUrl, "cancel");

            var user = await _accounts.GetByIdAsync(userId) ?? throw new NotFoundException("User not found.");
            var existing = await _subscriptions.GetByUserAsync(userId);
            if (existing is not null && existing.IsActive(DateTime.UtcNow))
            {
                throw new BadRequestException(existing.CancelAtPeriodEnd
                    ? "Your Nitro is cancelled but still runs. Use Manage subscription to resume it."
                    : "You already have Nitro.");
            }

            if (!successUrl.Contains("{CHECKOUT_SESSION_ID}"))
            {
                successUrl += (successUrl.Contains('?') ? "&" : "?") + "session_id={CHECKOUT_SESSION_ID}";
            }

            var checkout = await _stripe.CreateCheckoutAsync(userId, user.Email ?? string.Empty, existing?.StripeCustomerId, successUrl, cancelUrl);
            if (string.IsNullOrEmpty(checkout.Url))
            {
                throw new BadRequestException("The payment page could not be opened. Try again.");
            }

            return new CheckoutDto { Url = checkout.Url };
        }

        public async Task<CheckoutDto> CreatePortalAsync(string userId, CreatePortalDto model)
        {
            var returnUrl = EnsureOwnUrl(model?.ReturnUrl, "return");
            var existing = await _subscriptions.GetByUserAsync(userId);
            if (existing is null || existing.Source != NitroSource.Stripe || string.IsNullOrEmpty(existing.StripeCustomerId))
            {
                throw new BadRequestException("There is no subscription to manage.");
            }

            return new CheckoutDto { Url = await _stripe.CreatePortalAsync(existing.StripeCustomerId, returnUrl) };
        }

        public async Task<(NitroStatusDto Status, NitroChangeDto Change)> ConfirmCheckoutAsync(string userId, ConfirmCheckoutDto model)
        {
            var sessionId = (model?.SessionId ?? string.Empty).Trim();
            if (sessionId.Length == 0 || sessionId.Length > 200)
            {
                throw new BadRequestException("The payment could not be found.");
            }

            var session = await _stripe.GetCheckoutAsync(sessionId) ?? throw new NotFoundException("The payment could not be found.");
            if (session.UserId != userId)
            {
                throw new ForbiddenException("That payment belongs to someone else.");
            }

            if (!session.Paid || string.IsNullOrEmpty(session.SubscriptionId))
            {
                throw new BadRequestException("The payment is not finished yet.");
            }

            var change = await ActivateAsync(userId, session.CustomerId, session.SubscriptionId, null);
            return (await GetStatusAsync(userId), change);
        }

        public async Task<NitroChangeDto> HandleWebhookAsync(string payload, string signature)
        {
            var info = _stripe.ReadWebhook(payload ?? string.Empty, signature ?? string.Empty)
                ?? throw new BadRequestException("The webhook could not be verified.");

            if (await _events.ExistsForStripeEventAsync(info.Id))
            {
                return new NitroChangeDto();
            }

            switch (info.Type)
            {
                case "checkout.session.completed":
                    if (info.Kind == "gift" && info.SessionId.Length > 0)
                    {
                        await CreateGiftFromSessionAsync(info.SessionId, null);
                        return new NitroChangeDto();
                    }

                    if (info.UserId.Length > 0 && info.SubscriptionId.Length > 0)
                    {
                        return await ActivateAsync(info.UserId, info.CustomerId, info.SubscriptionId, info.Id);
                    }

                    return new NitroChangeDto();
                case "invoice.paid":
                    return await OnInvoicePaidAsync(info);
                case "invoice.payment_failed":
                    return await OnPaymentFailedAsync(info);
                case "customer.subscription.updated":
                    return await OnSubscriptionUpdatedAsync(info);
                case "customer.subscription.deleted":
                    return await OnSubscriptionDeletedAsync(info);
                default:
                    return new NitroChangeDto();
            }
        }

        public async Task<NitroChangeDto> ExpireOverdueAsync()
        {
            var result = new NitroChangeDto();
            foreach (var row in await _subscriptions.GetOverdueAsync(DateTime.UtcNow))
            {
                await Gate.WaitAsync();
                try
                {
                    var fresh = await _subscriptions.GetByUserAsync(row.UserId);
                    if (fresh is null || fresh.Status == NitroStatus.Ended || fresh.CurrentPeriodEnd > DateTime.UtcNow)
                    {
                        continue;
                    }

                    // a renewal whose webhook did not arrive (yet): Stripe knows better than the clock
                    if (fresh.Source == NitroSource.Stripe && fresh.StripeSubscriptionId.Length > 0)
                    {
                        var live = await TryGetSubscriptionAsync(fresh.StripeSubscriptionId);
                        if (live is null)
                        {
                            continue;
                        }

                        if (live.PeriodEnd > DateTime.UtcNow && IsRunning(live.Status))
                        {
                            fresh.CurrentPeriodEnd = live.PeriodEnd;
                            fresh.CancelAtPeriodEnd = live.CancelAtPeriodEnd;
                            await _subscriptions.UpdateAsync(fresh);
                            continue;
                        }
                    }

                    result.Merge(await EndAsync(fresh, NitroEventKind.Expired, "The paid time ran out", null, null));
                }
                finally
                {
                    Gate.Release();
                }
            }

            return result;
        }

        public async Task<NitroChangeDto> EndForDeletedAccountAsync(string userId)
        {
            await Gate.WaitAsync();
            try
            {
                var row = await _subscriptions.GetByUserAsync(userId);
                if (row is not null && row.Source == NitroSource.Stripe && row.StripeSubscriptionId.Length > 0 && row.Status != NitroStatus.Ended)
                {
                    try
                    {
                        await _stripe.CancelSubscriptionAsync(row.StripeSubscriptionId);
                    }
                    catch (BadRequestException)
                    {
                        // Stripe could not be reached: the account is still closed, the billing has to be stopped in Stripe by hand
                    }
                }

                if (row is not null)
                {
                    return await EndAsync(row, NitroEventKind.Expired, "The account was deleted", null, null);
                }

                var change = new NitroChangeDto { UserIds = { userId } };
                foreach (var serverId in await _boosts.RemoveAllForUserAsync(userId))
                {
                    change.ServerIds.Add(serverId);
                }

                return change;
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<NitroChangeDto> GrantAsync(string actorId, GrantNitroDto model)
        {
            var name = (model?.UserName ?? string.Empty).Trim();
            var days = model?.Days ?? 0;
            if (name.Length == 0)
            {
                throw new BadRequestException("Enter a user name.");
            }

            if (days < 1 || days > 3650)
            {
                throw new BadRequestException("Choose between 1 and 3650 days.");
            }

            var user = await _accounts.GetByUserNameAsync(name) ?? throw new NotFoundException("That user does not exist.");
            await Gate.WaitAsync();
            try
            {
                var now = DateTime.UtcNow;
                var row = await _subscriptions.GetByUserAsync(user.Id);
                if (row is not null && row.Source == NitroSource.Stripe && row.IsActive(now))
                {
                    throw new BadRequestException("This person already pays for Nitro through Stripe.");
                }

                var start = row is not null && row.IsActive(now) ? row.CurrentPeriodEnd : now;
                if (row is null)
                {
                    row = new NitroSubscription { UserId = user.Id };
                    row.Source = NitroSource.Admin;
                    row.Status = NitroStatus.Active;
                    row.StartedAt = now;
                    row.CurrentPeriodEnd = start.AddDays(days);
                    await _subscriptions.AddAsync(row);
                }
                else
                {
                    if (!row.IsActive(now))
                    {
                        row.StartedAt = now;
                    }

                    row.Source = NitroSource.Admin;
                    row.Status = NitroStatus.Active;
                    row.StripeCustomerId = string.Empty;
                    row.StripeSubscriptionId = string.Empty;
                    row.CancelAtPeriodEnd = false;
                    row.EndedAt = null;
                    row.CurrentPeriodEnd = start.AddDays(days);
                    await _subscriptions.UpdateAsync(row);
                }

                await LogAsync(user.Id, NitroEventKind.GrantedByAdmin, NitroSource.Admin, "Granted " + days + (days == 1 ? " day" : " days") + " of Nitro", null, null, null, actorId);
                return new NitroChangeDto { UserIds = { user.Id } };
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<NitroChangeDto> AdjustBoostTimeAsync(string actorId, string userName, int hours)
        {
            var user = await _accounts.GetByUserNameAsync((userName ?? string.Empty).Trim()) ?? throw new NotFoundException("That user does not exist.");
            await _boosts.AdjustLockAsync(user.Id, hours);
            await LogAsync(user.Id, NitroEventKind.BoostTimeChanged, NitroSource.Admin, "The wait on the boosts was made " + (hours > 0 ? "longer" : "shorter") + " by " + Math.Abs(hours) + (Math.Abs(hours) == 1 ? " hour" : " hours"), null, null, null, actorId);
            var change = new NitroChangeDto { UserIds = { user.Id } };
            foreach (var boost in await _boostRows.GetForUserAsync(user.Id))
            {
                if (!change.ServerIds.Contains(boost.ServerId))
                {
                    change.ServerIds.Add(boost.ServerId);
                }
            }

            return change;
        }

        public async Task<NitroChangeDto> RevokeAsync(string actorId, string userName)
        {
            var user = await _accounts.GetByUserNameAsync((userName ?? string.Empty).Trim()) ?? throw new NotFoundException("That user does not exist.");
            await Gate.WaitAsync();
            try
            {
                var row = await _subscriptions.GetByUserAsync(user.Id);
                if (row is null || !row.IsActive(DateTime.UtcNow))
                {
                    throw new BadRequestException("This person does not have Nitro.");
                }

                // a subscription that is paid through Stripe is stopped there too, so the person is not charged again
                if (row.Source == NitroSource.Stripe && row.StripeSubscriptionId.Length > 0)
                {
                    await _stripe.CancelSubscriptionAsync(row.StripeSubscriptionId);
                }

                return await EndAsync(row, NitroEventKind.RevokedByAdmin, row.Source == NitroSource.Stripe ? "Nitro cancelled by an admin and the Stripe subscription stopped" : "Nitro taken away by an admin", null, actorId);
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<AdminNitroPageDto> GetAdminPageAsync(string? search, bool activeOnly, int page)
        {
            const int size = 20;
            page = Math.Max(1, page);
            var now = DateTime.UtcNow;
            var subs = await _subscriptions.GetPageAsync(search, activeOnly, now, page, size);
            var events = await _events.GetPageAsync(search, page, size);
            var actors = new Dictionary<string, string>();
            foreach (var actorId in events.Items.Where(e => e.ActorId is not null).Select(e => e.ActorId!).Distinct())
            {
                var actor = await _accounts.GetByIdAsync(actorId);
                actors[actorId] = actor is null ? string.Empty : string.IsNullOrWhiteSpace(actor.DisplayName) ? actor.UserName ?? string.Empty : actor.DisplayName;
            }

            var result = new AdminNitroPageDto
            {
                Page = page,
                PageSize = size,
                TotalSubscriptions = subs.Total,
                TotalEvents = events.Total,
                ActiveCount = (await _subscriptions.GetPageAsync(null, true, now, 1, 1)).Total
            };
            var revenue = await _events.GetRevenueAsync(new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc));
            result.RevenueCents = revenue.Total;
            result.RevenueThisMonthCents = revenue.Since;
            var paid = (await _events.GetPaymentTotalsAsync(subs.Items.Select(row => row.UserId))).ToDictionary(x => x.UserId);
            foreach (var row in subs.Items)
            {
                result.Subscriptions.Add(new AdminNitroSubscriptionDto
                {
                    UserId = row.UserId,
                    UserName = row.User.UserName ?? string.Empty,
                    DisplayName = Name(row.User),
                    AvatarUrl = row.User.AvatarUrl,
                    Source = SourceName(row.Source),
                    Status = row.Status.ToString(),
                    IsActive = row.IsActive(now),
                    StartedAt = Utc(row.StartedAt),
                    CurrentPeriodEnd = Utc(row.CurrentPeriodEnd),
                    CancelAtPeriodEnd = row.CancelAtPeriodEnd,
                    Boosts = await _boostRows.CountForUserAsync(row.UserId),
                    TotalPaidCents = paid.TryGetValue(row.UserId, out var total) ? total.Cents : 0,
                    Payments = paid.TryGetValue(row.UserId, out var made) ? made.Count : 0,
                    LastPaymentAt = paid.TryGetValue(row.UserId, out var last) && last.LastAt is not null ? Utc(last.LastAt.Value) : null
                });
            }

            foreach (var entry in events.Items)
            {
                result.Events.Add(new AdminNitroEventDto
                {
                    Id = entry.Id,
                    CreatedAt = Utc(entry.CreatedAt),
                    UserId = entry.UserId,
                    UserName = entry.User.UserName ?? string.Empty,
                    DisplayName = Name(entry.User),
                    Kind = entry.Kind.ToString(),
                    Source = SourceName(entry.Source),
                    Details = entry.Details,
                    AmountCents = entry.AmountCents,
                    Currency = entry.Currency,
                    ActorName = entry.ActorId is not null && actors.TryGetValue(entry.ActorId, out var actorName) ? actorName : string.Empty
                });
            }

            return result;
        }

        // ---------- gifts

        private string GiftLink(string code)
        {
            return _app.ClientUrl.TrimEnd('/') + "/gift/" + code;
        }

        private static string GiftStatus(NitroGift gift)
        {
            return gift.RevokedAt is not null ? "revoked" : gift.RedeemedAt is not null ? "redeemed" : "available";
        }

        private NitroGiftDto ToGiftDto(NitroGift gift)
        {
            return new NitroGiftDto
            {
                Id = gift.Id,
                Code = gift.Code,
                Link = GiftLink(gift.Code),
                Days = gift.Days,
                Status = GiftStatus(gift),
                CreatedAt = Utc(gift.CreatedAt),
                RedeemedByUserName = gift.RedeemedBy?.UserName ?? string.Empty,
                RedeemedAt = gift.RedeemedAt is null ? null : Utc(gift.RedeemedAt.Value)
            };
        }

        private static string NewGiftCode()
        {
            return Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
        }

        public async Task<CheckoutDto> CreateGiftCheckoutAsync(string userId, CreateCheckoutDto model)
        {
            var successUrl = EnsureOwnUrl(model?.SuccessUrl, "success");
            var cancelUrl = EnsureOwnUrl(model?.CancelUrl, "cancel");
            var user = await _accounts.GetByIdAsync(userId) ?? throw new NotFoundException("User not found.");

            if (!successUrl.Contains("{CHECKOUT_SESSION_ID}"))
            {
                successUrl += (successUrl.Contains('?') ? "&" : "?") + "session_id={CHECKOUT_SESSION_ID}";
            }

            var checkout = await _stripe.CreateGiftCheckoutAsync(userId, user.Email ?? string.Empty, successUrl, cancelUrl);
            if (string.IsNullOrEmpty(checkout.Url))
            {
                throw new BadRequestException("The payment page could not be opened. Try again.");
            }

            return new CheckoutDto { Url = checkout.Url };
        }

        // the page Stripe sends the buyer back to: the payment is checked, and the gift (with its link) is made once for that payment
        public async Task<NitroGiftDto> ConfirmGiftAsync(string userId, ConfirmCheckoutDto model)
        {
            var sessionId = (model?.SessionId ?? string.Empty).Trim();
            if (sessionId.Length == 0 || sessionId.Length > 200)
            {
                throw new BadRequestException("The payment could not be found.");
            }

            var gift = await CreateGiftFromSessionAsync(sessionId, userId);
            return ToGiftDto(gift);
        }

        // by the return page (buyer known) or by the webhook (buyer taken from the payment)
        private async Task<NitroGift> CreateGiftFromSessionAsync(string sessionId, string? expectedBuyerId)
        {
            var session = await _stripe.GetCheckoutAsync(sessionId) ?? throw new NotFoundException("The payment could not be found.");
            if (session.Kind != "gift")
            {
                throw new BadRequestException("That payment is not a gift.");
            }

            if (expectedBuyerId is not null && session.UserId != expectedBuyerId)
            {
                throw new ForbiddenException("That payment belongs to someone else.");
            }

            if (!session.Paid || session.UserId.Length == 0)
            {
                throw new BadRequestException("The payment is not finished yet.");
            }

            await Gate.WaitAsync();
            try
            {
                var existing = await _gifts.GetByStripeSessionAsync(sessionId);
                if (existing is not null)
                {
                    return existing;
                }

                var gift = new NitroGift
                {
                    Code = NewGiftCode(),
                    BuyerId = session.UserId,
                    Days = NitroRules.GiftDays,
                    AmountCents = session.AmountCents > 0 ? session.AmountCents : NitroRules.GiftPriceCents,
                    StripeSessionId = sessionId
                };
                await _gifts.AddAsync(gift);
                await LogAsync(session.UserId, NitroEventKind.GiftBought, NitroSource.Gift, "Bought a Nitro gift (" + gift.Days + " days)", gift.AmountCents, NitroRules.Currency, null, null);
                return (await _gifts.GetByCodeAsync(gift.Code))!;
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<List<NitroGiftDto>> GetMyGiftsAsync(string userId)
        {
            return (await _gifts.GetBoughtByAsync(userId)).Select(ToGiftDto).ToList();
        }

        public async Task<NitroGiftInfoDto> GetGiftAsync(string viewerId, string code)
        {
            var gift = await _gifts.GetByCodeAsync((code ?? string.Empty).Trim().ToLowerInvariant()) ?? throw new NotFoundException("This gift link is not valid.");
            var status = GiftStatus(gift);
            var reason = string.Empty;
            if (status == "revoked")
            {
                reason = "This gift was taken back.";
            }
            else if (status == "redeemed")
            {
                reason = gift.RedeemedById == viewerId ? "You already claimed this gift." : "Somebody already claimed this gift.";
            }
            else
            {
                var row = await _subscriptions.GetByUserAsync(viewerId);
                if (row is not null && row.Source == NitroSource.Stripe && row.IsActive(DateTime.UtcNow))
                {
                    reason = "You already pay for Nitro. Claim the gift after your subscription ends.";
                }
            }

            return new NitroGiftInfoDto
            {
                Code = gift.Code,
                FromUserName = gift.Buyer.UserName ?? string.Empty,
                FromDisplayName = Name(gift.Buyer),
                FromAvatarUrl = gift.Buyer.AvatarUrl ?? string.Empty,
                Days = gift.Days,
                Status = status,
                CanRedeem = reason.Length == 0,
                Reason = reason
            };
        }

        public async Task<(NitroStatusDto Status, NitroChangeDto Change)> RedeemGiftAsync(string userId, string code)
        {
            var change = new NitroChangeDto { UserIds = { userId } };
            await Gate.WaitAsync();
            try
            {
                var gift = await _gifts.GetByCodeAsync((code ?? string.Empty).Trim().ToLowerInvariant()) ?? throw new NotFoundException("This gift link is not valid.");
                if (gift.RevokedAt is not null)
                {
                    throw new BadRequestException("This gift was taken back.");
                }

                if (gift.RedeemedAt is not null)
                {
                    throw new BadRequestException(gift.RedeemedById == userId ? "You already claimed this gift." : "Somebody already claimed this gift.");
                }

                var now = DateTime.UtcNow;
                var row = await _subscriptions.GetByUserAsync(userId);
                if (row is not null && row.Source == NitroSource.Stripe && row.IsActive(now))
                {
                    throw new BadRequestException("You already pay for Nitro. Claim the gift after your subscription ends.");
                }

                // a gift is added to what the person still has from an earlier gift or from an admin
                var start = row is not null && row.IsActive(now) ? row.CurrentPeriodEnd : now;
                if (row is null)
                {
                    row = new NitroSubscription { UserId = userId, StartedAt = now };
                    row.Source = NitroSource.Gift;
                    row.Status = NitroStatus.Active;
                    row.CurrentPeriodEnd = start.AddDays(gift.Days);
                    await _subscriptions.AddAsync(row);
                }
                else
                {
                    if (!row.IsActive(now))
                    {
                        row.StartedAt = now;
                    }

                    row.Source = NitroSource.Gift;
                    row.Status = NitroStatus.Active;
                    row.StripeCustomerId = string.Empty;
                    row.StripeSubscriptionId = string.Empty;
                    row.CancelAtPeriodEnd = false;
                    row.EndedAt = null;
                    row.CurrentPeriodEnd = start.AddDays(gift.Days);
                    await _subscriptions.UpdateAsync(row);
                }

                gift.RedeemedById = userId;
                gift.RedeemedAt = now;
                await _gifts.UpdateAsync(gift);
                await LogAsync(userId, NitroEventKind.GiftRedeemed, NitroSource.Gift, "Claimed a Nitro gift from " + (gift.Buyer.UserName ?? "someone") + " (" + gift.Days + " days)", null, null, null, null);
                if (gift.BuyerId != userId)
                {
                    change.UserIds.Add(gift.BuyerId);
                }
            }
            finally
            {
                Gate.Release();
            }

            return (await GetStatusAsync(userId), change);
        }

        // a super admin makes a gift link nobody paid for
        public async Task<NitroGiftDto> CreateFreeGiftAsync(string actorId, CreateFreeGiftDto model)
        {
            var days = model?.Days ?? 0;
            if (days < 1 || days > 3650)
            {
                throw new BadRequestException("Choose between 1 and 3650 days.");
            }

            var gift = new NitroGift { Code = NewGiftCode(), BuyerId = actorId, Days = days, IsFree = true };
            await _gifts.AddAsync(gift);
            await LogAsync(actorId, NitroEventKind.GiftBought, NitroSource.Gift, "Made a free Nitro gift (" + days + (days == 1 ? " day)" : " days)"), null, null, null, actorId);
            return ToGiftDto((await _gifts.GetByCodeAsync(gift.Code))!);
        }

        public async Task RevokeGiftAsync(string actorId, int giftId)
        {
            await Gate.WaitAsync();
            try
            {
                var gift = await _gifts.GetByIdAsync(giftId) ?? throw new NotFoundException("That gift does not exist.");
                if (gift.RedeemedAt is not null)
                {
                    throw new BadRequestException("This gift was already claimed: take the Nitro away from the person instead.");
                }

                if (gift.RevokedAt is not null)
                {
                    throw new BadRequestException("This gift was already taken back.");
                }

                gift.RevokedAt = DateTime.UtcNow;
                await _gifts.UpdateAsync(gift);
                await LogAsync(gift.BuyerId, NitroEventKind.GiftRevoked, NitroSource.Gift, "A Nitro gift (" + gift.Code.Substring(0, 6) + "...) was taken back by an admin", null, null, null, actorId);
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<AdminNitroGiftPageDto> GetAdminGiftsAsync(string? search, int page)
        {
            const int size = 20;
            page = Math.Max(1, page);
            var (items, total) = await _gifts.GetPageAsync(search, page, size);
            return new AdminNitroGiftPageDto
            {
                Page = page,
                PageSize = size,
                Total = total,
                Items = items.Select(gift => new AdminNitroGiftDto
                {
                    Id = gift.Id,
                    Code = gift.Code,
                    Link = GiftLink(gift.Code),
                    BuyerUserName = gift.Buyer.UserName ?? string.Empty,
                    Days = gift.Days,
                    AmountCents = gift.AmountCents,
                    IsFree = gift.IsFree,
                    Status = GiftStatus(gift),
                    RedeemedByUserName = gift.RedeemedBy?.UserName ?? string.Empty,
                    CreatedAt = Utc(gift.CreatedAt),
                    RedeemedAt = gift.RedeemedAt is null ? null : Utc(gift.RedeemedAt.Value)
                }).ToList()
            };
        }

        // ---------- Stripe news

        private async Task<NitroChangeDto> ActivateAsync(string userId, string customerId, string subscriptionId, string? stripeEventId)
        {
            var live = await _stripe.GetSubscriptionAsync(subscriptionId) ?? throw new NotFoundException("The subscription could not be found.");
            if (!IsRunning(live.Status))
            {
                throw new BadRequestException("The subscription is not active.");
            }

            await Gate.WaitAsync();
            try
            {
                var now = DateTime.UtcNow;
                var row = await _subscriptions.GetByUserAsync(userId);
                var alreadyThis = row is not null && row.IsActive(now) && row.StripeSubscriptionId == subscriptionId;
                if (row is null)
                {
                    row = new NitroSubscription { UserId = userId };
                    Fill(row, live, customerId, now, true);
                    await _subscriptions.AddAsync(row);
                }
                else
                {
                    Fill(row, live, customerId, now, !alreadyThis);
                    await _subscriptions.UpdateAsync(row);
                }

                if (!alreadyThis)
                {
                    await LogAsync(userId, NitroEventKind.Subscribed, NitroSource.Stripe, "Subscribed to Nitro", NitroRules.MonthlyPriceCents, NitroRules.Currency, stripeEventId, null);
                }

                return new NitroChangeDto { UserIds = { userId } };
            }
            finally
            {
                Gate.Release();
            }
        }

        private static void Fill(NitroSubscription row, StripeSubscriptionInfo live, string customerId, DateTime now, bool fresh)
        {
            row.Source = NitroSource.Stripe;
            row.Status = live.Status == "past_due" ? NitroStatus.PastDue : NitroStatus.Active;
            row.StripeCustomerId = string.IsNullOrEmpty(customerId) ? live.CustomerId : customerId;
            row.StripeSubscriptionId = live.Id;
            row.CurrentPeriodEnd = live.PeriodEnd;
            row.CancelAtPeriodEnd = live.CancelAtPeriodEnd;
            row.EndedAt = null;
            if (fresh)
            {
                row.StartedAt = now;
            }
        }

        private async Task<NitroChangeDto> OnInvoicePaidAsync(StripeEventInfo info)
        {
            await Gate.WaitAsync();
            try
            {
                var row = await FindAsync(info);
                if (row is null)
                {
                    // the first invoice can come before the checkout news: that one logs the start
                    return new NitroChangeDto();
                }

                var live = await TryGetSubscriptionAsync(info.SubscriptionId);
                if (live is not null && IsRunning(live.Status))
                {
                    var wasEnded = row.Status == NitroStatus.Ended;
                    Fill(row, live, info.CustomerId, DateTime.UtcNow, wasEnded);
                    await _subscriptions.UpdateAsync(row);
                }

                if (info.BillingReason == "subscription_cycle")
                {
                    await LogAsync(row.UserId, NitroEventKind.Renewed, NitroSource.Stripe, "Renewed for another month", info.AmountCents, Currency(info.Currency), info.Id, null);
                }

                return new NitroChangeDto { UserIds = { row.UserId } };
            }
            finally
            {
                Gate.Release();
            }
        }

        private async Task<NitroChangeDto> OnPaymentFailedAsync(StripeEventInfo info)
        {
            await Gate.WaitAsync();
            try
            {
                var row = await FindAsync(info);
                if (row is null)
                {
                    return new NitroChangeDto();
                }

                if (row.Status == NitroStatus.Active)
                {
                    row.Status = NitroStatus.PastDue;
                    await _subscriptions.UpdateAsync(row);
                }

                await LogAsync(row.UserId, NitroEventKind.PaymentFailed, NitroSource.Stripe, "The payment failed", info.AmountCents, Currency(info.Currency), info.Id, null);
                return new NitroChangeDto { UserIds = { row.UserId } };
            }
            finally
            {
                Gate.Release();
            }
        }

        private async Task<NitroChangeDto> OnSubscriptionUpdatedAsync(StripeEventInfo info)
        {
            await Gate.WaitAsync();
            try
            {
                var row = await FindAsync(info);
                if (row is null)
                {
                    return new NitroChangeDto();
                }

                if (info.Status is "canceled" or "unpaid" or "incomplete_expired")
                {
                    return await EndAsync(row, NitroEventKind.Expired, "The subscription ended (" + info.Status + ")", info.Id, null);
                }

                // a late "updated" news must not bring back a subscription that already ended
                if (row.Status == NitroStatus.Ended)
                {
                    return new NitroChangeDto();
                }

                var wasCancelled = row.CancelAtPeriodEnd;
                if (info.PeriodEnd is not null)
                {
                    row.CurrentPeriodEnd = info.PeriodEnd.Value;
                }

                row.CancelAtPeriodEnd = info.CancelAtPeriodEnd;
                row.Status = info.Status == "past_due" ? NitroStatus.PastDue : NitroStatus.Active;
                await _subscriptions.UpdateAsync(row);

                if (info.CancelAtPeriodEnd && !wasCancelled)
                {
                    await LogAsync(row.UserId, NitroEventKind.Canceled, NitroSource.Stripe, "Cancelled: Nitro runs until " + row.CurrentPeriodEnd.ToString("yyyy-MM-dd"), null, null, info.Id, null);
                }
                else if (!info.CancelAtPeriodEnd && wasCancelled)
                {
                    await LogAsync(row.UserId, NitroEventKind.Resumed, NitroSource.Stripe, "Resumed the subscription", null, null, info.Id, null);
                }

                return new NitroChangeDto { UserIds = { row.UserId } };
            }
            finally
            {
                Gate.Release();
            }
        }

        private async Task<NitroChangeDto> OnSubscriptionDeletedAsync(StripeEventInfo info)
        {
            await Gate.WaitAsync();
            try
            {
                var row = await FindAsync(info);
                if (row is null || row.Status == NitroStatus.Ended)
                {
                    return new NitroChangeDto();
                }

                return await EndAsync(row, NitroEventKind.Expired, "The subscription ended", info.Id, null);
            }
            finally
            {
                Gate.Release();
            }
        }

        // ---------- helpers

        // takes Nitro away: marks the row, logs it, removes the boosts and the perks that were bound to it
        private async Task<NitroChangeDto> EndAsync(NitroSubscription row, NitroEventKind kind, string details, string? stripeEventId, string? actorId)
        {
            var change = new NitroChangeDto { UserIds = { row.UserId } };
            if (row.Status == NitroStatus.Ended && row.CurrentPeriodEnd <= DateTime.UtcNow)
            {
                return change;
            }

            row.Status = NitroStatus.Ended;
            row.EndedAt = DateTime.UtcNow;
            row.CancelAtPeriodEnd = false;
            if (row.CurrentPeriodEnd > DateTime.UtcNow)
            {
                row.CurrentPeriodEnd = DateTime.UtcNow;
            }

            await _subscriptions.UpdateAsync(row);
            await LogAsync(row.UserId, kind, row.Source, details, null, null, stripeEventId, actorId);

            foreach (var serverId in await _boosts.RemoveAllForUserAsync(row.UserId))
            {
                change.ServerIds.Add(serverId);
            }

            var user = await _accounts.GetByIdAsync(row.UserId);
            if (user is not null && (NitroRules.IsAnimatedImage(user.AvatarUrl) || user.BannerUrl.Length > 0))
            {
                if (NitroRules.IsAnimatedImage(user.AvatarUrl))
                {
                    user.AvatarUrl = string.Empty;
                }

                user.BannerUrl = string.Empty;
                user.UpdatedAt = DateTime.UtcNow;
                await _accounts.UpdateAsync(user);
            }

            return change;
        }

        private async Task<NitroSubscription?> FindAsync(StripeEventInfo info)
        {
            var row = await _subscriptions.GetByStripeSubscriptionAsync(info.SubscriptionId);
            if (row is null && info.UserId.Length > 0)
            {
                row = await _subscriptions.GetByUserAsync(info.UserId);
                if (row is not null && row.Source != NitroSource.Stripe)
                {
                    row = null;
                }
            }

            return row ?? await _subscriptions.GetByStripeCustomerAsync(info.CustomerId);
        }

        private async Task<StripeSubscriptionInfo?> TryGetSubscriptionAsync(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                return null;
            }

            try
            {
                return await _stripe.GetSubscriptionAsync(subscriptionId);
            }
            catch (BadRequestException)
            {
                return null;
            }
        }

        private async Task LogAsync(string userId, NitroEventKind kind, NitroSource source, string details, int? amountCents, string? currency, string? stripeEventId, string? actorId)
        {
            await _events.AddAsync(new NitroEvent
            {
                UserId = userId,
                Kind = kind,
                Source = source,
                Details = details.Length > 500 ? details.Substring(0, 500) : details,
                AmountCents = amountCents,
                Currency = currency ?? string.Empty,
                StripeEventId = stripeEventId,
                ActorId = actorId
            });
        }

        private static string SourceName(NitroSource source)
        {
            return source switch { NitroSource.Admin => "admin", NitroSource.Gift => "gift", _ => "stripe" };
        }

        private static bool IsRunning(string status)
        {
            return status is "active" or "trialing" or "past_due";
        }

        private static string Currency(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? NitroRules.Currency : value.Trim().ToLowerInvariant();
        }

        private static string Name(AppUser user)
        {
            return string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? string.Empty : user.DisplayName;
        }

        private static DateTime Utc(DateTime value)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        // the return pages must be on our own site: otherwise Stripe could be made to send people anywhere
        private string EnsureOwnUrl(string? value, string what)
        {
            var text = (value ?? string.Empty).Trim();
            if (!Uri.TryCreate(text, UriKind.Absolute, out var url) || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
                || !Uri.TryCreate(_app.ClientUrl, UriKind.Absolute, out var own) || !string.Equals(url.Host, own.Host, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("The " + what + " address is not valid.");
            }

            return text;
        }
    }
}
