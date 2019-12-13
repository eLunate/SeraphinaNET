using Discord.Commands;
using System.Threading.Tasks;
using SeraphinaNET.Services;

namespace SeraphinaNET.Commands {
    class BasicCommands : ModuleBase<SocketCommandContext> {
        private readonly ActionService actions;

        public BasicCommands(ActionService actions) {
            this.actions = actions;
        }

        [Command("ping")]
        public Task Ping() => ReplyAsync("Pong!");

        [Command("echo")]
        public Task Echo(string echo) => ReplyAsync(echo);

        [Command("action_test")]
        public Task ActionTest() => actions.AttachAction(ActionService.GetAction(0), Context.Message);
    }
}