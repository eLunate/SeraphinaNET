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
        public Task<TopicData[]> GetTopics(ulong guild);
        public Task AddTopic(ulong guild, ulong channel, string name, string? emote);
        public Task RemoveTopic(ulong guild, ulong channel);
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
        public ulong TopicChannel { get; }
        public string? TopicEmote { get; }
    }
}