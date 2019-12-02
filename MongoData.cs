using System;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using System.Threading.Tasks;

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

        [BsonIgnoreExtraElements]
        private class DBChannelInfo {
            // Mongo uses _id
            [BsonId]
            public ulong Id { get; set; }
            [BsonElement("pins")]
            public ulong[]? Pins { get; set; }
        }
        static MongoDataContext() { 
            BsonClassMap.RegisterClassMap<DBChannelInfo>(); 
        }

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
            var info = await col.Find(filter.Eq("_id", channel)).SingleOrDefaultAsync();
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

        // Stub Dispose method as Mongo thinks you should only have one Client.
        // So the factory holds this and calls it a day.
        public void Dispose() {}
    }
}
