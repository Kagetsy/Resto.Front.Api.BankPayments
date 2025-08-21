using Resto.Front.Api.Attributes;
using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.BankPayments.Interfaces;
using Resto.Front.Api.BankPayments.Interfaces.Services.KasikornBank;
using Resto.Front.Api.BankPayments.Services.KasikornBank;

namespace Resto.Front.Api.BankPayments
{
    [UsedImplicitly, PluginLicenseModuleId(ModuleId)]
    public class BankPaymentsPlugin: IFrontPlugin
    {
        private const int ModuleId = 21016318;
        private readonly IKasikornBankPaymentService kasikornBankPaymentService;
        private readonly ISettings settings;
        public BankPaymentsPlugin()
        {
            settings = Settings.Settings.Instance();
            kasikornBankPaymentService = new KasikornBankPaymentService(settings);
        }

        public void Dispose()
        {
            kasikornBankPaymentService.Dispose();
        }
    }
}
