using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resto.Front.Api.BankPayments.Models.Kasikorn.Requests
{
    public class KasikornVoidQrRequest
    {
        public string merchantId { get; set; }
        public string origPartnerTxnUid { get; set; }
        public string partnerId { get; set; }
        public string partnerSecret { get; set; }
        public string partnerTxnUid { get; set; }
        public DateTime requestDt { get; set; }
    }
}