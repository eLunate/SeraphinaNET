using System;
using System.Threading.Tasks;
namespace SeraphinaNET.Data {
    public interface DataContextFactory {
        // I hate factories but they seem to be the best way to encapsulate 
        // hidden arguments from an external context.
        DataContext GetContext();
    }

    public interface DataContext : IDisposable {
        #region Pins
        Task<ulong[]> GetPins(ulong channel);
        Task ClearPins(ulong channel);
        Task AddPin(ulong channel, ulong message);
        Task RemovePin(ulong channel, ulong message);
        Task<bool> IsPinned(ulong channel, ulong message);
        #endregion
    }
}