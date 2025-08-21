using Resto.Front.Api.BankPayments.Entities.Kasikorn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resto.Front.Api.BankPayments.Models.Kasikorn.Responses
{
    public class KasikornStatusQrResponse
    {
        public object errorCode { get; set; }
        public object errorDesc { get; set; }
        public object loyaltyId { get; set; }
        public string merchantId { get; set; }
        public string partnerId { get; set; }
        public string partnerTxnUid { get; set; }
        public string qrType { get; set; }
        public string reference1 { get; set; }
        public string reference2 { get; set; }
        public string reference3 { get; set; }
        public string reference4 { get; set; }
        public StatusCodeEnum statusCode { get; set; }
        public string terminalId { get; set; }
        public string txnAmount { get; set; }
        public string txnCurrencyCode { get; set; }
        public object txnNo { get; set; }
        public TransactionStatusEnum txnStatus { get; set; }
    }
}
