using II.RPA.Core.Contracts.Talker;
using II.RPA.Core.Models;

namespace II.RPA.Core.Messages
{
    public class SettingsMessage : IPayload
    {
        public SettingsModel? Settings { get; set; }
    }
}
