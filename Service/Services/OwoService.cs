using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Repository.Repositories.Interfaces;
using Service.Helpers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Service.Services
{
    public class OwoService : IOwoService
    {
        private static readonly Regex Command = new(@"^\s*owo(?:\s+(?<cmd>\S+))?(?:\s+(?<rest>.*))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new();
        private string? _botId;
        public static Random Dice = Random.Shared;

        private readonly IOwoRepository _owo;
        private readonly IServerRepository _servers;
        private readonly IServerMemberRepository _members;
        private readonly IAccountRepository _accounts;
        private readonly IServerAuditService _audit;

        public OwoService(IOwoRepository owo, IServerRepository servers, IServerMemberRepository members, IAccountRepository accounts, IServerAuditService audit)
        {
            _owo = owo;
            _servers = servers;
            _members = members;
            _accounts = accounts;
            _audit = audit;
        }

        public async Task<string?> GetBotIdAsync()
        {
            if (_botId is not null)
            {
                return _botId;
            }

            var bot = await _accounts.GetByUserNameAsync(OwoBot.UserName);
            _botId = bot?.Id;
            return _botId;
        }

        public async Task SetEnabledAsync(string userId, int serverId, bool enabled)
        {
            var server = await _servers.GetByIdAsync(serverId) ?? throw new NotFoundException("Server not found.");
            if (server.OwnerId != userId)
            {
                throw new ForbiddenException("Only the server owner can turn the OwO game on or off.");
            }

            if (server.OwoEnabled == enabled)
            {
                return;
            }

            server.OwoEnabled = enabled;
            await _servers.UpdateAsync(server);
            await _audit.LogAsync(serverId, userId, ServerAuditAction.ServerUpdate, "Server", serverId.ToString(), server.Name, enabled ? "Turned the OwO game on" : "Turned the OwO game off");
        }

        public async Task<string?> PlayAsync(int serverId, string userId, string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > 300 || !text.TrimStart().StartsWith("owo", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var match = Command.Match(text);
            if (!match.Success)
            {
                return null;
            }

            var server = await _servers.GetByIdAsync(serverId);
            if (server is null || !server.OwoEnabled)
            {
                return null;
            }

            var user = await _accounts.GetByIdAsync(userId);
            if (user is null || user.IsSystem)
            {
                return null;
            }

            var name = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? "player" : user.DisplayName;
            var command = match.Groups["cmd"].Success ? match.Groups["cmd"].Value.ToLowerInvariant() : string.Empty;
            var rest = match.Groups["rest"].Success ? match.Groups["rest"].Value.Trim() : string.Empty;

            var gate = Gates.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                var profile = await _owo.GetOrCreateProfileAsync(userId, OwoBot.StartingCash);
                var now = DateTime.UtcNow;
                switch (command)
                {
                    case "":
                        return await OwOAsync(profile, now);
                    case "help":
                        return Help(name);
                    case "cash":
                    case "money":
                    case "balance":
                    case "bal":
                    case "c":
                        return "💰 **| " + name + "**, you currently have **" + OwoRules.Money(profile.Cowoncy) + " cowoncy**!";
                    case "daily":
                        return await DailyAsync(profile, name, now);
                    case "hunt":
                    case "h":
                        return await HuntAsync(profile, userId, name, now);
                    case "zoo":
                    case "z":
                        return await ZooAsync(userId, name);
                    case "sell":
                        return await SellAsync(profile, userId, name, rest);
                    case "battle":
                    case "b":
                        return await BattleAsync(profile, userId, name, now);
                    case "cf":
                    case "coinflip":
                        return await CoinflipAsync(profile, name, rest, now);
                    case "slots":
                    case "s":
                        return await SlotsAsync(profile, name, rest, now);
                    case "give":
                    case "pay":
                    case "send":
                        return await GiveAsync(profile, serverId, userId, name, rest);
                    case "pray":
                        return await PrayAsync(profile, name, now);
                    case "top":
                    case "leaderboard":
                    case "lb":
                        return await TopAsync(serverId, name);
                    default:
                        return "❓ **| " + name + "**, I do not know `owo " + command + "`. Type `owo help` to see what I can do.";
                }
            }
            finally
            {
                gate.Release();
            }
        }

        private static string Help(string name)
        {
            return "📖 **| " + name + "**, here is what you can do:\n"
                + "`owo hunt` find animals · `owo zoo` your animals · `owo sell all` sell them for cowoncy\n"
                + "`owo battle` fight with your best animals · `owo pray` more luck when hunting\n"
                + "`owo cash` your cowoncy · `owo daily` free cowoncy every day · `owo give @user 100` send cowoncy\n"
                + "`owo cf 50` coin flip · `owo slots 50` slot machine · `owo top` richest in this server";
        }

        private async Task<string?> OwOAsync(OwoProfile profile, DateTime now)
        {
            if (profile.LastOwoAt is DateTime last && now - last < TimeSpan.FromSeconds(10))
            {
                return null;
            }

            profile.LastOwoAt = now;
            profile.Cowoncy += 1;
            await _owo.SaveAsync();
            return null;
        }

        private async Task<string> DailyAsync(OwoProfile profile, string name, DateTime now)
        {
            if (profile.LastDailyAt is DateTime last && now - last < TimeSpan.FromHours(24))
            {
                return "⏱ **| " + name + "**, Nu! You need to wait **" + OwoRules.Wait(TimeSpan.FromHours(24) - (now - last)) + "** until your next daily!";
            }

            profile.DailyStreak = profile.LastDailyAt is DateTime before && now - before < TimeSpan.FromHours(48) ? profile.DailyStreak + 1 : 1;
            var amount = 250 + Math.Min(profile.DailyStreak, 30) * 10;
            profile.Cowoncy += amount;
            profile.LastDailyAt = now;
            await _owo.SaveAsync();
            return "💰 **| " + name + "**, here is your daily **" + OwoRules.Money(amount) + " cowoncy**! You are on a **x" + profile.DailyStreak + "** daily streak!";
        }

        private async Task<string> HuntAsync(OwoProfile profile, string userId, string name, DateTime now)
        {
            if (profile.LastHuntAt is DateTime last && now - last < TimeSpan.FromSeconds(15))
            {
                return "⏱ **| " + name + "**, slow down! Try again in **" + OwoRules.Wait(TimeSpan.FromSeconds(15) - (now - last)) + "**.";
            }

            var cost = 5;
            if (profile.Cowoncy < cost)
            {
                return "❌ **| " + name + "**, you need **" + cost + " cowoncy** to hunt. Try `owo daily`.";
            }

            var found = new List<OwoAnimalInfo>();
            var count = 1 + (Dice.NextDouble() < 0.35 ? 1 : 0) + (Dice.NextDouble() < 0.1 ? 1 : 0);
            for (var i = 0; i < count; i++)
            {
                found.Add(OwoRules.Roll(Dice, profile.Luck));
            }

            profile.Cowoncy -= cost;
            profile.Hunts += 1;
            profile.LastHuntAt = now;
            await _owo.AddAnimalsAsync(userId, found.Select(a => a.Key));
            await _owo.SaveAsync();
            var best = found.Max(a => a.Rarity);
            return "🌱 **| " + name + "**, you spent **" + cost + "** cowoncy and found: " + string.Join(" ", found.Select(a => a.Emoji))
                + (best >= 3 ? "\n" + OwoRules.RarityMarks[best] + " **A " + OwoRules.RarityNames[best] + " animal!**" : string.Empty);
        }

        private async Task<string> ZooAsync(string userId, string name)
        {
            var owned = await _owo.GetAnimalsAsync(userId);
            if (owned.Count == 0)
            {
                return "🌿 **| " + name + "**'s zoo is empty. Try `owo hunt`!";
            }

            var lines = new List<string> { "🌿 **| " + name + "**'s zoo!" };
            for (var rarity = 0; rarity < OwoRules.RarityNames.Length; rarity++)
            {
                var row = owned
                    .Select(o => (Info: OwoRules.Find(o.AnimalKey), o.Count))
                    .Where(x => x.Info is not null && x.Info.Rarity == rarity)
                    .Select(x => x.Info!.Emoji + "x" + x.Count)
                    .ToList();
                if (row.Count > 0)
                {
                    lines.Add(OwoRules.RarityMarks[rarity] + " " + OwoRules.RarityNames[rarity] + ": " + string.Join("  ", row));
                }
            }

            lines.Add("Total: " + owned.Sum(o => o.Count) + " animals");
            return string.Join("\n", lines);
        }

        private async Task<string> SellAsync(OwoProfile profile, string userId, string name, string rest)
        {
            var target = rest.ToLowerInvariant();
            if (target.Length == 0)
            {
                return "❓ **| " + name + "**, tell me what to sell: `owo sell all` or `owo sell common` (uncommon, rare, epic, mythical, legendary).";
            }

            var owned = await _owo.GetAnimalsAsync(userId);
            var rarity = Array.IndexOf(OwoRules.RarityNames, target);
            if (target != "all" && rarity < 0)
            {
                return "❓ **| " + name + "**, I can sell `all` or one of: " + string.Join(", ", OwoRules.RarityNames) + ".";
            }

            var sold = 0;
            long total = 0;
            foreach (var row in owned)
            {
                var info = OwoRules.Find(row.AnimalKey);
                if (info is null || (target != "all" && info.Rarity != rarity))
                {
                    continue;
                }

                sold += row.Count;
                total += (long)row.Count * OwoRules.SellPrice[info.Rarity];
                row.Count = 0;
            }

            if (sold == 0)
            {
                return "❌ **| " + name + "**, you have nothing like that to sell.";
            }

            profile.Cowoncy += total;
            await _owo.SaveAsync();
            return "💰 **| " + name + "**, you sold **" + sold + "** animals for **" + OwoRules.Money(total) + " cowoncy**!";
        }

        private async Task<string> BattleAsync(OwoProfile profile, string userId, string name, DateTime now)
        {
            if (profile.LastBattleAt is DateTime last && now - last < TimeSpan.FromSeconds(20))
            {
                return "⏱ **| " + name + "**, your team is resting. Try again in **" + OwoRules.Wait(TimeSpan.FromSeconds(20) - (now - last)) + "**.";
            }

            var owned = await _owo.GetAnimalsAsync(userId);
            var expanded = new List<OwoAnimalInfo>();
            foreach (var row in owned)
            {
                var info = OwoRules.Find(row.AnimalKey);
                if (info is not null)
                {
                    expanded.AddRange(Enumerable.Repeat(info, row.Count));
                }
            }

            var best = expanded.OrderByDescending(a => a.Rarity).Take(3).ToList();
            if (best.Count == 0)
            {
                return "❌ **| " + name + "**, you need at least one animal to battle. Try `owo hunt`!";
            }

            var mine = best.Sum(a => OwoRules.Power[a.Rarity]);
            var enemyRarity = Math.Clamp(best.Max(a => a.Rarity) + Dice.Next(-1, 2), 0, 5);
            var enemy = Enumerable.Range(0, best.Count).Sum(_ => OwoRules.Power[Math.Clamp(enemyRarity + Dice.Next(-1, 1), 0, 5)]);
            var won = Dice.NextDouble() < (double)mine / (mine + enemy);
            profile.LastBattleAt = now;
            string result;
            if (won)
            {
                var reward = 20 + mine / 2 + Dice.Next(0, 30);
                profile.Cowoncy += reward;
                profile.BattlesWon += 1;
                result = "⚔ **| " + name + "**, your team " + string.Join("", best.Select(a => a.Emoji)) + " **won** the battle and earned **" + OwoRules.Money(reward) + " cowoncy**!";
            }
            else
            {
                profile.BattlesLost += 1;
                result = "⚔ **| " + name + "**, your team " + string.Join("", best.Select(a => a.Emoji)) + " **lost** the battle. Better luck next time!";
            }

            await _owo.SaveAsync();
            return result;
        }

        private static bool ReadBet(string rest, OwoProfile profile, out long bet, out string error)
        {
            error = string.Empty;
            bet = 0;
            var text = rest.Trim().ToLowerInvariant();
            if (text == "all")
            {
                bet = Math.Min(profile.Cowoncy, 250000);
            }
            else if (!long.TryParse(text, out bet))
            {
                error = "tell me how much to bet, like `50` or `all`.";
                return false;
            }

            if (bet < 1)
            {
                error = "the bet must be at least 1 cowoncy.";
                return false;
            }

            if (bet > 250000)
            {
                error = "the most you can bet is 250,000 cowoncy.";
                return false;
            }

            if (bet > profile.Cowoncy)
            {
                error = "you only have **" + OwoRules.Money(profile.Cowoncy) + " cowoncy**.";
                return false;
            }

            return true;
        }

        private async Task<string> CoinflipAsync(OwoProfile profile, string name, string rest, DateTime now)
        {
            if (!ReadBet(rest.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty, profile, out var bet, out var error))
            {
                return "❌ **| " + name + "**, " + error;
            }

            if (profile.LastGambleAt is DateTime last && now - last < TimeSpan.FromSeconds(5))
            {
                return "⏱ **| " + name + "**, slow down! Try again in **" + OwoRules.Wait(TimeSpan.FromSeconds(5) - (now - last)) + "**.";
            }

            profile.LastGambleAt = now;
            var won = Dice.NextDouble() < 0.5;
            profile.Cowoncy += won ? bet : -bet;
            await _owo.SaveAsync();
            return "🪙 **| " + name + "**, you bet **" + OwoRules.Money(bet) + "** and the coin spins... " + (won ? "**heads**! You won **" + OwoRules.Money(bet * 2) + "** cowoncy!" : "**tails**. You lost it all.");
        }

        private async Task<string> SlotsAsync(OwoProfile profile, string name, string rest, DateTime now)
        {
            if (!ReadBet(rest.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty, profile, out var bet, out var error))
            {
                return "❌ **| " + name + "**, " + error;
            }

            if (profile.LastGambleAt is DateTime last && now - last < TimeSpan.FromSeconds(5))
            {
                return "⏱ **| " + name + "**, slow down! Try again in **" + OwoRules.Wait(TimeSpan.FromSeconds(5) - (now - last)) + "**.";
            }

            string[] symbols = { "🍒", "🍋", "🍇", "🔔", "💎", "7️⃣" };
            var reels = Enumerable.Range(0, 3).Select(_ => symbols[Dice.Next(symbols.Length)]).ToArray();
            var multiplier = 0;
            if (reels[0] == reels[1] && reels[1] == reels[2])
            {
                multiplier = reels[0] == "7️⃣" ? 10 : reels[0] == "💎" ? 6 : 4;
            }
            else if (reels[0] == reels[1] || reels[1] == reels[2] || reels[0] == reels[2])
            {
                multiplier = 2;
            }

            profile.LastGambleAt = now;
            var change = bet * multiplier - bet;
            profile.Cowoncy += change;
            await _owo.SaveAsync();
            var board = "[ " + string.Join(" | ", reels) + " ]";
            return "🎰 **| " + name + "**, you bet **" + OwoRules.Money(bet) + "**\n" + board + "\n"
                + (multiplier == 0 ? "No match. You lost **" + OwoRules.Money(bet) + "** cowoncy." : "You won **" + OwoRules.Money(bet * multiplier) + "** cowoncy (x" + multiplier + ")!");
        }

        private async Task<string> GiveAsync(OwoProfile profile, int serverId, string userId, string name, string rest)
        {
            var parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return "❓ **| " + name + "**, use `owo give @user 100`.";
            }

            var handle = parts[0].TrimStart('@').Trim().ToLowerInvariant();
            var memberIds = await _members.GetUserIdsAsync(serverId);
            AppUser? target = null;
            foreach (var id in memberIds)
            {
                var candidate = await _accounts.GetByIdAsync(id);
                if (candidate is not null && string.Equals(candidate.UserName, handle, StringComparison.OrdinalIgnoreCase))
                {
                    target = candidate;
                    break;
                }
            }

            if (target is null || target.IsSystem)
            {
                return "❌ **| " + name + "**, I could not find **" + handle + "** in this server.";
            }

            if (target.Id == userId)
            {
                return "❌ **| " + name + "**, you cannot give cowoncy to yourself.";
            }

            if (!long.TryParse(parts[1], out var amount) || amount < 1)
            {
                return "❌ **| " + name + "**, the amount must be a number above 0.";
            }

            if (amount > profile.Cowoncy)
            {
                return "❌ **| " + name + "**, you only have **" + OwoRules.Money(profile.Cowoncy) + " cowoncy**.";
            }

            var other = await _owo.GetOrCreateProfileAsync(target.Id, OwoBot.StartingCash);
            profile.Cowoncy -= amount;
            other.Cowoncy += amount;
            await _owo.SaveAsync();
            return "💸 **| " + name + "** sent **" + OwoRules.Money(amount) + " cowoncy** to **" + (string.IsNullOrWhiteSpace(target.DisplayName) ? target.UserName : target.DisplayName) + "**!";
        }

        private async Task<string> PrayAsync(OwoProfile profile, string name, DateTime now)
        {
            if (profile.LastPrayAt is DateTime last && now - last < TimeSpan.FromMinutes(5))
            {
                return "⏱ **| " + name + "**, you can pray again in **" + OwoRules.Wait(TimeSpan.FromMinutes(5) - (now - last)) + "**.";
            }

            if (profile.Cowoncy < 10)
            {
                return "❌ **| " + name + "**, praying costs **10 cowoncy**.";
            }

            profile.Cowoncy -= 10;
            profile.Luck = Math.Min(100, profile.Luck + 1);
            profile.LastPrayAt = now;
            await _owo.SaveAsync();
            return "🙏 **| " + name + "** prays... and spends **10 cowoncy**. Your luck is now **" + profile.Luck + "**!";
        }

        private async Task<string> TopAsync(int serverId, string name)
        {
            var memberIds = await _members.GetUserIdsAsync(serverId);
            var top = await _owo.TopAsync(memberIds, 10);
            if (top.Count == 0)
            {
                return "🏆 **| " + name + "**, nobody has played here yet. Try `owo daily`!";
            }

            var lines = new List<string> { "🏆 **| Richest players in this server**" };
            for (var i = 0; i < top.Count; i++)
            {
                var who = string.IsNullOrWhiteSpace(top[i].User.DisplayName) ? top[i].User.UserName : top[i].User.DisplayName;
                lines.Add((i + 1) + ". **" + who + "** — " + OwoRules.Money(top[i].Cowoncy) + " cowoncy");
            }

            return string.Join("\n", lines);
        }
    }
}
