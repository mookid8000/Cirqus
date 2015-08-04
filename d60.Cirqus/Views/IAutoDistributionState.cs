using System.Collections.Generic;

namespace d60.Cirqus.Views
{
    /// <summary>
    /// Crude consensus protocol
    /// </summary>
    public interface IAutoDistributionState
    {
        /// <summary>
        /// Registers a heartbeat for this particular ID and registers it as online/offline. If registered as online,
        /// the IDs of the views currently to be managed by this ID are returned.
        /// </summary>
        IEnumerable<string> Heartbeat(string id, bool online);
        
        /// <summary>
        /// Gets the current distribution of views among online IDs.
        /// </summary>
        IEnumerable<AutoDistributionState> GetCurrentState();

        /// <summary>
        /// Updates the current distribution
        /// </summary>
        void SetNewState(IEnumerable<AutoDistributionState> newState);
    }

    /// <summary>
    /// Represents one manager and the IDs of the views that it manages/is supposed to manage
    /// </summary>
    public class AutoDistributionState
    {
        /// <summary>
        /// Constructs the state for the given manager
        /// </summary>
        public AutoDistributionState(string managerId)
        {
            ManagerId = managerId;
            ViewIds = new HashSet<string>();
        }

        /// <summary>
        /// Constructs the state for the given manager and the given view IDs
        /// </summary>
        public AutoDistributionState(string managerId, IEnumerable<string> viewIds)
        {
            ManagerId = managerId;
            ViewIds = new HashSet<string>(viewIds);
        }

        /// <summary>
        /// Gets the ID of the manager
        /// </summary>
        public string ManagerId { get; private set; }

        /// <summary>
        /// Gets the IDs of the views that this manager manages/is supposed to manage
        /// </summary>
        public HashSet<string> ViewIds { get; private set; }
    }
}