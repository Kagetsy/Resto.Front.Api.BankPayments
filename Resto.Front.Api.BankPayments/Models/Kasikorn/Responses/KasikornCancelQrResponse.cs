using Resto.Front.Api.BankPayments.Entities.Kasikorn;

namespace Resto.Front.Api.BankPayments.Models.Kasikorn.Responses
{
    public class KasikornCancelQrResponse
    {
        public object errorCode { get; set; }
        public object errorDesc { get; set; }
        public string partnerId { get; set; }
        public string partnerTxnUid { get; set; }
        public StatusCodeEnum statusCode { get; set; }
    }
}