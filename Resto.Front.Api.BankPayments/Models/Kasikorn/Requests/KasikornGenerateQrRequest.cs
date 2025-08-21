namespace Resto.Front.Api.BankPayments.Models.Kasikorn.Requests
{
    internal class KasikornGenerateQrRequest
    {
        public string merchantId { get; set; }
        public string partnerId { get; set; }
        public string partnerSecret { get; set; }
        public string partnerTxnUid { get; set; }
        public int qrType { get; set; }
        public string reference1 { get; set; }
        public string reference2 { get; set; }
        public string reference3 { get; set; }
        public string reference4 { get; set; }
        public string requestDt { get; set; }
        public string txnAmount { get; set; }
        public string txnCurrencyCode { get; set; }
    }
}

