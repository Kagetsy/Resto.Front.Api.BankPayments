using Resto.Front.Api.Attributes;
using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.BankPayments.Interfaces;
using Resto.Front.Api.BankPayments.Interfaces.Services.PromptPay;
using Resto.Front.Api.BankPayments.Services.PromptPay;

namespace Resto.Front.Api.BankPayments
{
    [UsedImplicitly, PluginLicenseModuleId(ModuleId)]
    public class BankPaymentsPlugin: IFrontPlugin
    {
        private const int ModuleId = 21016318;
        private readonly IPromptPayPaymentService promptPayPaymentService;
        private readonly ISettings settings;
        public BankPaymentsPlugin()
        {
            settings = Settings.Settings.Instance();
            promptPayPaymentService = new PromptPayPaymentService(settings.PromptPay);
        }

        public void Dispose()
        {
            promptPayPaymentService.Dispose();
        }
    }
}
