using DSC.TLink.ITv2.Enumerations;
using DSC.TLink.ITv2.MediatR;
using DSC.TLink.ITv2.Messages;
using MediatR;
using NeoHub.Services.Models;

namespace NeoHub.Services.Handlers
{
    /// <summary>
    /// Handles arm/disarm notifications from the panel and updates partition state.
    /// </summary>
    public class ArmDisarmNotificationHandler
        : INotificationHandler<SessionNotification<NotificationArmDisarm>>
    {
        private readonly IPanelStateService _service;
        private readonly ILogger<ArmDisarmNotificationHandler> _logger;

        public ArmDisarmNotificationHandler(
            IPanelStateService service,
            ILogger<ArmDisarmNotificationHandler> logger)
        {
            _service = service;
            _logger = logger;
        }

        public Task Handle(
            SessionNotification<NotificationArmDisarm> notification,
            CancellationToken cancellationToken)
        {
            var msg = notification.MessageData;
            var sessionId = notification.SessionId;

            _logger.LogInformation(
                "Partition {Partition} {ArmMode} via {Method} by user {UserId} (Session: {SessionId})",
                msg.Partition, msg.ArmMode, msg.Method, msg.UserId, sessionId);

            var partition = _service.GetPartition(sessionId, (byte)msg.Partition)
                ?? new PartitionState { PartitionNumber = (byte)msg.Partition };

            partition.Status = msg.ArmMode switch
            {
                ArmingMode.Disarm => PartitionStatus.Disarmed,
                ArmingMode.AwayArm or ArmingMode.AwayArmWithNoEntryDelay => PartitionStatus.ArmedAway,
                ArmingMode.StayArm or ArmingMode.StayArmWithNoEntryDelay => PartitionStatus.ArmedHome,
                ArmingMode.NightArm or ArmingMode.NightArmWithNoEntryDelay => PartitionStatus.ArmedNight,
                ArmingMode.ArmWithNoEntryDelay => PartitionStatus.ArmedAway,
                _ => PartitionStatus.ArmedAway
            };

            // Clear exit delay state when disarming
            if (msg.ArmMode == ArmingMode.Disarm)
            {
                partition.ExitDelayActive = false;
                partition.ExitDelayStartedAt = null;
                partition.ExitDelayDurationSeconds = 0;
                partition.ExitDelayAudible = false;
                partition.ExitDelayUrgent = false;
            }

            partition.LastUpdated = notification.ReceivedAt;

            _service.UpdatePartition(sessionId, partition);

            return Task.CompletedTask;
        }
    }
}
