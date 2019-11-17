using Discord.Commands;
using System.Threading.Tasks;

namespace SeraphinaNET.Commands {
    class BasicCommands : ModuleBase<SocketCommandContext> {
        [Command("ping")]
        public Task Ping() => ReplyAsync("Pong!");

        [Command("echo")]
        public Task Echo(string echo) => ReplyAsync(echo);
    }
}