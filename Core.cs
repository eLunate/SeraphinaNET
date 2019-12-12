// Core;
// Takes a database factory
// And by the looks of things a Discord client token

using Discord;
using SeraphinaNET.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;
using System;

namespace SeraphinaNET {
    class SeraphinaCore : IDisposable {
        private readonly DataContextFactory data;
        private readonly DiscordSocketClient discord;

        // This is IDisposable, and it's going to give me a seizure at this rate.
        private readonly ServiceProvider services;

        public SeraphinaCore(DiscordSocketClient discord, DataContextFactory data) {
            this.discord = discord;
            this.data = data;
            this.services = new ServiceCollection()
            .AddSingleton(discord)
            .AddSingleton(data)
            .AddSingleton<CommandService>()
            .AddSingleton<PinService>()
            .AddSingleton<ActionService>()
            .AddSingleton<TopicService>()
            .AddSingleton<ModerationService>()
            .BuildServiceProvider();

            var actions = services.GetRequiredService<ActionService>();

            
            discord.Log += Log;
            discord.MessageReceived += MessageReceived;
            discord.ReactionAdded += actions.HandleReactionAdd;
            discord.ReactionRemoved += actions.HandleReactionRemove;

            var commandService = services.GetRequiredService<CommandService>();
            commandService.Log += Log;
            commandService.CommandExecuted += CommandExecuted;
            commandService.AddModuleAsync<Commands.BasicCommands>(services);
            commandService.AddModuleAsync<Commands.PinCommands>(services);
            commandService.AddModuleAsync<Commands.TopicsCommands>(services);
        }
        
        public async Task Connect() {
            await discord.StartAsync();
        }
        public async Task Connect(string token) {
            await discord.LoginAsync(TokenType.Bot, token);
            await Connect();
        }
        private static Task Log(LogMessage msg) {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage message) {
            // Ignore system messages, or messages from other bots. For now.
            if (!(message is SocketUserMessage userMessage)) return;
            if (message.Source != MessageSource.User) return;

            var argPos = 0;
            if (userMessage.HasCharPrefix('!', ref argPos) || userMessage.HasMentionPrefix(discord.CurrentUser, ref argPos)) {
                // This cast is safe because this event is only attached if the client is a socket client.
                var ctx = new SocketCommandContext(discord, userMessage);
                await services.GetRequiredService<CommandService>().ExecuteAsync(ctx, argPos, services);
                return;
            }
        }

        private static async Task CommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result) {
            if (!command.IsSpecified) {
                await context.Channel.SendMessageAsync("You fucked up: That command doesn't exist.");
                return;
            }

            // the command was successful, we don't care about this result, unless we want to log that a command succeeded.
            if (result.IsSuccess)
                return;

            // the command failed, let's notify the user that something happened.
            await context.Channel.SendMessageAsync($"I fucked up: {result}");
        }

        public void Dispose() {
            this.services.Dispose();
        }
    }
}