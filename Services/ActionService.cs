using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using System.Linq;
using SeraphinaNET.Data;

namespace SeraphinaNET.Services {
    public abstract class Action {
        internal Action() { } // Please don't extend this outside of the assembly
                              // Not that Seraphina is designed for that to be possible, but don't anyway.

        public abstract Task ActionOn(ActionData action, SocketReaction reaction);
        public abstract Task ActionOff(ActionData action, SocketReaction reaction);
        public virtual bool ValidateEmote(IEmote emote) => Emotes.Contains(emote);

        public abstract IEmote[] Emotes { get; }
    }

    public sealed class ActionService {
        private readonly DataContextFactory data;

        private static IEmote GetEmote(string emoteStr) {
            if (Emote.TryParse(emoteStr, out var emote)) return emote;
            else return new Emoji(emoteStr);
        }

        private abstract class RadioAction : Action {
            public async sealed override Task ActionOn(ActionData action, SocketReaction reaction) {
                if (!await ToggleOn(reaction)) {
                    if(!((reaction.Message.IsSpecified ? reaction.Message.Value : await reaction.Channel.GetMessageAsync(reaction.MessageId)) is IUserMessage message)) {
                        Console.WriteLine("Can't do radio actions on non-user messages");
                        return;
                    }
                    // I should be able to do this without getting the full user object and this infuriates me.
                    await message.RemoveReactionAsync(reaction.Emote, reaction.User.IsSpecified ? reaction.User.Value : await reaction.Channel.GetUserAsync(reaction.UserId));
                    return;
                }
                var radio = await action.GetRadioData(reaction.UserId);
                await action.SetRadioData(reaction.UserId, reaction.Emote.ToString()); // All IEmote can be deserialized from this format
                if (radio != null) {
                    var emote = GetEmote(radio);
                    if (!((reaction.Message.IsSpecified ? reaction.Message.Value : await reaction.Channel.GetMessageAsync(reaction.MessageId)) is IUserMessage message)) {
                        Console.WriteLine("Can't do radio actions on non-user messages");
                        return;
                    }
                    var toggle = ToggleOff(reaction.Channel, message, reaction.UserId, emote);
                    await message.RemoveReactionAsync(reaction.Emote, reaction.User.IsSpecified ? reaction.User.Value : await reaction.Channel.GetUserAsync(reaction.UserId));
                    await toggle;
                } 
            }

            public async sealed override Task ActionOff(ActionData action, SocketReaction reaction) {
                var radio = await action.GetRadioData(reaction.UserId);
                if (radio == null) {
                    Console.WriteLine("What t he  fuc k"); // Pretty sure this is an invalid state
                    return;
                }
                var emote = GetEmote(radio);
                if (emote == reaction.Emote) {
                    await Task.WhenAll(
                        ToggleOff(reaction.Channel, reaction.Message.Value ?? (IUserMessage)(await reaction.Channel.GetMessageAsync(reaction.MessageId)), reaction.UserId, emote),
                        action.SetRadioData(reaction.UserId, null)
                    );
                } // Otherwise, Seraphina killed the reaction.
            }

            protected abstract Task<bool> ToggleOn(SocketReaction reaction);
            protected abstract Task ToggleOff(ISocketMessageChannel channel, IUserMessage message, ulong userId, IEmote emote); // fuck
        }

        // Tally and radio have so much shared code that I might merge them under a common parent.
        private abstract class TallyAction : Action {
            public async sealed override Task ActionOn(ActionData action, SocketReaction reaction) {
                var tally = (await action.GetTallyData(reaction.UserId)).Select(x => GetEmote(x)).ToArray();
                if (!await ToggleOn(tally, reaction)) {
                    if (!((reaction.Message.IsSpecified ? reaction.Message.Value : await reaction.Channel.GetMessageAsync(reaction.MessageId)) is IUserMessage message)) {
                        Console.WriteLine("Can't do toggle actions on non-user messages");
                        return;
                    }
                    await message.RemoveReactionAsync(reaction.Emote, reaction.User.IsSpecified ? reaction.User.Value : await reaction.Channel.GetUserAsync(reaction.UserId));
                    return;
                }
                await action.AddTally(reaction.UserId, reaction.Emote.ToString() ?? throw new ArgumentNullException("Reaction emote has null name somehow. Asshole emote."));
            }

            public async sealed override Task ActionOff(ActionData action, SocketReaction reaction) {
                var tally = (await action.GetTallyData(reaction.UserId)).Select(x => GetEmote(x)).ToArray();
                if (tally.Length == 0) {
                    Console.WriteLine("What th e f uck"); // Pretty sure this is also invalid
                    return;
                }
                if (tally.Contains(reaction.Emote)) {
                    await Task.WhenAll(
                        ToggleOff(tally, reaction.Channel, reaction.Message.IsSpecified ? reaction.Message.Value : (IUserMessage)(await reaction.Channel.GetMessageAsync(reaction.MessageId)), reaction.UserId, reaction.Emote),
                        action.RemoveTally(reaction.UserId, reaction.Emote.ToString() ?? throw new ArgumentNullException("Reaction emote has null name and it hurts the soul."))
                    ); 
                } // Seraphina removed the reaction otherwise so it's all good?
            }

            protected abstract Task<bool> ToggleOn(IEmote[] tallies, SocketReaction reaction);
            protected abstract Task ToggleOff(IEmote[] tallies, ISocketMessageChannel channel, IUserMessage message, ulong userId, IEmote emote);
        }

        private abstract class ButtonAction : Action {
            public async sealed override Task ActionOn(ActionData action, SocketReaction reaction) {
                var actTask = this.Action(reaction);
                if ((reaction.Message.IsSpecified ? reaction.Message.Value : await reaction.Channel.GetMessageAsync(reaction.MessageId)) is IUserMessage message) {
                    await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value ?? await reaction.Channel.GetUserAsync(reaction.UserId));
                }
                await actTask;
            }

            public sealed override Task ActionOff(ActionData action, SocketReaction reaction) => Task.CompletedTask; // Only Seraphina does this

            protected abstract Task Action(SocketReaction reaction);
        }

        private sealed class ButtonTest : ButtonAction {
            private static readonly IEmote[] _emotes = new IEmote[] {
                new Emoji("✅")
            };
            public sealed override IEmote[] Emotes => _emotes;

            protected sealed override Task Action(SocketReaction reaction) => reaction.Channel.DeleteMessageAsync(reaction.MessageId);
        }

        public ActionService(DataContextFactory data) { this.data = data; }

        private static readonly Action[] actions = new Action[] {
            new ButtonTest()
        };

        private static readonly Dictionary<string, int> aliases = new Dictionary<string, int> {
            ["button_test"] = 0,
        };

        private static Action GetAction(ActionData action) {
            return actions[action.ActionType]; // God forbid should the action id be out of range.
        }

        private static int ActionId(Action action) {
            return Array.IndexOf(actions, action);
        }

        public static Action GetAction(int actionId) {
            return actions[actionId];
        }

        public static Action GetAction(string actionAlias) {
            // Please just blow up.
            return actions[aliases[actionAlias]];
        }

        public async Task HandleReactionAdd(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction) {
            if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;
            using var db = data.GetContext();
            var actionData = await db.GetAction(message.Id);
            if (actionData == null) return;
            var action = GetAction(actionData);
            if (action.ValidateEmote(reaction.Emote)) await action.ActionOn(actionData, reaction);
        }

        public async Task HandleReactionRemove(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction) {
            using var db = data.GetContext();
            var actionData = await db.GetAction(message.Id);
            if (actionData == null) return;
            var action = GetAction(actionData);
            if (action.ValidateEmote(reaction.Emote)) await action.ActionOff(actionData, reaction);
        }

        public async Task AttachAction(Action action, IUserMessage message) {
            using var db = data.GetContext();
            var id = ActionId(action);
            if (id < 0) throw new ArgumentException("Action is not registered. The fuck did you do?");
            await Task.WhenAll(
                message.AddReactionsAsync(action.Emotes),
                db.SetAction(message.Id, ActionId(action))
            );
        }
    }
}
