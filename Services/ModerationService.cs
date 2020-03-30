using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using SeraphinaNET.Data;
using System.Threading.Tasks;
using Discord;

namespace SeraphinaNET.Services {
    class ModerationService {
        private readonly DataContextFactory data;
        private readonly UserService userService;
        private readonly ActivityService activityService;

        private const double PURGE_THRESHOLD_TIME_GAIN_RATE = 1 / 8;
        private const double PURGE_THRESHOLD_GAIN_RATE = 1 / 12;
        private const double MUTE_BASE_ACTIVITY_PER_SECOND = 8;
        private const double MUTE_LEVEL_BIAS = 1 / 12;

        public ModerationService(DataContextFactory data, UserService userService, ActivityService activityService) {
            this.data = data;
            this.userService = userService;
            this.activityService = activityService;
        }

        private enum ModerationType:byte {
            Warn = 0,
            Mute = 1,
            Void = 2,
            Kick = 3,
            Ban = 4,
            // Prune isn't really a moderation action that counts.
        }

        [System.Serializable]
        public class MissingRoleException : Exception { // Thank you, I hate boilerplate.
            public MissingRoleException() { }
            public MissingRoleException(string message) : base(message) { }
            public MissingRoleException(string message, Exception inner) : base(message, inner) { }
            protected MissingRoleException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        public async Task Kick(IGuildUser member, ulong moderator, string? reason) {
            using var db = data.GetContext();
            await Task.WhenAll(
                db.AddModerationAction(
                    guild: member.GuildId,
                    member: member.Id,
                    moderator: moderator,
                    since: DateTime.UtcNow,
                    type: (byte)ModerationType.Kick,
                    reason: reason
                ),
                member.KickAsync()
            );
        }
        public Task Ban(IGuildUser member, ulong moderator, string? reason, DateTime? until) => Ban(member.Guild, member.Id, moderator, reason, until);
        public async Task Ban(IGuild guild, ulong member, ulong moderator, string? reason, DateTime? until) { // Timespans can be represented and calculated outside
            using var db = data.GetContext();
            await Task.WhenAll(
                db.AddModerationAction(
                    guild: guild.Id,
                    member: member,
                    moderator: moderator,
                    since: DateTime.UtcNow,
                    until: until,
                    type: (byte)ModerationType.Ban,
                    reason: reason
                ),
                guild.AddBanAsync(member)
            );
        }
        public async Task Pardon(IGuild guild, ulong member) {
            using var db = data.GetContext();
            await Task.WhenAll(
                db.RemoveModerationActionCompletionTimer(guild.Id, member, (byte)ModerationType.Ban),
                guild.RemoveBanAsync(member)
            );
        }
        public async Task Mute(IGuildUser member, ulong moderator, string? reason, DateTime? until) {
            var mute = member.Guild.Roles.Where(x => x.Name.StartsWith("mute", StringComparison.OrdinalIgnoreCase)).FirstOrDefault(); // Mute, muted
            if (mute == null) throw new MissingRoleException("No mute role exists.");
            using var db = data.GetContext();
            await Task.WhenAll(
                db.AddModerationAction(
                    guild: member.GuildId,
                    member: member.Id,
                    moderator: moderator,
                    since: DateTime.UtcNow,
                    until: until,
                    type: (byte)ModerationType.Mute,
                    reason: reason
                ),
                member.AddRoleAsync(mute)
            );
        }
        public async Task Unmute(IGuildUser member) {
            var mute = member.Guild.Roles.Where(x => x.Name.StartsWith("mute", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (mute == null) throw new MissingRoleException("No mute role exists.");
            using var db = data.GetContext();
            await Task.WhenAll(
                db.RemoveModerationActionCompletionTimer(member.GuildId, member.Id, (byte)ModerationType.Mute),
                member.RemoveRoleAsync(mute)
            );
        }
        public async Task Void(IGuildUser member, ulong moderator, string? reason) {
            var moderation = member.Guild.Roles.Where(x => x.Name.StartsWith("moderat", StringComparison.OrdinalIgnoreCase)).FirstOrDefault(); // Moderation, moderator. Yes, it's shit.
            var voided = member.Guild.Roles.Where(x => x.Name.StartsWith("void", StringComparison.OrdinalIgnoreCase)).FirstOrDefault(); // Void, voided
            if (moderation == null) throw new MissingRoleException("No moderation/moderator role exists.");
            if (voided == null) throw new MissingRoleException("No void role exists.");
            using var db = data.GetContext();
            var channel = await member.Guild.CreateTextChannelAsync("void-"+member.Id);
            await Task.WhenAll(
                channel.AddPermissionOverwriteAsync(member, new OverwritePermissions(68608, 0)),
                channel.AddPermissionOverwriteAsync(member.Guild.EveryoneRole, new OverwritePermissions(0, 68608)),
                channel.AddPermissionOverwriteAsync(moderation, new OverwritePermissions(68608, 0)),
                member.AddRoleAsync(voided),
                db.AddModerationAction(
                    guild: member.GuildId,
                    member: member.Id,
                    moderator: moderator,
                    since: DateTime.Now,
                    type: (byte)ModerationType.Void,
                    reason: reason
                )
            );
        }
        public async Task Release(IGuildUser member) {
            var voided = member.Guild.Roles.Where(x => x.Name.StartsWith("void", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (voided == null) throw new MissingRoleException("No void role exists.");
            var channel = (await member.Guild.GetChannelsAsync()).Where(x => x.Name == "void-" + member.Id).FirstOrDefault(); // Not scalable. Absolute dogshit.
            using var db = data.GetContext();
            await Task.WhenAll(
                channel.DeleteAsync(),
                member.RemoveRoleAsync(voided)
                // Nothing to clear from the database.
            );
        }
        public Task Prune(ITextChannel channel, IGuildUser member, ulong after) {
            // Will chew through up to 50 messages until `after`
            var messages = channel.GetMessagesAsync(50);
            return messages.ForEachAsync(x => x.ToAsyncEnumerable().ForEachAsync(x => { if (x.Author.Id == member.Id && x.Id < after) x.DeleteAsync(); }));
            // Actually I have no idea how ForEachAsync works when you use something async in it.
            // Do the tasks get captured and wrapped up? I don't know.
        }
        public async Task Purge(IGuild guild) {
            // First, get Discord to try and purge the users who haven't been seen for a while in Discord.
            await guild.PruneUsersAsync(); // Not online for 30 days, no role = clear
            var users = await guild.GetUsersAsync();
            foreach (var user in users) {
                if (user.IsBot) continue;
                // May want to extend UserService (and Data) or something else to get these stats in bulk,
                // Because this job will take a long time on large servers.
                var level = UserService.XPToLevel(await userService.GetMemberXP(user));
                // With each level, the activity requirement goes up but the threshold time also increases
                // In total the user needs less activity per day and has more time to do it in.
                // The user gets a free pass if they've been in the server for less than two threshold periods.
                var thresholdTime = TimeSpan.FromDays(5 + level * PURGE_THRESHOLD_TIME_GAIN_RATE);
                if (user.JoinedAt >= DateTime.UtcNow - thresholdTime*2) continue;
                var activity = await activityService.GetMemberActivity(user.GuildId, user.Id, DateTime.UtcNow - thresholdTime);
                // Potentially define the activity threshold in terms of scoring hours.
                // Not that activity tracking itself has any elastic scoring, but it's all based on the same targets.
                // Rename activity.TextScore as activity.ContentScore? It would make more sense.
                if (activity.TextScore < 200 * (5 + level * PURGE_THRESHOLD_GAIN_RATE)) {
                    // Ideally I wouldn't await here and I would just do all of the kicks completely async on cooldown in the background. 
                    await user.KickAsync();
                }
            }
        }

        public async Task<bool> MemberIsMuted(ulong guild, ulong member) {
            // Later this should check if they're mute-immune via perms.
            // Check if they're manually muted.
            using var db = data.GetContext();
            var actions = await db.GetActiveModerationActions(guild, member);
            return actions.Any(x => x.Type == (byte)ModerationType.Mute);
        }
        public Task<bool> MemberIsMuted(IGuildUser user) => MemberIsMuted(user.GuildId, user.Id);

        public async Task<bool> ActionIsMuted(ulong guild, ulong member, uint score) {
            if (await MemberIsMuted(guild, member)) return true;
            var userLevel = UserService.XPToLevel(await userService.GetMemberXP(guild, member));
            var userActivity = await activityService.GetMemberActivity(guild, member, DateTime.UtcNow - TimeSpan.FromSeconds(score / (MUTE_BASE_ACTIVITY_PER_SECOND * (1 + userLevel * MUTE_LEVEL_BIAS))));
            Console.WriteLine(DateTime.UtcNow - TimeSpan.FromSeconds(score / (MUTE_BASE_ACTIVITY_PER_SECOND * (1 + userLevel * MUTE_LEVEL_BIAS))));
            Console.WriteLine(score);
            Console.WriteLine(userActivity.TextScore);
            return userActivity.TextScore > score * 1.2; // This bias works differently to changing the activity capture threshold
        }
        public Task<bool> ActionIsMuted(IUserMessage message, uint score) {
            if (message.Source != MessageSource.User || !(message.Channel is IGuildChannel channel)) return Task.FromResult(false);
            return ActionIsMuted(channel.GuildId, message.Author.Id, score);
        }
    }
}
