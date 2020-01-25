using Discord.Commands;
using System.Threading.Tasks;
using SeraphinaNET.Services;

namespace SeraphinaNET.Commands {
    class UserCommands : ModuleBase<SocketCommandContext> {
        private readonly UserService user;

        public UserCommands(UserService user) {
            this.user = user;
        }

        [Command("xp")]
        [RequireContext(ContextType.Guild)]
        public async Task GetXP() => await ReplyAsync((await user.GetMemberXP(Context.Guild.Id, Context.User.Id)).ToString());

        [Command("level")]
        [RequireContext(ContextType.Guild)]
        public async Task GetLevel() => await ReplyAsync(UserService.XPToLevel(await user.GetMemberXP(Context.Guild.Id, Context.User.Id)).ToString());
    }
}