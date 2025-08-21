using Resto.Front.Api.BankPayments.Settings;

namespace Resto.Front.Api.BankPayments.Interfaces
{
    public interface ISettings
    {
        SettingsKasikorn Kasikorn { get; set; }
        SettingsBangkok Bangkok { get; set; }
    }
}
