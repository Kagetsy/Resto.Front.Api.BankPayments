using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.BankPayments.Entities.Kasikorn;
using Resto.Front.Api.BankPayments.Interfaces.Services.KasikornBank;
using Resto.Front.Api.BankPayments.Models.Kasikorn.Responses;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.UI;
using System.Threading;

namespace Resto.Front.Api.BankPayments.Helpers.KasikornBank
{
    public static class KasikornPaymentHelper
    {
        public static KasikornStatusQrResponse GetStatusQr(IKasikornBankApiService kasikornBankApiService, CancellationToken cancellationToken, CollectedData data, [NotNull] IOrder order, [NotNull] IViewManager viewManager, TransactionStatusEnum transactionStatus)
        {
            KasikornStatusQrResponse resultStatus = null;
            var retriesCount = 0;
            PluginContext.Log.Info($"[{nameof(KasikornPaymentHelper)}.{nameof(GetStatusQr)}] Start get status for payment {order.Id} {order.Number} - {data.origPartnerTxnUid}");
            while (true)
            {
                if (retriesCount > 5)
                {
                    PluginContext.Log.Info($"[{nameof(KasikornPaymentHelper)}.{nameof(GetStatusQr)}] End attempts for get status code {order.Id} {order.Number} - {data.origPartnerTxnUid}. Waiting answer cashier");
                    var userAnswer = viewManager.ShowOkCancelPopup("Need get data for payment?", $"Need get data for payment {order.Number}!", "Retry", "Cancel payment");
                    PluginContext.Log.Info($"[{nameof(KasikornPaymentHelper)}.{nameof(GetStatusQr)}] User answer for status {userAnswer}");
                    if (!userAnswer)
                    {
                        return resultStatus;
                    }
                }
                Thread.Sleep(10000);
                var statusQrResponse = kasikornBankApiService.GetStatusQrCode(data.origPartnerTxnUid, cancellationToken);
                resultStatus = statusQrResponse.Result;
                if (resultStatus.statusCode == StatusCodeEnum.Success && resultStatus.txnStatus == transactionStatus)
                    break;
                else if (resultStatus.statusCode == StatusCodeEnum.Error)
                {
                    PluginContext.Log.Error($"[{nameof(KasikornPaymentHelper)}.{nameof(GetStatusQr)}] Error ger data for qr {order.Number} {resultStatus.errorCode} {resultStatus.errorDesc} status {resultStatus.txnStatus}");
                    
                    retriesCount++;
                    continue;
                }
                retriesCount++;
            }
            return resultStatus;
        }

        public static bool ReturnPaymentQr(IKasikornBankApiService kasikornBankApiService, CancellationToken cancellationToken, CollectedData data, [NotNull] IOrder order, [NotNull] IViewManager viewManager)
        {
            var retriesCountVoid = 0;
            var paymentVoided = false;
            while (true)
            {
                if (retriesCountVoid > 5)
                {
                    var userAnswer = viewManager.ShowOkCancelPopup("Need void for payment?", $"Need void for payment {order.Number}!", "Retry", "Cancel");
                    if (!userAnswer)
                    {
                        viewManager.ShowErrorPopup("Please contact to support");
                        return false;
                    }
                }
                if (!paymentVoided)
                {
                    var voidQrResponse = kasikornBankApiService.VoidQrCode(data.origPartnerTxnUid, cancellationToken);
                    var voidResult = voidQrResponse.Result;
                    if (voidResult.statusCode == StatusCodeEnum.Error)
                    {
                        var userAnswer = viewManager.ShowOkCancelPopup("Error void for payment", $"Error void for payment {order.Number}!", "Retry", "Cancel payment");
                        if (userAnswer)
                        {
                            retriesCountVoid++;
                        }
                        else
                        {
                            viewManager.ShowErrorPopup("Please contact to support");
                            return false;
                        }
                    }
                }
                else
                {
                    var statusQrResponse = kasikornBankApiService.GetStatusQrCode(data.origPartnerTxnUid, cancellationToken);
                    var resultStatus = statusQrResponse.Result;
                    if (resultStatus.statusCode == StatusCodeEnum.Success && resultStatus.txnStatus == TransactionStatusEnum.VOIDED)
                        break;
                }
            }
            return true;
        }
    }
}
