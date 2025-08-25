using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resto.Front.Api.BankPayments
{
    public class Constants
    {
        public const string PAYLOAD_FORMAT_INDICATOR = "01";
        public const string STATIC_QR_CODE = "11";
        public const string DYNAMIC_QR_CODE = "12";
        public const string DEFAULT_CURRENCY_CODE = "764";
        public const string DEFAULT_COUNTRY_CODE = "TH";
        public const string DEFAULT_COUNTRY_CODE_TEL = "66";
        public const int CREDIT_TRANSFER_DATA_FIELD_ID = 29;
        protected static string CREDIT_TRANSFER_ACQUIRER_ID = "A000000677010111";
        public const int BILL_PAYMENT_DATA_FIELD_ID = 30;
        public const string BILL_PAYMENT_DATA_ACQUIRER_ID = "A000000677010112";
        public const decimal oneHundred = 100.00m;
    }
}
