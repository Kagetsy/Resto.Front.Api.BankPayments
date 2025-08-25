using System;

namespace Resto.Front.Api.BankPayments.Entities.PromptPay
{
    [Serializable]
    public class CollectedData
    {
        public string QrCode { get; set; }
        public string User { get; set; }
    }
}
