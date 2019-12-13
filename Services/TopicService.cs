using System;
using System.Threading.Tasks;
using SeraphinaNET.Data;
using Discord;

namespace SeraphinaNET.Services {
    class TopicService {
        // At this point I'm beginning to ask myself if I should place my services in their own folder
        private readonly DataContextFactory data;

        public TopicService(DataContextFactory data) {
            this.data = data;
        }

        public Task<TopicData?> GetTopic(IGuildChannel channel) {
            using var db = data.GetContext();
            return db.GetTopicByChannel(channel.GuildId, channel.Id);
        }
        public Task<TopicData?> GetTopic(ulong guild, string name) {
            using var db = data.GetContext();
            return db.GetTopicByName(guild, name);
        }
        public Task<TopicData?> GetTopic(IGuild guild, string name) => GetTopic(guild.Id, name);
        public Task<TopicData?> GetTopic(ulong guild, IEmote emote) {
            using var db = data.GetContext();
            var emoteStr = emote.ToString();
            return emoteStr == null ? Task.FromResult<TopicData?>(null) : db.GetTopicByEmote(guild, emoteStr);
        }
        public Task<TopicData?> GetTopic(IGuild guild, IEmote emote) => GetTopic(guild.Id, emote);
        
        public Task AddUser(TopicData topic, IGuildUser user) {
            var role = user.Guild.GetRole(topic.TopicRole);
            return user.AddRoleAsync(role);
        }
        public Task RemoveUser(TopicData topic, IGuildUser user) {
            var role = user.Guild.GetRole(topic.TopicRole);
            return user.RemoveRoleAsync(role);
        }

        public Task AddTopic(ulong guild, ulong channel, ulong role, string name, string? emote) {
            using var db = data.GetContext();
            return db.AddTopic(guild, channel, role, name, emote);
        }
        public Task AddTopic(IGuildChannel channel, IRole role, string name, string? emote = null) => AddTopic(channel.GuildId, channel.Id, role.Id, name, emote);
        public Task RemoveTopic(TopicData topic) {
            using var db = data.GetContext();
            return db.RemoveTopic(topic.TopicGuild, topic.TopicChannel);
        }
    }
}
