using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SeraphinaNET.Data;
using Discord;

namespace SeraphinaNET.Services {
    class ActivityService {
        private readonly DataContextFactory data;

        public ActivityService(DataContextFactory data) {
            this.data = data;
        }

        public async Task AddMessageActivity(IUserMessage message, uint activityScore, uint altScore) {
            if (!(message.Channel is IGuildChannel channel)) return;
            using var db = data.GetContext();
            // I'll return to this later or something.
            await db.AddActivity(channel.GuildId, channel.Id, message.Author.Id, activityScore, altScore, DateTime.UtcNow);
        }

        // Why unpack things into a struct?
        // I don't want things to need references to the data layer, and the implementations may hold session references.
        public struct ActivityScore {
            public readonly uint TextScore;
            public readonly uint AltScore;

            public ActivityScore(uint textScore, uint altScore) {
                this.TextScore = textScore;
                this.AltScore = altScore;
            }
        }

        public async Task<ActivityScore> GetMemberActivity(ulong guild, ulong member, DateTime since) {
            using var db = data.GetContext();
            var activity = await db.GetMemberActivityScore(guild, member, since);
            return new ActivityScore(activity.TextScore, activity.AltScore);
        }
        public Task<ActivityScore> GetActivity(IGuildUser member, DateTime? since = null) 
            => GetMemberActivity(member.GuildId, member.Id, since ?? (DateTime.UtcNow - TimeSpan.FromHours(2)));

        public async Task<ActivityScore> GetChannelActivity(ulong guild, ulong channel, DateTime since) {
            using var db = data.GetContext();
            var activity = await db.GetChannelActivityScore(guild, channel, since);
            return new ActivityScore(activity.TextScore, activity.AltScore);
        }
        public Task<ActivityScore> GetActivity(IGuildChannel channel, DateTime? since = null) 
            => GetChannelActivity(channel.GuildId, channel.Id, since ?? (DateTime.UtcNow - TimeSpan.FromHours(2)));
    }
}
