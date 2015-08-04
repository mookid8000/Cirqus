using System.Collections.Generic;

namespace d60.Cirqus.Views
{
    /// <summary>
    /// Crude consensus protocol
    /// </summary>
    public interface IAutoDistributionPersistence
    {
        /// <summary>
        /// Registers a heartbeat for this particular ID and registers it as online/offline. If registered as online,
        /// the IDs of the views currently to be managed by this ID are returned.
        /// </summary>
        IEnumerable<string> Heartbeat(string id, bool online);
        
        /// <summary>
        /// Gets the current distribution of views among online IDs.
        /// </summary>
        Dictionary<string, HashSet<string>> GetCurrentState();

        /// <summary>
        /// Updates the current distribution
        /// </summary>
        void SetNewState(Dictionary<string, HashSet<string>> newState);
    }
}