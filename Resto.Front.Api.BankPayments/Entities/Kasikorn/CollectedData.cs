using System;

namespace Resto.Front.Api.BankPayments.Entities.Kasikorn
{

    [Serializable]
    public class CollectedData
    {
        public string origPartnerTxnUid { get; set; }
        public string qrCode { get; set; }
    }
}
