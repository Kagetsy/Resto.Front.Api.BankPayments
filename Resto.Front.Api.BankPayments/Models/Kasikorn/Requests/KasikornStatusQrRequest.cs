namespace Resto.Front.Api.BankPayments.Models.Kasikorn.Requests
{
    public class KasikornStatusQrRequest
    {
        public string partnerTxnUid { get; set; }
        public string origPartnerTxnUid { get; set; }
        public string partnerId { get; set; }
        public string partnerSecret { get; set; }
        public string requestDt { get; set; }
        public string merchantId { get; set; }
        public string terminalId { get; set; }
    }
}

