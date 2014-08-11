namespace d60.EventSorcerer.Config
{
    public class EventSorcererOptions
    {
        public const int DefaultMaxRetries = 10;

        public EventSorcererOptions()
        {
            PurgeExistingViews = false;
            MaxRetries = DefaultMaxRetries;
        }

        /// <summary>
        /// Configures whether to purge all existing views during initialization.
        /// </summary>
        public bool PurgeExistingViews { get; set; }

        /// <summary>
        /// Configures the number of retries when processing commands.
        /// </summary>
        public int MaxRetries { get; set; }
    }
}