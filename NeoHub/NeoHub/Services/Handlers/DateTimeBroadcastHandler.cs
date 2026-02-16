using DSC.TLink.ITv2.MediatR;
using DSC.TLink.ITv2.Messages;
using MediatR;

namespace NeoHub.Services.Handlers
{
    /// <summary>
    /// Handles date/time broadcast notifications from the panel and updates session state.
    /// </summary>
    public class DateTimeBroadcastHandler
        : INotificationHandler<SessionNotification<NotificationDateTimeBroadcast>>
    {
        private readonly IPanelStateService _service;
        private readonly ILogger<DateTimeBroadcastHandler> _logger;

        public DateTimeBroadcastHandler(
            IPanelStateService service,
            ILogger<DateTimeBroadcastHandler> logger)
        {
            _service = service;
            _logger = logger;
        }

        public Task Handle(
            SessionNotification<NotificationDateTimeBroadcast> notification,
            CancellationToken cancellationToken)
        {
            var msg = notification.MessageData;
            var sessionId = notification.SessionId;

            _logger.LogDebug("Panel date/time broadcast: {DateTime} (Session: {SessionId})",
                msg.DateTime, sessionId);

            _service.UpdateSession(sessionId, session =>
            {
                session.PanelDateTime = msg.DateTime;
                session.PanelDateTimeSyncedAt = DateTime.UtcNow;
            });

            return Task.CompletedTask;
        }
    }
}
