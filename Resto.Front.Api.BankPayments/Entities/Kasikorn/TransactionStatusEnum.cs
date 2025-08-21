using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resto.Front.Api.BankPayments.Entities.Kasikorn
{
    public enum TransactionStatusEnum
    {
        PAID,
        CANCELLED,
        EXPIRED,
        REQUESTED,
        VOIDED
    }
}
