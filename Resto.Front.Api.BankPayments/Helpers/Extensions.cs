using System;
using System.Drawing;
using System.IO;
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

        public static string ToBase64String(this byte[] data)
        {
            return Convert.ToBase64String(data);
        }
        public static string BitmapToBase64(this Bitmap bitmap)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                byte[] imageBytes = memoryStream.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
        }
    }
}
