using Discord.Commands;
using Discord;
using System.Threading.Tasks;
using System.Linq;
using SeraphinaNET;

namespace SeraphinaNET.Commands {
    public class PinCommands : ModuleBase<SocketCommandContext> {
        private readonly PinService pins;

        public PinCommands(PinService pins) {
            this.pins = pins;
        }


        [Command("pin_add")]
        public async Task AddPin(ulong messageId) {
            if(!(await Context.Channel.GetMessageAsync(messageId) is IUserMessage message)) {
                await ReplyAsync("Fuck you I couldn't find that message");
                return;
            }
            await pins.AddPin(message);
        }

        [Command("pin_remove")]
        public async Task RemovePin(ulong messageId) {
            if (!(await Context.Channel.GetMessageAsync(messageId) is IUserMessage message)) {
                await ReplyAsync("Fuck you I couldn't find that message");
                return;
            }
            await pins.RemovePin(message);
        }

        [Command("pin_clearall")]
        public async Task ClearPins() {
            await pins.ClearPins(Context.Channel);
            await ReplyAsync("Done. You monster.");
        }

        [Command("pin_refresh")]
        public async Task RefreshPins() {
            await pins.Refresh(Context.Channel);
            await ReplyAsync("Done.");
        }

        [Command("pin_list")]
        public async Task ListPins() {
            await ReplyAsync(string.Join("; ", (await pins.GetPins(Context.Channel)).Select(x => x.superPin)));
        }
    }
}