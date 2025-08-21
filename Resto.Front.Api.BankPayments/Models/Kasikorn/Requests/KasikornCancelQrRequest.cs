namespace Resto.Front.Api.BankPayments.Models.Kasikorn.Requests
{
    public class KasikornCancelQrRequest
    {
        public string merchantId { get; set; }
        public string origPartnerTxnUid { get; set; }
        public string partnerId { get; set; }
        public string partnerSecret { get; set; }
        public string partnerTxnUid { get; set; }
        public string requestDt { get; set; }
    }
}