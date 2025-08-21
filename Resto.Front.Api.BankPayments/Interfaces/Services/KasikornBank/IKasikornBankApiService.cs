using Resto.Front.Api.BankPayments.Models.Kasikorn.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resto.Front.Api.BankPayments.Interfaces.Services.KasikornBank
{
    public interface IKasikornBankApiService : IDisposable
    {
        Task<KasikornGenerateQrResponse> GenerateQRCode(decimal amount, CancellationToken cancellationToken);
        Task<KasikornStatusQrResponse> GetStatusQrCode(string origPartnerTxnUid, CancellationToken cancellationToken);
        Task<KasikornCancelQrResponse> CancelQrCode(string origPartnerTxnUid, CancellationToken cancellationToken);
        Task<KasikornVoidQrResponse> VoidQrCode(string origPartnerTxnUid, CancellationToken cancellationToken);
    }
}
