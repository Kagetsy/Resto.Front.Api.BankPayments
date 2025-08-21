using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.BankPayments.Entities;
using Resto.Front.Api.BankPayments.Entities.Kasikorn;
using Resto.Front.Api.BankPayments.Helpers.KasikornBank;
using Resto.Front.Api.BankPayments.Interfaces;
using Resto.Front.Api.BankPayments.Interfaces.Services.KasikornBank;
using Resto.Front.Api.Data.Cheques;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.Data.Organization;
using Resto.Front.Api.Data.Payments;
using Resto.Front.Api.Data.Security;
using Resto.Front.Api.Extensions;
using Resto.Front.Api.UI;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Xml.Linq;

namespace Resto.Front.Api.BankPayments.Services.KasikornBank
{
    public class KasikornBankPaymentService : IKasikornBankPaymentService
    {
        public string PaymentSystemKey { get; }
        public string PaymentSystemName { get; }
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();
        private readonly ISettings settings;
        private readonly IKasikornBankApiService kasikornBankApiService;
        private readonly CancellationToken cancellationToken;
        private readonly CancellationTokenSource cancellationSource;
        public KasikornBankPaymentService(ISettings settings)
        {
            this.settings = settings;
            this.kasikornBankApiService = new KasikornBankApiService(settings);
            cancellationSource = new CancellationTokenSource();
            cancellationToken = cancellationSource.Token;
            PaymentSystemName = "KasikornBankPayment";
            PaymentSystemKey = "KasikornBankPayment";
            subscriptions.Add(PluginContext.Operations.RegisterPaymentSystem(this));
            PluginContext.Log.Warn("KasikornBankPaymentService was registered.");
        }

        public void Dispose()
        {
            subscriptions?.Dispose();
        }

        public bool CanPaySilently(decimal sum, Guid? orderId, Guid paymentTypeId, IPaymentDataContext context)
        {
            return false;
        }

        public void CollectData(Guid orderId, Guid paymentTypeId, [NotNull] IUser cashier, IReceiptPrinter printer, IViewManager viewManager, IPaymentDataContext context)
        {
            try
            {
                var user = PluginContext.Operations.GetCurrentUser()?.Name;
                PluginContext.Log.Info($"CollectData: Current user {user}");
                var order = PluginContext.Operations.GetOrderById(orderId);
                if (order is null)
                    return;

                var generateQrResponse = kasikornBankApiService.GenerateQRCode(order.ResultSum, cancellationToken);
                var resultGenerateQrResponse = generateQrResponse.Result;
                var data = new CollectedData
                {
                    origPartnerTxnUid = resultGenerateQrResponse.partnerTxnUid,
                    qrCode = resultGenerateQrResponse.qrCode,
                };
                context.SetRollbackData(data);
                var slip = new ReceiptSlip
                {
                    Doc = new XElement(Tags.Doc,
                        new XElement(Tags.QRCode, data.qrCode))
                };
                printer.Print(slip);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error(ex.ToString());
            }
        }

        public void EmergencyCancelPayment(decimal sum, Guid? orderId, Guid paymentTypeId, Guid transactionId, [NotNull] IPointOfSale pointOfSale, [NotNull] IUser cashier, IReceiptPrinter printer, IViewManager viewManager, IPaymentDataContext context)
        {
            PluginContext.Log.InfoFormat("Cancel {0}", sum);
            ReturnPayment(sum, orderId, paymentTypeId, transactionId, pointOfSale, cashier, printer, viewManager, context);
        }

        public void EmergencyCancelPaymentSilently(decimal sum, Guid? orderId, Guid paymentTypeId, Guid transactionId, [NotNull] IPointOfSale pointOfSale, [NotNull] IUser cashier, IReceiptPrinter printer, IPaymentDataContext context)
        {
        }

        public void OnPaymentAdded([NotNull] IOrder order, [NotNull] IPaymentItem paymentItem, [NotNull] IUser cashier, [NotNull] IOperationService operationService, IReceiptPrinter printer, [NotNull] IViewManager viewManager, IPaymentDataContext context)
        {
            var credits = operationService.GetDefaultCredentials();
            var data = context.GetRollbackData<CollectedData>();
            if (data == null)
            {
                PluginContext.Log.Error($"[{nameof(KasikornBankPaymentService)}|{nameof(OnPaymentAdded)}] Not found qrCode info for order {order.Id} - {order.Number}");
                operationService.DeletePaymentItem(paymentItem, order, credits);
                viewManager.ShowErrorPopup($"Not found qrCode info for order {order.Number}!");
                return;
            }
            var statusResult = KasikornPaymentHelper.GetStatusQr(kasikornBankApiService, cancellationToken, data, order, viewManager, TransactionStatusEnum.PAID);
            if (statusResult.statusCode != StatusCodeEnum.Error)
                return;

            if (statusResult.txnStatus == TransactionStatusEnum.PAID)
            {
                var userResult = viewManager.ShowOkCancelPopup("Need return for payment", $"Need return for payment {order.Number}?", "Ok", "Cancel");
                if (userResult)
                {
                    var returnResult = KasikornPaymentHelper.ReturnPaymentQr(kasikornBankApiService, cancellationToken, data, order, viewManager);
                    if (returnResult)
                        OnPaymentDeleting(order, paymentItem, cashier, operationService, printer, viewManager, context);
                }
            }
            else
            {
                var cancelPayment = kasikornBankApiService.CancelQrCode(data.origPartnerTxnUid, cancellationToken);
                OnPaymentDeleting(order, paymentItem, cashier, operationService, printer, viewManager, context);
            }
        }

        public void OnPaymentDeleting([NotNull] IOrder order, [NotNull] IPaymentItem paymentItem, [NotNull] IUser cashier, [NotNull] IOperationService operationService, IReceiptPrinter printer, [NotNull] IViewManager viewManager, IPaymentDataContext context)
        {
            try
            {
                var credits = operationService.GetDefaultCredentials();
                operationService.DeletePaymentItem(paymentItem, order, credits);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error(ex.ToString());
            }
        }

        public bool OnPreliminaryPaymentEditing([NotNull] IOrder order, [NotNull] IPaymentItem paymentItem, [NotNull] IUser cashier, [NotNull] IOperationService operationService, IReceiptPrinter printer, [NotNull] IViewManager viewManager, IPaymentDataContext context)
        {
            return false;
        }

        public void Pay(decimal sum, [NotNull] IOrder order, [NotNull] IPaymentItem paymentItem, Guid transactionId, [NotNull] IPointOfSale pointOfSale, [NotNull] IUser cashier, [NotNull] IOperationService operationService, IReceiptPrinter printer, [NotNull] IViewManager viewManager, IPaymentDataContext context)
        {
            var data = context.GetRollbackData<CollectedData>();
            if (data == null)
            {
                PluginContext.Log.Error($"[{nameof(KasikornBankPaymentService)}|{nameof(OnPaymentAdded)}] Pay Not found qrCode info for order {order.Id} - {order.Number}");
                viewManager.ShowErrorPopup($"Not found qrCode info for order {order.Number}!");
                return;
            }
            var credentials = operationService.GetDefaultCredentials();
            try
            {
                var user = operationService.GetCurrentUser()?.Name;
                PluginContext.Log.Info($"Pay: Current user {user}");
                viewManager.ChangeProgressBarMessage("Printing slip");

                // Slip to print. Slip consists of XElement children from Resto.CashServer.Agent.Print.Tags.Xml (Resto.Framework.dll)
                var slip = new ReceiptSlip
                {
                    Doc =
                        new XElement(Tags.Doc,
                            new XElement(Tags.Pair,
                                new XAttribute(Data.Cheques.Attributes.Left, "Payment System"),
                                new XAttribute(Data.Cheques.Attributes.Right, PaymentSystemKey),
                                new XAttribute(Data.Cheques.Attributes.Fit, Data.Cheques.Attributes.Right)),
                            new XElement(Tags.Pair,
                                new XAttribute(Data.Cheques.Attributes.Left, "Transaction ID"),
                                new XAttribute(Data.Cheques.Attributes.Right, transactionId.ToString()),
                                new XAttribute(Data.Cheques.Attributes.Fit, Data.Cheques.Attributes.Right)),
                            new XElement(Tags.Pair,
                                new XAttribute(Data.Cheques.Attributes.Left, "Order #"),
                                new XAttribute(Data.Cheques.Attributes.Right, order.Number.ToString()),
                                new XAttribute(Data.Cheques.Attributes.Fit, Data.Cheques.Attributes.Right)),
                            new XElement(Tags.Pair,
                                new XAttribute(Data.Cheques.Attributes.Left, "Full sum"),
                                new XAttribute(Data.Cheques.Attributes.Right, order.FullSum.ToString()),
                                new XAttribute(Data.Cheques.Attributes.Fit, Data.Cheques.Attributes.Right)),
                            new XElement(Tags.Pair,
                                new XAttribute(Data.Cheques.Attributes.Left, "Sum to pay"),
                                new XAttribute(Data.Cheques.Attributes.Right, order.ResultSum.ToString()),
                                new XAttribute(Data.Cheques.Attributes.Fit, Data.Cheques.Attributes.Right)),
                            new XElement(Tags.Pair,
                                new XAttribute(Data.Cheques.Attributes.Left, "Sum to process"),
                                new XAttribute(Data.Cheques.Attributes.Right, sum.ToString()),
                                new XAttribute(Data.Cheques.Attributes.Fit, Data.Cheques.Attributes.Right)))
                };

                printer.Print(slip);
                context.SetInfoForReports(data.origPartnerTxnUid, PaymentSystemName);
                var paymentType = operationService.GetPaymentTypes().Single(i => i.Kind == PaymentTypeKind.Card && i.Name == PaymentSystemName);
                if (paymentType != null)
                {
                    context.SetRollbackData(data);
                }
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error(ex.ToString());
                throw;
            }
        }

        public void PaySilently(decimal sum, [NotNull] IOrder order, [NotNull] IPaymentItem paymentItem, Guid transactionId, [NotNull] IPointOfSale pointOfSale, [NotNull] IUser cashier, IReceiptPrinter printer, IPaymentDataContext context)
        {
        }

        public void ReturnPayment(decimal sum, Guid? orderId, Guid paymentTypeId, Guid transactionId, [NotNull] IPointOfSale pointOfSale, [NotNull] IUser cashier, IReceiptPrinter printer, [NotNull] IViewManager viewManager, IPaymentDataContext context)
        {
            if (orderId is null)
            {
                PluginContext.Log.Error("ReturnPayment: Not found orderId for return payment");
                viewManager.ShowErrorPopup("ReturnPayment: Not found orderId for return payment");
                return;
            }

            var order = PluginContext.Operations.TryGetOrderById(orderId.Value);
            if (order is null)
            {
                PluginContext.Log.Error($"ReturnPayment: ReturnPayment not found order {orderId}");
                viewManager.ShowErrorPopup("ReturnPayment: Not found not found order!");
                return;
            }

            PluginContext.Log.Info($"ReturnPayment: Return payment for order id {orderId}");
            var user = PluginContext.Operations.GetCurrentUser()?.Name;
            PluginContext.Log.Info($"ReturnPayment: return payment start user {user}");
            var data = context.GetRollbackData<CollectedData>();
            if (data is null)
            {
                PluginContext.Log.Error($"ReturnPayment not found QR info for order {orderId}");
                viewManager.ShowErrorPopup("Not found QR info for order!");
                return;
            }
            var slip = new ReceiptSlip
            {
                Doc = new XElement(Tags.Doc,
                    new XElement(Tags.Pair,
                        new XAttribute(Data.Cheques.Attributes.Left, "Return"),
                        new XAttribute(Data.Cheques.Attributes.Right, PaymentSystemKey),
                        new XAttribute(Data.Cheques.Attributes.Fit, Data.Cheques.Attributes.Right)),
                    new XElement(Tags.Pair,
                        new XAttribute(Data.Cheques.Attributes.Left, "Transaction ID"),
                        new XAttribute(Data.Cheques.Attributes.Right, transactionId.ToString()),
                        new XAttribute(Data.Cheques.Attributes.Fit, Data.Cheques.Attributes.Right)))
            };
            printer.Print(slip);
            var returnResult = KasikornPaymentHelper.ReturnPaymentQr(kasikornBankApiService, cancellationToken, data, order, viewManager);
            if (!returnResult)
                PluginContext.Log.Info($"ReturnPayment: Error return payment for orderId {orderId}");
        }

        public void ReturnPaymentSilently(decimal sum, Guid? orderId, Guid paymentTypeId, Guid transactionId, [NotNull] IPointOfSale pointOfSale, [NotNull] IUser cashier, IReceiptPrinter printer, IPaymentDataContext context)
        {
        }

        public void ReturnPaymentWithoutOrder(decimal sum, Guid? orderId, Guid paymentTypeId, [NotNull] IPointOfSale pointOfSale, [NotNull] IUser cashier, IReceiptPrinter printer, [CanBeNull] IViewManager viewManager)
        {
        }
    }
}
