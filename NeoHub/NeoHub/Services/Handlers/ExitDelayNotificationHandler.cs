using DSC.TLink.ITv2.MediatR;
using DSC.TLink.ITv2.Messages;
using MediatR;
using NeoHub.Services.Models;

namespace NeoHub.Services.Handlers
{
    /// <summary>
    /// Handles exit delay notifications from the panel and updates partition state.
    /// </summary>
    public class ExitDelayNotificationHandler
        : INotificationHandler<SessionNotification<NotificationExitDelay>>
    {
        private readonly IPanelStateService _service;
        private readonly ILogger<ExitDelayNotificationHandler> _logger;

        public ExitDelayNotificationHandler(
            IPanelStateService service,
            ILogger<ExitDelayNotificationHandler> logger)
        {
            _service = service;
            _logger = logger;
        }

        public Task Handle(
            SessionNotification<NotificationExitDelay> notification,
            CancellationToken cancellationToken)
        {
            var msg = notification.MessageData;
            var sessionId = notification.SessionId;

            _logger.LogInformation(
                "Exit delay notification: Partition={Partition}, Duration={Duration}s, Active={Active}, Audible={Audible}, Urgent={Urgent} (Session: {SessionId})",
                msg.Partition, msg.DurationInSeconds, msg.IsActive, msg.AudibleDelay, msg.IsUrgent, sessionId);

            var partition = _service.GetPartition(sessionId, (byte)msg.Partition)
                ?? new PartitionState { PartitionNumber = (byte)msg.Partition };

            if (msg.IsActive && msg.DurationInSeconds > 0)
            {
                // Only reset start time if this is a new delay or the duration changed
                if (!partition.ExitDelayActive || partition.ExitDelayDurationSeconds != msg.DurationInSeconds)
                {
                    partition.ExitDelayStartedAt = DateTime.UtcNow;
                    partition.ExitDelayDurationSeconds = msg.DurationInSeconds;
                }

                partition.ExitDelayActive = true;
                partition.ExitDelayAudible = msg.AudibleDelay;
                partition.ExitDelayUrgent = msg.IsUrgent;
            }
            else
            {
                // Delay expired or cancelled
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
