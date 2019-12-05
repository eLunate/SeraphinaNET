using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using System.Linq;

namespace SeraphinaNET.Commands {
    class TopicsCommands : ModuleBase<SocketCommandContext> {
        private readonly TopicService topics;

        public TopicsCommands(TopicService topics) {
            this.topics = topics;
        }

        [Command("topic_join")]
        [RequireContext(ContextType.Guild)]
        public async Task AddUserToTopic(string topicName) {
            var topic = await topics.GetTopic(Context.Guild, topicName);
            if (topic == null) {
                // God this entire library/framework is such a fucking nightmare.
                // This is begging for somebody to write !topic_join @everyone
                // await ReplyAsync($"No such topic `{topicName}`");
                await ReplyAsync($"No such topic");
                return;
            }
            // They couldn't just give me a GuildUser if I'm requiring the Guild context?
            await topics.AddUser(topic, Context.Guild.GetUser(Context.User.Id));
        }

        [Command("topic_leave")]
        [RequireContext(ContextType.Guild)]
        public async Task RemoveUserFromTopic(string topicName) {
            var topic = await topics.GetTopic(Context.Guild, topicName);
            if (topic == null) {
                await ReplyAsync("No such topic");
                return;
            }
            await topics.RemoveUser(topic, Context.Guild.GetUser(Context.User.Id));
        }

        //[Command("topic_leave")]
        //[RequireContext(ContextType.Guild)]
        //public async Task RemoveUserFromTopic() => await RemoveUserFromTopic((await topics.GetTopic((IGuildChannel)Context.Channel)).TopicName); 
        // Oh it's awfully inefficient. Kind of don't care rn.
        // Refactor later.

        [Command("topic_add")]
        [RequireContext(ContextType.Guild)]
        public async Task AddTopic(string roleName, string name, string? emoji) {
            var role = Context.Guild.Roles.Where(x => x.Name == roleName).FirstOrDefault(); // Hello? Should be able to get by name.
            if (role == null) {
                await ReplyAsync("No such role");
                return;
            }
            // I'm just going to hard cast and it can suck one if it blows up.
            await topics.AddTopic((IGuildChannel)Context.Channel, role, name, emoji);
        }

        [Command("topic_remove")]
        [RequireContext(ContextType.Guild)]
        public async Task RemoveTopic() {
            var topic = await topics.GetTopic((IGuildChannel)Context.Channel);
            await (topic == null ? ReplyAsync("No topic in this channel") : topics.RemoveTopic(topic));
        }
    }
}
