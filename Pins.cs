using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using Discord;

using SeraphinaNET.Data;

namespace SeraphinaNET {
    public readonly struct PinData {
        public readonly IUserMessage message;
        public readonly bool superPin;

        public PinData(IUserMessage message, bool superPin) {
            this.message = message;
            this.superPin = superPin;
        }
    }
    public sealed class PinService {
        private readonly DataContextFactory data;
        private readonly IDiscordClient client;
        public PinService(DataContextFactory data, IDiscordClient client) {
            this.data = data;
            this.client = client;
        }

        public async Task ClearPins(IMessageChannel channel) {
            using (var db = data.GetContext()) {
                await db.ClearPins(channel.Id);
            }
        }

        public async Task AddPin(IUserMessage message) {
            await message.PinAsync();
            using (var db = data.GetContext()) {
                await db.AddPin(message.Channel.Id, message.Id);
            }
        }

        public async Task RemovePin(IUserMessage message) {
            await message.UnpinAsync();
            using (var db = data.GetContext()) {
                await db.RemovePin(message.Channel.Id, message.Id);
            }
        }

        public async Task Refresh(IMessageChannel channel) {
            var pinsTask = channel.GetPinnedMessagesAsync();
            using (var db = data.GetContext()) {
                var superPinsTask = db.GetPins(channel.Id);
                await Task.WhenAll(pinsTask, superPinsTask);
                var superPins = superPinsTask.Result;
                await Repin(pinsTask.Result.OfType<IUserMessage>().Where(x => superPins.Contains(x.Id)));
            }
        }
        public async Task Repin(IEnumerable<IUserMessage> messages) {
            foreach (var message in messages) {
                await message.UnpinAsync();
                await message.PinAsync();
            }
        }

        public async Task<IEnumerable<PinData>> GetPins(IMessageChannel channel) {
            using (var db = data.GetContext()) {
                var pinIdsTask = db.GetPins(channel.Id);
                var pinsTask = channel.GetPinnedMessagesAsync();
                await Task.WhenAll(pinIdsTask, pinsTask);
                var pinIds = pinIdsTask.Result;
                return pinsTask.Result.OfType<IUserMessage>().Select(x => new PinData(x, pinIds.Contains(x.Id)));
            }
        }
        public async Task<PinData?> GetPin(IUserMessage message) {
            using (var db = data.GetContext()) {
                if (message.IsPinned) return new PinData(message, await db.IsPinned(message.Channel.Id, message.Id));
                return null;
            }
        }
    }
}