using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Resto.Front.Api.BankPayments.Helpers
{
    public static class XmlSerializerHelper
    {
        public static string Serialize<T>(T data) where T : class
        {
            using (var sw = new StringWriter())
            using (var writer = XmlWriter.Create(sw))
            {
                new XmlSerializer(typeof(T)).Serialize(writer, data);
                return sw.ToString();
            }
        }

        public static T Deserialize<T>(string data) where T : class
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            using (TextReader reader = new StringReader(data))
            {
                return (T)ser.Deserialize(reader);
            }
        }
    }
}
