using System.Threading.Tasks;
using System;
using Discord.WebSocket;
using SeraphinaNET.Data;

namespace SeraphinaNET
{
    class Program
    {
        public static void Main(string[] args) {
            MainAsync().GetAwaiter().GetResult();
        }
        public static async Task MainAsync() {
            var data = new MongoData(
                Environment.GetEnvironmentVariable("DB_STRING") ?? throw new InvalidOperationException("DB_STRING not in environment"), 
                Environment.GetEnvironmentVariable("DB_NAME") ?? "dev"
            );
            var discord = new DiscordSocketClient(); 
            using var seraphina = new SeraphinaCore(discord, data);
            await seraphina.Connect(Environment.GetEnvironmentVariable("BOT_TOKEN") ?? throw new InvalidOperationException("BOT_TOKEN not in environment"));

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }
    }
}
