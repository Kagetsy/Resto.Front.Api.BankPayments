using Resto.Front.Api.BankPayments.Entities.Kasikorn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resto.Front.Api.BankPayments.Models.Kasikorn.Responses
{
    public class KasikornVoidQrResponse
    {
        public string errorCode { get; set; }
        public string errorDesc { get; set; }
        public string partnerId { get; set; }
        public string partnerTxnUid { get; set; }
        public StatusCodeEnum statusCode { get; set; }
    }
}