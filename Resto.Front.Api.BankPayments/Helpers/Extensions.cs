using System;
using System.Text;

namespace Resto.Front.Api.BankPayments.Helpers
{
    public static class Extensions
    {
        public static string ToBase64String(this string msg)
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            return Convert.ToBase64String(data);
        }
    }
}
