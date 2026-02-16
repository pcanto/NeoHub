using DSC.TLink.ITv2.Transactions;
using DSC.TLink.Serialization;

namespace DSC.TLink.ITv2.Messages
{
    [ITv2Command(Enumerations.ITv2Command.Notification_Text, isAppSequence: true)]
    [SimpleAckTransaction]
    public record NotificationText : IMessageData
    {
        [UnicodeString]
        public string Message { get; init; } = String.Empty;
    }
}
