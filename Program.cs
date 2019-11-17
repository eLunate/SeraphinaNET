using System.Threading.Tasks;
using System;

namespace SeraphinaNET
{
    class Program
    {
        public static void Main(string[] args) {
            MainAsync().GetAwaiter().GetResult();
        }
        public static async Task MainAsync() {
            var data = throw new NotImplementedException();
            var discord = throw new NotImplementedException(); // I don't have code completion
            using var seraphina = new SeraphinaCore(discord, data);
            await seraphina.Connect("seizure token from environment");

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }
    }
}
