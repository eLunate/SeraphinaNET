using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using SeraphinaNET.Data;
using System.Threading.Tasks;
using Discord;

namespace SeraphinaNET {
    class ModerationService {
        private readonly DataContextFactory data;

        public ModerationService(DataContextFactory data) {
            this.data = data;
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

        public Task Kick(IGuildUser member, ulong moderator, string? reason) {
            using var db = data.GetContext();
            return Task.WhenAll(
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
        public Task Ban(IGuildUser member, ulong moderator, string? reason, DateTime? until) {
            return Ban(member.Guild, member.Id, moderator, reason, until);
        }
        public Task Ban(IGuild guild, ulong member, ulong moderator, string? reason, DateTime? until) { // Timespans can be represented and calculated outside
            using var db = data.GetContext();
            return Task.WhenAll(
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
        public Task Pardon(IGuild guild, ulong member) {
            using var db = data.GetContext();
            return Task.WhenAll(
                db.RemoveModerationActionCompletionTimer(guild.Id, member, (byte)ModerationType.Ban),
                guild.RemoveBanAsync(member)
            );
        }
        public Task Mute(IGuildUser member, ulong moderator, string? reason, DateTime? until) {
            var mute = member.Guild.Roles.Where(x => x.Name.StartsWith("mute", StringComparison.OrdinalIgnoreCase)).FirstOrDefault(); // Mute, muted
            if (mute == null) throw new MissingRoleException("No mute role exists.");
            using var db = data.GetContext();
            return Task.WhenAll(
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
        public Task Unmute(IGuildUser member) {
            var mute = member.Guild.Roles.Where(x => x.Name.StartsWith("mute", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (mute == null) throw new MissingRoleException("No mute role exists.");
            using var db = data.GetContext();
            return Task.WhenAll(
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
        // public Task Purge(IGuild guild) { }
        // There's no activity indicators yet, so purging can't happen
        // Also consider: Prune/purge have archaic reasons for their names, and don't match Discord's language.
        // Maybe switch the two of them.
    }
}
