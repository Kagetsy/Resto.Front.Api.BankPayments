using Resto.Front.Api.BankPayments.Settings;

namespace Resto.Front.Api.BankPayments.Interfaces
{
    public interface ISettings
    {
        SettingsPromptPay PromptPay { get; set; }
    }
}
