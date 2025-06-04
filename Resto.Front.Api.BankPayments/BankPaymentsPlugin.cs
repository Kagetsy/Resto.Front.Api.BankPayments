using Resto.Front.Api.Attributes;
using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.BankPayments.Interfaces.Services;
using Resto.Front.Api.BankPayments.Services;

namespace Resto.Front.Api.BankPayments
{
    [UsedImplicitly, PluginLicenseModuleId(ModuleId)]
    public class BankPaymentsPlugin: IFrontPlugin
    {
        private const int ModuleId = 21016318;
        private readonly IKasikornBankPaymentService kasikornBankPaymentService;
        public BankPaymentsPlugin()
        {
            kasikornBankPaymentService = new KasikornBankPaymentService();
        }

        public void Dispose()
        {
            kasikornBankPaymentService.Dispose();
        }
    }
}
