using System;
using System.Threading.Tasks;

namespace SeraphinaNET.Data {
    public interface DataContextFactory {
        // I hate factories but they seem to be the best way to encapsulate 
        // hidden arguments from an external context.
        DataContext GetContext();
    }

    public interface DataContext : IDisposable { 
        // In the future perhaps extract these into their own interfaces and produce an aggregate interface for ease of signatures.
        #region Pins
        Task<ulong[]> GetPins(ulong channel);
        Task SetPins(ulong channel, ulong[] messages);
        Task ClearPins(ulong channel);
        Task AddPin(ulong channel, ulong message);
        Task RemovePin(ulong channel, ulong message);
        Task<bool> IsPinned(ulong channel, ulong message);
        #endregion

        #region Actions
        // I would use an enum but I'm not sure about the conversion
        // I'm also not sure if I should promise myself that messges will have unique IDs
        // They should, but Discord doesn't appear to have a primary index on them

        Task<ActionData?> GetAction(ulong message);
        Task SetAction(ulong message, int action);
        #endregion

        #region Topics
        // Topics as self-assignable roles just makes way more sense.
        // This may need a review because currently naming is per channel rather than role
        // But logically (from a human perspective) there needs to be a name<1..> -> role<1> -> channel<1..> relationship
        public Task<TopicData[]> GetTopics(ulong guild);
        public Task<TopicData?> GetTopicByName(ulong guild, string name);
        public Task<TopicData?> GetTopicByEmote(ulong guild, string emote);
        public Task<TopicData?> GetTopicByChannel(ulong guild, ulong channel);
        public Task AddTopic(ulong guild, ulong channel, ulong role, string name, string? emote);
        public Task RemoveTopic(ulong guild, ulong channel); // Roles are automatically unlisted when there's
                                                             // no more channels left with the role assigned for the topic.
        #endregion

        #region Moderation
        public Task AddModerationAction(ulong guild, ulong member, ulong moderator, DateTime since, byte type, DateTime? until = null, string? reason = null);
        public Task<ModerationActionData[]> GetModerationActions(ulong guild, ulong member);
        public Task<ModerationActionData[]> GetActiveModerationActions(ulong guild, ulong member);
        public Task<ModerationActionData[]> GetExpiredModerationActions();
        public Task RemoveModerationActionCompletionTimer(ulong guild, ulong member, byte type); // A little unorthodox, but whatever. Should only be one active per combo.
                                                                                                 // May later need the ability to add a filter (moderator, type, since)
                                                                                                 // And possibly the ability to remove moderation actions. We'll see.
        #endregion

        #region Activity
        public Task AddActivity(ulong guild, ulong channel, ulong member, uint textScore, uint altScore, DateTime at);
        // public Task AddActivity(ulong guild, ulong channel, ulong member, uint textScore, uint altScore) => AddActivity(guild, channel, member, textScore, altScore, DateTime.UtcNow);
        public Task<ActivityData> GetMemberActivityScore(ulong guild, ulong member, DateTime since);
        public Task<ActivityData> GetChannelActivityScore(ulong guild, ulong channel, DateTime since);
        #endregion

        #region User
        public Task<double> GetMemberXP(ulong guild, ulong member);
        public Task GiveMemberXP(ulong guild, ulong member, double xp); // Also negative XP.
        #endregion
    }

    public interface ActionData {
        public int ActionType { get; }
        public Task<string?> GetRadioData(ulong user);
        public Task<string[]> GetTallyData(ulong user);
        public Task SetRadioData(ulong user, string? data);
        public Task AddTally(ulong user, string emote);
        public Task RemoveTally(ulong user, string emote);
    }

    public interface TopicData {
        public string TopicName { get; }
        public ulong TopicRole { get; }
        public ulong TopicChannel { get; }
        public string? TopicEmote { get; }
        public ulong TopicGuild { get; }
    }

    public interface ModerationActionData {
        public ulong Moderator { get; }
        public ulong Member { get; }
        public ulong Guild { get; }
        public DateTime Since { get; }
        public DateTime? Until { get; }
        public string? Reason { get; }
        public byte Type { get; } // God forbid should I need more than one byte.
    }

    public interface ActivityData {
        public uint TextScore { get; }
        public uint AltScore { get; }
        // Really need a better content score breakdown
        // But that really depends on a concrete scoring impl
    }
}