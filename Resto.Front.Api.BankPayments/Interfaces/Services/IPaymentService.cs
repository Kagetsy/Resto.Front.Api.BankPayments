using System;

namespace Resto.Front.Api.BankPayments.Interfaces.Services
{
    public interface IPaymentService : IPaymentProcessor, IDisposable
    {
    }
}
