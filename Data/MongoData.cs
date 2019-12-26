using System;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace SeraphinaNET.Data {
    public class MongoData : DataContextFactory {
        private readonly MongoClient client;
        private readonly string db;
        public MongoData(string connectionString, string dbName) {
            this.client = new MongoClient(connectionString);
            this.db = dbName;
        }

        public MongoDataContext GetContext() => new MongoDataContext(client.GetDatabase(db));
        DataContext DataContextFactory.GetContext() => GetContext();
    }

    // I have to seal this class or VS throws a fit about proper IDisposable impl
    sealed public class MongoDataContext : DataContext {
        private readonly IMongoDatabase db;
        internal MongoDataContext(IMongoDatabase db) {
            this.db = db;
        }

        // Stub Dispose method as Mongo thinks you should only have one Client.
        // So the factory holds this and calls it a day.
        public void Dispose() { }

        #region Data Mappings
        // The nullability of fields is really just for me. I assert it though usage rather than
        // through implementation or contract. It could be wrong.
        #nullable disable warnings
        [BsonIgnoreExtraElements]
        private class DBChannelInfo {
            // Mongo uses _id
            [BsonId]
            public ulong Id { get; set; }
            [BsonElement("pins")]
            public ulong[]? Pins { get; set; }
        }

        [BsonIgnoreExtraElements]
        private class DBActionInfo {
            [BsonId]
            public ulong Id { get; set; }
            [BsonElement("action")]
            public int ActionId { get; set; }
            [BsonElement("radio")]
            public Dictionary<string, string> Radio { get; set; }
            [BsonElement("tally")]
            public Dictionary<string, string[]> Tally { get; set; }
        }

        [BsonIgnoreExtraElements]
        private class DBTopicInfo : TopicData {
            [BsonElement("role")]
            public ulong TopicRole { get; set; }
            [BsonElement("guild")]
            public ulong TopicGuild { get; set; }
            [BsonElement("name")]
            public string TopicName { get; set; }
            [BsonId]
            public ulong TopicChannel { get; set; }
            [BsonElement("emote")]
            public string? TopicEmote { get; set; }
        }

        [BsonIgnoreExtraElements]
        [BsonNoId]
        private class DBModerationActionInfo : ModerationActionData {
            [BsonElement("moderator")]
            public ulong Moderator { get; set; }
            [BsonElement("member")]
            public ulong Member { get; set; }
            [BsonElement("guild")]
            public ulong Guild { get; set; }
            [BsonElement("since")]
            public DateTime Since { get; set; }
            [BsonElement("until")]
            public DateTime? Until { get; set; }
            [BsonElement("reason")]
            public string? Reason { get; set; }
            [BsonElement("type")]
            public byte Type { get; set; }
        }

        [BsonIgnoreExtraElements]
        [BsonNoId]
        private class DBActivityInfo : ActivityData {
            [BsonElement("text_score")]
            public uint TextScore { get; set; }
            [BsonElement("alt_score")]
            public uint AltScore { get; set; }
            [BsonElement("guild")]
            public ulong Guild { get; set; }
            [BsonElement("channel")]
            public ulong Channel { get; set; }
            [BsonElement("member")]
            public ulong Member { get; set; }
            [BsonElement("at")]
            public DateTime At { get; set; }
        }

        [BsonIgnoreExtraElements]
        private class DBUserInfo {
            [BsonId]
            public ulong Id { get; set; }
            [BsonElement("guild_xp")]
            public Dictionary<ulong, double> GuildXP { get; set; }
        }
#nullable restore warnings
        #endregion

        private class DBActionController : ActionData {
            private readonly IMongoDatabase db;
            private readonly DBActionInfo info;

            int ActionData.ActionType => info.ActionId;

            public DBActionController(IMongoDatabase db, DBActionInfo info) {
                this.db = db;
                this.info = info;
            }

            async Task ActionData.AddTally(ulong user, string emote) {
                var col = db.GetCollection<DBActionInfo>("actions");
                var filter = Builders<DBActionInfo>.Filter;
                var update = Builders<DBActionInfo>.Update;
                await col.UpdateOneAsync(filter.Eq("_id", info.Id), update.AddToSet($"tally.{user}", emote));
            }
            Task<string?> ActionData.GetRadioData(ulong user) => Task.FromResult(info.Radio.TryGetValue(user.ToString(), out var data) ? data : null);
            Task<string[]> ActionData.GetTallyData(ulong user) => Task.FromResult(info.Tally.TryGetValue(user.ToString(), out var data) ? data : Array.Empty<string>());
            async Task ActionData.RemoveTally(ulong user, string emote) {
                var col = db.GetCollection<DBActionInfo>("actions");
                var filter = Builders<DBActionInfo>.Filter;
                var update = Builders<DBActionInfo>.Update;
                await col.UpdateOneAsync(filter.Eq("_id", info.Id), update.Pull($"tally.{user}", emote));
            }
            async Task ActionData.SetRadioData(ulong user, string? data) {
                var col = db.GetCollection<DBActionInfo>("actions");
                var filter = Builders<DBActionInfo>.Filter;
                var update = Builders<DBActionInfo>.Update;
                await col.UpdateOneAsync(filter.Eq("_id", info.Id), update.Set($"radio.{user}", data));
            }
        }
        static MongoDataContext() { 
            BsonClassMap.RegisterClassMap<DBChannelInfo>();
            BsonClassMap.RegisterClassMap<DBActionInfo>();
            BsonClassMap.RegisterClassMap<DBTopicInfo>();
            BsonClassMap.RegisterClassMap<DBModerationActionInfo>();
            BsonClassMap.RegisterClassMap<DBActivityInfo>();
            BsonClassMap.RegisterClassMap<DBUserInfo>();
        }

        #region impl Pin
        public async Task AddPin(ulong channel, ulong message) {
            var col = db.GetCollection<DBChannelInfo>("channels");
            var filter = Builders<DBChannelInfo>.Filter;
            var update = Builders<DBChannelInfo>.Update;
            await col.UpdateOneAsync(filter.Eq("_id", channel), update.AddToSet("pins", message), new UpdateOptions { IsUpsert = true });
        }
        public Task ClearPins(ulong channel) => SetPins(channel, Array.Empty<ulong>());
        public async Task<ulong[]> GetPins(ulong channel) {
            var col = db.GetCollection<DBChannelInfo>("channels");
            var filter = Builders<DBChannelInfo>.Filter;
            var info = await col.Find(filter.Eq("_id", channel)).FirstOrDefaultAsync();
            return info?.Pins ?? Array.Empty<ulong>();
        }
        public async Task SetPins(ulong channel, ulong[] messages) {
            var col = db.GetCollection<DBChannelInfo>("channels");
            var filter = Builders<DBChannelInfo>.Filter;
            var update = Builders<DBChannelInfo>.Update;
            await col.UpdateOneAsync(filter.Eq("_id", channel), update.Set("pins", messages));
        }
        public async Task<bool> IsPinned(ulong channel, ulong message) {
            var col = db.GetCollection<DBChannelInfo>("channels");
            var filter = Builders<DBChannelInfo>.Filter;
            return 0 != await col.CountDocumentsAsync(filter.Eq("_id", channel) & filter.Eq("pins", message));
        }
        public async Task RemovePin(ulong channel, ulong message) {
            var col = db.GetCollection<DBChannelInfo>("channels");
            var filter = Builders<DBChannelInfo>.Filter;
            var update = Builders<DBChannelInfo>.Update;
            await col.UpdateOneAsync(filter.Eq("_id", channel), update.Pull("pins", message));
        }
        #endregion

        #region impl Action
        public async Task<ActionData?> GetAction(ulong message) {
            var col = db.GetCollection<DBActionInfo>("actions");
            var filter = Builders<DBActionInfo>.Filter;
            var data = await col.Find(filter.Eq("_id", message)).FirstAsync();
            return (data == null || data.Id == 0) ? null : new DBActionController(db, data);
        }
        public async Task SetAction(ulong message, int action) {
            var col = db.GetCollection<DBActionInfo>("actions");
            await col.InsertOneAsync(new DBActionInfo() { Id = message, ActionId = action });
        }
        #endregion

        #region impl Topic
        public async Task<TopicData[]> GetTopics(ulong guild) {
            var col = db.GetCollection<DBTopicInfo>("topics");
            var filter = Builders<DBTopicInfo>.Filter;
            return (await col.Find(filter.Eq("guild", guild)).ToListAsync()).ToArray();
        }
        public Task AddTopic(ulong guild, ulong channel, ulong role, string name, string? emote) {
            var col = db.GetCollection<DBTopicInfo>("topics");
            return col.InsertOneAsync(new DBTopicInfo { TopicGuild = guild, TopicChannel = channel, TopicRole = role, TopicName = name, TopicEmote = emote });
        }
        public async Task RemoveTopic(ulong guild, ulong channel) {
            var col = db.GetCollection<DBTopicInfo>("topics");
            var filter = Builders<DBTopicInfo>.Filter;
            await col.DeleteManyAsync(filter.Eq("_id", channel)); // Data model kind of doesn't care.
        }
        public async Task<TopicData?> GetTopicByName(ulong guild, string name) {
            var col = db.GetCollection<DBTopicInfo>("topics");
            var filter = Builders<DBTopicInfo>.Filter;
            return await col.Find(filter.Eq("guild", guild) & filter.Eq("name", name)).FirstOrDefaultAsync(); // Default -> null
            // Can't just return the Task because C# won't do an implicit cast DBTopicInfo -> TopicData
        }
        public async Task<TopicData?> GetTopicByEmote(ulong guild, string emote) {
            var col = db.GetCollection<DBTopicInfo>("topics");
            var filter = Builders<DBTopicInfo>.Filter;
            return await col.Find(filter.Eq("guild", guild) & filter.Eq("emote", emote)).FirstOrDefaultAsync();
        }
        public async Task<TopicData?> GetTopicByChannel(ulong guild, ulong channel) {
            var col = db.GetCollection<DBTopicInfo>("topics");
            var filter = Builders<DBTopicInfo>.Filter;
            return await col.Find(filter.Eq("_id", channel)).FirstOrDefaultAsync();
        }
        #endregion

        #region impl Moderation
        public Task AddModerationAction(ulong guild, ulong member, ulong moderator, DateTime since, byte type, DateTime? until = null, string? reason = null) {
            var col = db.GetCollection<DBModerationActionInfo>("moderation_events");
            return col.InsertOneAsync(new DBModerationActionInfo() {
                Guild = guild,
                Member = member,
                Moderator = moderator,
                Since = since,
                Until = until,
                Reason = reason,
                Type = type
            });
        }
        public Task<ModerationActionData[]> GetModerationActions(ulong guild, ulong member) {
            var col = db.GetCollection<DBModerationActionInfo>("moderation_events");
            var filter = Builders<DBModerationActionInfo>.Filter;
            // This is fucking disgusting by the way.
            return col.Find(filter.Eq("guild", guild) & filter.Eq("member", member)).ToCursorAsync().ContinueWith(x => x.Result.ToEnumerable().Cast<ModerationActionData>().ToArray());
        }
        public Task<ModerationActionData[]> GetActiveModerationActions(ulong guild, ulong member) {
            var col = db.GetCollection<DBModerationActionInfo>("moderation_events");
            var filter = Builders<DBModerationActionInfo>.Filter;
            return col.Find(filter.Eq("guild", guild) & filter.Eq("member", member) & filter.Gt("until", DateTime.UtcNow)).ToCursorAsync().ContinueWith(x => x.Result.ToEnumerable().Cast<ModerationActionData>().ToArray());
        }
        public Task<ModerationActionData[]> GetExpiredModerationActions() {
            var col = db.GetCollection<DBModerationActionInfo>("moderation_events");
            var filter = Builders<DBModerationActionInfo>.Filter;
            return col.Find(filter.Lt("until", DateTime.UtcNow)).ToCursorAsync().ContinueWith(x => x.Result.ToEnumerable().Cast<ModerationActionData>().ToArray());
        }
        public Task RemoveModerationActionCompletionTimer(ulong guild, ulong member, byte type) {
            var col = db.GetCollection<DBModerationActionInfo>("moderation_events");
            var filter = Builders<DBModerationActionInfo>.Filter;
            var update = Builders<DBModerationActionInfo>.Update;
            return col.UpdateManyAsync(filter.Eq("guild", guild) & filter.Eq("member", member) & filter.Gt("until", DateTime.UtcNow) & filter.Eq("type", type), update.Unset("until"));
        }
        #endregion

        #region impl Activity
        public Task AddActivity(ulong guild, ulong channel, ulong member, uint textScore, uint altScore, DateTime at) {
            var col = db.GetCollection<DBActivityInfo>("activity");
            return col.InsertOneAsync(new DBActivityInfo() { 
                Guild = guild,
                Channel = channel,
                Member = member,
                TextScore = textScore,
                AltScore = altScore,
                At = at
            });
        }
        public async Task<ActivityData> GetMemberActivityScore(ulong guild, ulong member, DateTime since) {
            var col = db.GetCollection<DBActivityInfo>("activity");
            var filter = Builders<DBActivityInfo>.Filter;
            return await col.Aggregate()
                .Match(filter.Eq("guild", guild) & filter.Eq("member", member) & filter.Gt("at", since))
                .Group(x => x.Member, x => new DBActivityInfo { AltScore = (uint)x.Sum(x => x.AltScore), TextScore = (uint)x.Sum(x => x.TextScore) })
                .FirstOrDefaultAsync() ?? new DBActivityInfo();
        }
        public async Task<ActivityData> GetChannelActivityScore(ulong guild, ulong channel, DateTime since) {
            var col = db.GetCollection<DBActivityInfo>("activity");
            var filter = Builders<DBActivityInfo>.Filter;
            return await col.Aggregate()
                .Match(filter.Eq("guild", guild) & filter.Eq("channel", channel) & filter.Gt("at", since))
                .Group(x => x.Member, x => new DBActivityInfo { AltScore = (uint)x.Sum(x => x.AltScore), TextScore = (uint)x.Sum(x => x.TextScore) })
                .FirstOrDefaultAsync() ?? new DBActivityInfo();
        }
        #endregion

        #region impl User
        public async Task<double> GetMemberXP(ulong guild, ulong member) {
            var col = db.GetCollection<DBUserInfo>("users");
            var filter = Builders<DBUserInfo>.Filter;
            var projection = Builders<DBUserInfo>.Projection;
            var doc = await col.Find(filter.Eq("_id", member)).Project(projection.Include("guild_xp")).FirstOrDefaultAsync();
            try {
                return doc.GetValue("GuildXP")?.AsBsonDocument?.GetValue(guild.ToString()).AsNullableDouble ?? default;
            #pragma warning disable CA1031 // Most specific exception I can see from this sequence.
            } catch (InvalidCastException) {
                return default;
            }
            #pragma warning restore CA1031
        }

        public Task GiveMemberXP(ulong guild, ulong member, double xp) {
            var col = db.GetCollection<DBUserInfo>("users");
            var filter = Builders<DBUserInfo>.Filter;
            var update = Builders<DBUserInfo>.Update;
            return col.UpdateOneAsync(filter.Eq("_id", member), update.Inc($"guild_xp.{guild.ToString()}", xp), new UpdateOptions { IsUpsert = true });
        }
        #endregion
    }
}
