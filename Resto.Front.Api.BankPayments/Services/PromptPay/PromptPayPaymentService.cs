using QRCoder;
using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.BankPayments.Entities.PromptPay;
using Resto.Front.Api.BankPayments.Helpers;
using Resto.Front.Api.BankPayments.Interfaces.Services.PromptPay;
using Resto.Front.Api.BankPayments.Settings;
using Resto.Front.Api.Data.Cheques;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.Data.Organization;
using Resto.Front.Api.Data.Payments;
using Resto.Front.Api.Data.Security;
using Resto.Front.Api.Extensions;
using Resto.Front.Api.UI;
using System;
using System.Drawing;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using static QRCoder.PayloadGenerator;

namespace Resto.Front.Api.BankPayments.Services.PromptPay
{
    public class PromptPayPaymentService : IPromptPayPaymentService
    {
        public string PaymentSystemKey { get; }
        public string PaymentSystemName { get; }
        private readonly string addressApi;
        private readonly string account;
        private readonly CancellationToken cancellationToken;
        private readonly CancellationTokenSource cancellationSource;
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();
        public PromptPayPaymentService(SettingsPromptPay settingsPromptPay)
        {
            addressApi = settingsPromptPay.AddressApi;
            account = settingsPromptPay.Account;
            cancellationSource = new CancellationTokenSource();
            cancellationToken = cancellationSource.Token;
            PaymentSystemName = "PromptPayBankPayment";
            PaymentSystemKey = "PromptPayBankPayment";
            subscriptions.Add(PluginContext.Operations.RegisterPaymentSystem(this));
            PluginContext.Log.Info($"[{nameof(PromptPayPaymentService)}] was registered.");
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
                PluginContext.Log.Info($"[{nameof(PromptPayPaymentService)}|{nameof(CollectData)}]: Current user {user}");
                var order = PluginContext.Operations.GetOrderById(orderId);
                if (order is null)
                    return;
                var dataQr = string.Empty;
                byte[] urlBytes = Encoding.UTF8.GetBytes($"{addressApi}/{account}/{order.ResultSum}");

                // Convert the byte array to a Base64 string
                string base64String = Convert.ToBase64String(urlBytes);
                Bitmap qrCodeAsBitmap = null;
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    Url generator = new Url($"{addressApi}/{account}/{order.ResultSum}");
                    string payload = generator.ToString();
                    using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q))
                    {
                        QRCode qrCode = new QRCode(qrCodeData);
                        qrCodeAsBitmap = qrCode.GetGraphic(20);
                    }
                }
                var data = new CollectedData
                {
                    QrCode = base64String,
                    User = user,
                };
                
                context.SetRollbackData(data);
                var slip = new ReceiptSlip
                {
                    Doc = new XElement(Tags.Doc,
                        new XElement(Tags.QRCode, $"https://promptpay.io/{account}/{order.ResultSum}"))
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
                PluginContext.Log.Error($"[{nameof(PromptPayPaymentService)}|{nameof(OnPaymentAdded)}] Not found qrCode info for order {order.Id} - {order.Number}");
                operationService.DeletePaymentItem(paymentItem, order, credits);
                viewManager.ShowErrorPopup($"Not found qrCode info for order {order.Number}!");
                return;
            }

            var vm = viewManager.ShowOkCancelPopup("Customer paid?", "Did customer pay bill?");
            if (!vm)
            {
                vm = viewManager.ShowOkCancelPopup("Wait?", "should we wait here?");
                if (!vm)
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
                PluginContext.Log.Error($"[{nameof(PromptPayPaymentService)}|{nameof(Pay)}] Pay Not found qrCode info for order {order.Id} - {order.Number}");
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
                context.SetInfoForReports(data.QrCode, PaymentSystemName);
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
            
        }

        public void ReturnPaymentSilently(decimal sum, Guid? orderId, Guid paymentTypeId, Guid transactionId, [NotNull] IPointOfSale pointOfSale, [NotNull] IUser cashier, IReceiptPrinter printer, IPaymentDataContext context)
        {
        }

        public void ReturnPaymentWithoutOrder(decimal sum, Guid? orderId, Guid paymentTypeId, [NotNull] IPointOfSale pointOfSale, [NotNull] IUser cashier, IReceiptPrinter printer, [CanBeNull] IViewManager viewManager)
        {
        }
    }
}
