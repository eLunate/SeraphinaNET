using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeraphinaNET.Data {
    sealed public class MySQLData : DataContextFactory {
        private readonly DbContextOptions options;
        DataContext DataContextFactory.GetContext() => GetContext();
        public MySQLContext GetContext() => new MySQLContext(options);

        public MySQLData(string connectionString) {
            this.options = new DbContextOptionsBuilder()
                .UseMySql(connectionString)
                .Options;
        }
    }

    sealed internal class DesignFactory : IDesignTimeDbContextFactory<MySQLContext> {
        MySQLContext IDesignTimeDbContextFactory<MySQLContext>.CreateDbContext(string[] _) {
            var options = new DbContextOptionsBuilder()
                .UseMySql(Environment.GetEnvironmentVariable("DB_STRING"))
                .Options;
            return new MySQLContext(options);
        }
    }

    sealed public class MySQLContext : DbContext, DataContext {
        #pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        // Given a proper initialisation path they'll be initialised dynamically.
        internal MySQLContext(DbContextOptions options) : base(options) {}
        #pragma warning restore CS8618 

        protected override void OnModelCreating(ModelBuilder model) {
            model.Entity<SQLActivity>().HasKey(k => new { k.Guild, k.Member, k.At });
            model.Entity<SQLModerationAction>().HasKey(k => new { k.Guild, k.Member, k.Since, k.Type });
            model.Entity<SQLPin>().HasKey(k => new { k.Channel, k.Message });
            model.Entity<SQLTopic>().HasKey(k => new { k.Guild, k.Channel });
            model.Entity<SQLActionRadioData>().HasKey(k => new { k.Message, k.Member });
            model.Entity<SQLActionTallyData>().HasKey(k => new { k.Message, k.Member, k.Emoji });
            model.Entity<SQLAction>().HasKey(k => k.Message);
            model.Entity<SQLMemberXP>().HasKey(k => new { k.Member, k.Guild });
        }

        #region Data Mappings
        #pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        private class SQLActivityData : ActivityData {
            public uint TextScore { get; set; }

            public uint AltScore { get; set; }
        }

        private DbSet<SQLActivity> Activities { get; set; }
        [Table("Activity")]
        private class SQLActivity {
            public ulong Guild { get; set; }
            public ulong Channel { get; set; }
            public ulong Member { get; set; }
            public uint TextScore { get; set; }
            public uint AltScore { get; set; }
            public DateTime At { get; set; }
        }

        private DbSet<SQLModerationAction> ModerationActions { get; set; }
        [Table("ModerationAction")]
        private class SQLModerationAction : ModerationActionData {       
            [Column("moderator")]
            public ulong Moderator { get; set; }       
            [Column("member")]
            public ulong Member { get; set; }       
            [Column("guild")]
            public ulong Guild { get; set; }       
            [Column("since")]
            public DateTime Since {get;set;}       
            [Column("until")]
            public DateTime? Until {get;set;}       
            [Column("reason")]
            public string? Reason {get;set;}       
            [Column("type")]
            public byte Type {get;set;}
        }

        private DbSet<SQLPin> Pins { get; set; }
        [Table("Pin")]
        private class SQLPin {            
            [Column("channel")]
            public ulong Channel { get; set; }            
            [Column("message")]
            public ulong Message { get; set; }
        }

        private DbSet<SQLTopic> Topics { get; set; }
        [Table("Topic")]
        private class SQLTopic {
            public ulong Guild { get; set; }
            public ulong Channel { get; set; }
            public ulong Role { get; set; }
            public string Name { get; set; }
            public string? Emote { get; set; }
        }

        private DbSet<SQLActionRadioData> ActionRadioData { get; set; }
        [Table("ActionRadioData")]
        private class SQLActionRadioData {
            [Column("message")]
            public ulong Message { get; set; }
            [Column("member")]
            public ulong Member { get; set; }
            [Column("emoji")]
            public string Emoji { get; set; }
        }

        private DbSet<SQLActionTallyData> ActionTallyData { get; set; }
        [Table("ActionTallyData")]
        private class SQLActionTallyData {
            public ulong Message { get; set; }
            public ulong Member { get; set; }
            public string Emoji { get; set; }
        }

        private DbSet<SQLAction> Actions { get; set; }
        [Table("Action")]
        private class SQLAction : ActionData {
            [NotMapped]
            public MySQLContext Context { set; get; }
            public ulong Message { get; set; }
            public int ActionType { get; set; }

            public SQLAction WithContext(MySQLContext context) {
                this.Context = context;
                return this;
            }

            public Task AddTally(ulong user, string emote) {
                Context.ActionTallyData.Add(new SQLActionTallyData {
                    Emoji = emote,
                    Member = user,
                    Message = Message
                });
                return Context.SaveChangesAsync();
            }
            public async Task<string?> GetRadioData(ulong user) {
                return (await Context.ActionRadioData.FindAsync(this.Message, user))?.Emoji;
            }
            public async Task<string[]> GetTallyData(ulong user) {
                return await Context.ActionTallyData.Where(k => k.Message == this.Message && k.Member == user).Select(x => x.Emoji).ToArrayAsync();
            }
            public Task RemoveTally(ulong user, string emote) {
                Context.ActionTallyData.Remove(new SQLActionTallyData { Message = this.Message, Member = user, Emoji = emote });
                return Context.SaveChangesAsync();
            }
            public Task SetRadioData(ulong user, string? data) {
                if (data is null) {
                    Context.ActionRadioData.Remove(new SQLActionRadioData { Message = this.Message, Member = user });
                    return Context.SaveChangesAsync();
                } else {
                    return Context.Database.ExecuteSqlInterpolatedAsync($"REPLACE INTO ActionRadioData (message, member, emoji) VALUES ({this.Message}, {user}, {data})");
                }
            }
        }

        private DbSet<SQLMemberXP> MemberXP { get; set; }
        [Table("MemberXP")]
        private class SQLMemberXP {
            [Column("member")]
            public ulong Member { get; set; }
            [Column("guild")]
            public ulong Guild { get; set; }
            [Column("xp")]
            public double XP { get; set; }
        }
        #pragma warning restore CS8618
        #endregion

        Task DataContext.AddActivity(ulong guild, ulong channel, ulong member, uint textScore, uint altScore, DateTime at) {
            Activities.Add(new SQLActivity {
                Guild = guild,
                Channel = channel,
                Member = member,
                TextScore = textScore,
                AltScore = altScore,
                At = at
            });
            return SaveChangesAsync();
        }
        Task DataContext.AddModerationAction(ulong guild, ulong member, ulong moderator, DateTime since, byte type, DateTime? until, string? reason) {
            ModerationActions.Add(new SQLModerationAction {
                Moderator = moderator,
                Member = member,
                Guild = guild,
                Since = since,
                Until = until,
                Type = type,
                Reason = reason
            });
            return SaveChangesAsync();
        }
        Task DataContext.AddPin(ulong channel, ulong message) {
            Pins.Add(new SQLPin {
                Channel = channel,
                Message = message
            });
            return SaveChangesAsync();
        }
        Task DataContext.AddTopic(ulong guild, ulong channel, ulong role, string name, string? emote) {
            Topics.Add(new SQLTopic {
                Guild = guild,
                Channel = channel,
                Role = role,
                Name = name,
                Emote = emote
            });
            return SaveChangesAsync();
        }
        Task DataContext.ClearPins(ulong channel) {
            return Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Pin WHERE channel = {channel}");
        }
        async Task<ActionData?> DataContext.GetAction(ulong message) {
            return (await Actions.FindAsync(message))?.WithContext(this);
        }
        Task<ModerationActionData[]> DataContext.GetActiveModerationActions(ulong guild, ulong member) {
            return ModerationActions.Where(x => x.Until != null && x.Until > DateTime.UtcNow).Cast<ModerationActionData>().ToArrayAsync();
        }
        Task<ActivityData> DataContext.GetChannelActivityScore(ulong guild, ulong channel, DateTime since) {
            // SELECT SUM(text_score) as text_score, SUM(alt_score) as alt_score FROM Activity WHERE guild = @guild AND channel = @channel AND at > @since
            return Activities.Where(x => x.Guild == guild && x.Channel == channel && x.At > since)
                .GroupBy(_ => 1)
                .Select(g => (ActivityData) new SQLActivityData { TextScore = (uint)g.Sum(x => x.TextScore), AltScore = (uint)g.Sum(x => x.AltScore) })
                .FirstOrDefaultAsync();
        }
        Task<ModerationActionData[]> DataContext.GetExpiredModerationActions() {
            return ModerationActions.Where(x => x.Until != null && x.Until < DateTime.UtcNow).Cast<ModerationActionData>().ToArrayAsync();
        }
        Task<ActivityData> DataContext.GetMemberActivityScore(ulong guild, ulong member, DateTime since) {
            return Activities.Where(x => x.Guild == guild && x.Member == member && x.At > since)
                .GroupBy(_ => 1)
                .Select(g => (ActivityData)new SQLActivityData { TextScore = (uint)g.Sum(x => x.TextScore), AltScore = (uint)g.Sum(x => x.AltScore) })
                .FirstOrDefaultAsync();
        }
        async Task<double> DataContext.GetMemberXP(ulong guild, ulong member) {
            return (await MemberXP.FindAsync(member, guild))?.XP ?? 0; // ?? default
        }
        Task<ModerationActionData[]> DataContext.GetModerationActions(ulong guild, ulong member) {
            return ModerationActions.Where(x => x.Guild == guild && x.Member == member).Cast<ModerationActionData>().ToArrayAsync();
        }
        Task<ulong[]> DataContext.GetPins(ulong channel) {
            return Pins.Where(x => x.Channel == channel).Select(x => x.Message).ToArrayAsync();
        }
        async Task<TopicData?> DataContext.GetTopicByChannel(ulong guild, ulong channel) {
            return (TopicData?)await Topics.FindAsync(guild, channel);
        }
        async Task<TopicData?> DataContext.GetTopicByEmote(ulong guild, string emote) {
            return (TopicData?)await Topics.Where(x => x.Guild == guild && x.Emote == emote).FirstOrDefaultAsync();
        }
        async Task<TopicData?> DataContext.GetTopicByName(ulong guild, string name) {
            return (TopicData?)await Topics.Where(x => x.Guild == guild && x.Name == name).FirstOrDefaultAsync();
        }
        Task<TopicData[]> DataContext.GetTopics(ulong guild) {
            return Topics.Where(x => x.Guild == guild).Cast<TopicData>().ToArrayAsync();
        }
        Task DataContext.GiveMemberXP(ulong guild, ulong member, double xp) {
            return Database.ExecuteSqlInterpolatedAsync($"UPDATE MemberXP SET xp = xp + {xp} WHERE guild = {guild} AND member = {member}");
        }
        Task<bool> DataContext.IsPinned(ulong channel, ulong message) {
            return Pins.Where(x => x.Channel == channel && x.Message == message).AnyAsync();
        }
        Task DataContext.RemoveModerationActionCompletionTimer(ulong guild, ulong member, byte type) {
            return Database.ExecuteSqlInterpolatedAsync($"UPDATE ModerationAction SET until = NULL WHERE guild = {guild} AND member = {member} AND type = {type}");
        }
        Task DataContext.RemovePin(ulong channel, ulong message) {
            Pins.Remove(new SQLPin { Channel = channel, Message = message });
            return SaveChangesAsync();
        }
        Task DataContext.RemoveTopic(ulong guild, ulong channel) {
            Topics.Remove(new SQLTopic { Guild = guild, Channel = channel });
            return SaveChangesAsync();
        }
        Task DataContext.SetAction(ulong message, int action) {
            Actions.Add(new SQLAction { Message = message, ActionType = action });
            return SaveChangesAsync();
        }
        async Task DataContext.SetPins(ulong channel, ulong[] messages) {
            // await (this as DataContext).ClearPins(channel);
            await Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Pin WHERE channel = {channel}");
            Pins.AddRange(messages.Select(x => new SQLPin { Channel = channel, Message = x }).ToArray());
            await SaveChangesAsync();
        }
    }
}
