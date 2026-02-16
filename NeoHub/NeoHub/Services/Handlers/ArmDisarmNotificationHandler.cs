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

            partition.IsArmed = msg.ArmMode != ArmingMode.Disarm;
            partition.ArmMode = msg.ArmMode switch
            {
                ArmingMode.Disarm => "Disarmed",
                ArmingMode.AwayArm => "Armed Away",
                ArmingMode.StayArm => "Armed Stay",
                ArmingMode.NightArm => "Armed Night",
                ArmingMode.ArmWithNoEntryDelay => "Armed (No Entry Delay)",
                ArmingMode.StayArmWithNoEntryDelay => "Armed Stay (No Entry Delay)",
                ArmingMode.AwayArmWithNoEntryDelay => "Armed Away (No Entry Delay)",
                ArmingMode.NightArmWithNoEntryDelay => "Armed Night (No Entry Delay)",
                _ => $"Armed ({msg.ArmMode})"
            };

            partition.LastUpdated = notification.ReceivedAt;

            _service.UpdatePartition(sessionId, partition);

            return Task.CompletedTask;
        }
    }
}
