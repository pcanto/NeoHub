using DSC.TLink.ITv2.Enumerations;
using DSC.TLink.ITv2.MediatR;
using DSC.TLink.ITv2.Messages;
using MediatR;

namespace NeoHub.Services
{
    public class PanelCommandService : IPanelCommandService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PanelCommandService> _logger;

        public PanelCommandService(IMediator mediator, ILogger<PanelCommandService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<PanelCommandResult> ArmAsync(string sessionId, byte partition, ArmingMode mode, string? accessCode = null)
        {
            _logger.LogInformation(
                "Arm command: Session={SessionId}, Partition={Partition}, Mode={Mode}",
                sessionId, partition, mode);

            var message = new PartitionArm
            {
                Partition = partition,
                ArmMode = mode,
                AccessCode = accessCode ?? string.Empty
            };

            return await SendCommandAsync(sessionId, message);
        }

        public async Task<PanelCommandResult> DisarmAsync(string sessionId, byte partition, string accessCode)
        {
            _logger.LogInformation(
                "Disarm command: Session={SessionId}, Partition={Partition}",
                sessionId, partition);

            var message = new PartitionDisarm
            {
                Partition = partition,
                AccessCode = accessCode
            };

            return await SendCommandAsync(sessionId, message);
        }

        private async Task<PanelCommandResult> SendCommandAsync(string sessionId, IMessageData message)
        {
            try
            {
                var response = await _mediator.Send(new SessionCommand
                {
                    SessionID = sessionId,
                    MessageData = message
                });

                if (response.Success)
                    return PanelCommandResult.Ok();

                _logger.LogWarning("Command failed: {Error}", response.ErrorMessage);
                return PanelCommandResult.Error(response.ErrorMessage ?? "Unknown error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending command to session {SessionId}", sessionId);
                return PanelCommandResult.Error(ex.Message);
            }
        }
    }
}
