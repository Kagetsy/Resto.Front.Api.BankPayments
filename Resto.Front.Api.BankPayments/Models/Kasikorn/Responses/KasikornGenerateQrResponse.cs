using Resto.Front.Api.BankPayments.Entities.Kasikorn;

namespace Resto.Front.Api.BankPayments.Models.Kasikorn.Responses
{
    public class KasikornGenerateQrResponse
    {
        public string accountName { get; set; }
        public string errorCode { get; set; }
        public string errorDesc { get; set; }
        public string partnerId { get; set; }
        public string partnerTxnUid { get; set; }
        public string qrCode { get; set; }
        public string[] sof { get; set; }
        public StatusCodeEnum statusCode { get; set; }
    }
}
