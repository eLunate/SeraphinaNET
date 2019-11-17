using Discord.Commands;
using Discord;
using System.Threading.Tasks;
using SeraphinaNET;

namespace SeraphinaNET.Commands {
    public class PinCommands : ModuleBase<SocketCommandContext> {
        [Command("pin_add")]
        public async Task AddPin(ulong messageId) {
            if(!(await Context.Channel.GetMessageAsync(messageId) is IUserMessage message)) {
                await ReplyAsync("Fuck you I couldn't find that message");
                return;
            }
            
        }
    }
}