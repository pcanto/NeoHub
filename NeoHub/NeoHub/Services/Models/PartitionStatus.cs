namespace NeoHub.Services.Models
{
    /// <summary>
    /// Represents the current state of a partition.
    /// Values align with Home Assistant alarm_control_panel states
    /// and serialize as snake_case for the WebSocket API.
    /// </summary>
    public enum PartitionStatus
    {
        Unknown,
        Disarmed,
        ArmedAway,
        ArmedHome,
        ArmedNight,
        Arming,
        Pending,
        Triggered
    }
}
