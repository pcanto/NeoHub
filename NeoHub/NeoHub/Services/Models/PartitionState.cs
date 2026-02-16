namespace NeoHub.Services.Models
{
    /// <summary>
    /// State of a single partition (no longer contains zones).
    /// </summary>
    public class PartitionState
    {
        public byte PartitionNumber { get; set; }
        public bool IsReady { get; set; }
        public bool IsArmed { get; set; }
        public string ArmMode { get; set; } = "Unknown";
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Exit delay tracking
        public bool ExitDelayActive { get; set; }
        public DateTime? ExitDelayStartedAt { get; set; }
        public int ExitDelayDurationSeconds { get; set; }
        public bool ExitDelayAudible { get; set; }
        public bool ExitDelayUrgent { get; set; }

        /// <summary>
        /// Returns the remaining exit delay time, or null if no delay is active.
        /// </summary>
        public TimeSpan? ExitDelayRemaining
        {
            get
            {
                if (!ExitDelayActive || !ExitDelayStartedAt.HasValue)
                    return null;

                var elapsed = DateTime.UtcNow - ExitDelayStartedAt.Value;
                var remaining = TimeSpan.FromSeconds(ExitDelayDurationSeconds) - elapsed;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
    }
}