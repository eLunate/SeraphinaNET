using System;
using System.Collections.Generic;
using System.Text;
using SeraphinaNET.Data;
using System.Threading.Tasks;
using Discord;

namespace SeraphinaNET.Services {
    class UserService {
        private readonly DataContextFactory data;
        private readonly ActivityService activity;

        private const double XP_ELASTIC_THRESHOLD = 200d;
        private const double XP_ELASTIC_TIME = 1; // Amount of hours that the threshold is over.
        private const uint LEVEL_ELASTIC_TARGET = 8; // Where the levels begin to get harder with elastic scaling
        private const double LEVEL_TIME_TARGET = 2; // Amount of hours (of the xp threshold) per level.

        private const double LEVEL_TIME_RATIO = LEVEL_TIME_TARGET / XP_ELASTIC_TIME;
        private const double LN2 = 0.693147180559945286227d; // Math.Log(2) is not a constant expression. ln(2) from FDLIBM.

        public UserService(DataContextFactory data, ActivityService activity) {
            this.data = data;
            this.activity = activity;
        }

        public Task GiveMemberXPRaw(ulong guild, ulong member, double amount) {
            using var db = data.GetContext();
            return db.GiveMemberXP(guild, member, amount);
        }
        public Task GiveMemberXPRaw(IGuildUser member, double amount) => GiveMemberXPRaw(member.GuildId, member.Id, amount);

        public async Task<double> GiveMemberXPScaled(ulong guild, ulong member, double amount) {
            var recentActivity = await activity.GetMemberActivity(guild, member, DateTime.UtcNow - TimeSpan.FromHours(XP_ELASTIC_TIME));
            var scaledAmount = amount / Math.Pow(2, recentActivity.AltScore <= 200 ? 1 : (recentActivity.AltScore / XP_ELASTIC_THRESHOLD));
            await GiveMemberXPRaw(guild, member, scaledAmount);
            return scaledAmount;
        }
        public Task GiveMemberXPScaled(IGuildUser member, double amount) => GiveMemberXPScaled(member.GuildId, member.Id, amount);

        // XP per level starts at XP_ELASTIC_THRESHOLD * LEVEL_TIME_RATIO
        // It doubles every LEVEL_ELASTIC_TARGET levels accoring to (3^k)f(x) = 3f(kx), where k is LEVEL_ELASTIC_TARGET (and x is the level)
        public static double LevelToXP(double level) 
            // ScaleB could be useful here if I can get another fractional part or settle for truncation.
            => 1/LN2 * XP_ELASTIC_THRESHOLD * LEVEL_TIME_RATIO * LEVEL_ELASTIC_TARGET * Math.Pow(2, level / LEVEL_ELASTIC_TARGET);

        public static double XPToLevel(double xp)
            // I'm hoping that constant folding means I don't have to worry about most of these expressions.
            => 1/LN2 * LEVEL_ELASTIC_TARGET * Math.Log((LN2 * XP_ELASTIC_THRESHOLD * LEVEL_TIME_RATIO) / (LEVEL_ELASTIC_TARGET * xp));

        public static XPStats XPToNextLevel(double xp) {
            var currentLevel = XPToLevel(xp);
            var nextXP = LevelToXP(Math.Ceiling(currentLevel));
            var prevXP = LevelToXP(Math.Floor(currentLevel));
            // Current
            return new XPStats(currentLevel, xp - prevXP, nextXP - xp, nextXP - prevXP);
        }

        public struct XPStats {
            public readonly double CurrentLevel, CurrentProgress, NeededXP, NextLevelXP;

            public XPStats(double currentLevel, double currentProgress, double neededXP, double nextXP) {
                this.CurrentLevel = currentLevel;
                this.CurrentProgress = currentProgress;
                this.NeededXP = neededXP;
                this.NextLevelXP = nextXP;
            }
        }
    }
}
