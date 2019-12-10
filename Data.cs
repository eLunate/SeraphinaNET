using System;
using System.Threading.Tasks;

namespace SeraphinaNET.Data {
    public interface DataContextFactory {
        // I hate factories but they seem to be the best way to encapsulate 
        // hidden arguments from an external context.
        DataContext GetContext();
    }

    public interface DataContext : IDisposable {
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
}