using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;
using System.Data.Common;

namespace SeraphinaNET.Data {
    sealed public class MySQLData : DataContextFactory {
        DataContext DataContextFactory.GetContext() => throw new NotImplementedException();
    }

    sealed public class MySQLContext : DataContext {

        public void Dispose() => throw new NotImplementedException();
        Task DataContext.AddActivity(ulong guild, ulong channel, ulong member, uint textScore, uint altScore, DateTime at) => throw new NotImplementedException();
        Task DataContext.AddModerationAction(ulong guild, ulong member, ulong moderator, DateTime since, byte type, DateTime? until, string? reason) => throw new NotImplementedException();
        Task DataContext.AddPin(ulong channel, ulong message) => throw new NotImplementedException();
        Task DataContext.AddTopic(ulong guild, ulong channel, ulong role, string name, string? emote) => throw new NotImplementedException();
        Task DataContext.ClearPins(ulong channel) => throw new NotImplementedException();
        Task<ActionData?> DataContext.GetAction(ulong message) => throw new NotImplementedException();
        Task<ModerationActionData[]> DataContext.GetActiveModerationActions(ulong guild, ulong member) => throw new NotImplementedException();
        Task<ActivityData> DataContext.GetChannelActivityScore(ulong guild, ulong channel, DateTime since) => throw new NotImplementedException();
        Task<ModerationActionData[]> DataContext.GetExpiredModerationActions() => throw new NotImplementedException();
        Task<ActivityData> DataContext.GetMemberActivityScore(ulong guild, ulong member, DateTime since) => throw new NotImplementedException();
        Task<double> DataContext.GetMemberXP(ulong guild, ulong member) => throw new NotImplementedException();
        Task<ModerationActionData[]> DataContext.GetModerationActions(ulong guild, ulong member) => throw new NotImplementedException();
        Task<ulong[]> DataContext.GetPins(ulong channel) => throw new NotImplementedException();
        Task<TopicData?> DataContext.GetTopicByChannel(ulong guild, ulong channel) => throw new NotImplementedException();
        Task<TopicData?> DataContext.GetTopicByEmote(ulong guild, string emote) => throw new NotImplementedException();
        Task<TopicData?> DataContext.GetTopicByName(ulong guild, string name) => throw new NotImplementedException();
        Task<TopicData[]> DataContext.GetTopics(ulong guild) => throw new NotImplementedException();
        Task DataContext.GiveMemberXP(ulong guild, ulong member, double xp) => throw new NotImplementedException();
        Task<bool> DataContext.IsPinned(ulong channel, ulong message) => throw new NotImplementedException();
        Task DataContext.RemoveModerationActionCompletionTimer(ulong guild, ulong member, byte type) => throw new NotImplementedException();
        Task DataContext.RemovePin(ulong channel, ulong message) => throw new NotImplementedException();
        Task DataContext.RemoveTopic(ulong guild, ulong channel) => throw new NotImplementedException();
        Task DataContext.SetAction(ulong message, int action) => throw new NotImplementedException();
        Task DataContext.SetPins(ulong channel, ulong[] messages) => throw new NotImplementedException();
    }
}
