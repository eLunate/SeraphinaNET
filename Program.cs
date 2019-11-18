using System.Threading.Tasks;
using System;
using Discord.WebSocket;

namespace SeraphinaNET
{
    class Program
    {
        public static void Main(string[] args) {
            MainAsync().GetAwaiter().GetResult();
        }
        public static async Task MainAsync() {
            throw new NotImplementedException();
            //var data;
            var discord = new DiscordSocketClient(); // I don't have code completion
            using var seraphina = new SeraphinaCore(discord, data);
            await seraphina.Connect(Environment.GetEnvironmentVariable("BOT_TOKEN") ?? throw new InvalidOperationException("BOT_TOKEN not in environment"));

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }
    }
}
