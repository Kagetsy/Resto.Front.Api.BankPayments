using Resto.Front.Api.BankPayments.Helpers;
using Resto.Front.Api.BankPayments.Interfaces;
using System.IO;
using System.Reflection;

namespace Resto.Front.Api.BankPayments.Settings
{
    public partial class Settings
    {
        private static ISettings instance;
        private Settings() { }
        /// <summary>
        /// instance for work with settings
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static ISettings Instance()
        {
            if (instance == null)
            {
                var settingsFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(typeof(Settings)).Location), "Settings", "Settings.xml");
                if (File.Exists(settingsFilePath))
                {
                    var settingsXml = File.ReadAllText(settingsFilePath);
                    PluginContext.Log.Info(settingsXml);
                    instance = XmlSerializerHelper.Deserialize<Settings>(settingsXml);
                }
                else
                {
                    PluginContext.Log.Error($"File settings not found in path {settingsFilePath}");
                    throw new FileNotFoundException(settingsFilePath);
                }
            }
            return instance;
        }
    }

    // Примечание. Для запуска созданного кода может потребоваться NET Framework версии 4.5 или более поздней версии и .NET Core или Standard версии 2.0 или более поздней.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class Settings : ISettings
    {

        private SettingsPromptPay promptPayField;

        /// <remarks/>
        public SettingsPromptPay PromptPay
        {
            get
            {
                return this.promptPayField;
            }
            set
            {
                this.promptPayField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SettingsPromptPay
    {

        private string accountField;

        private string addressApiField;

        private string paymentSystemNameField;

        /// <remarks/>
        public string Account
        {
            get
            {
                return this.accountField;
            }
            set
            {
                this.accountField = value;
            }
        }

        /// <remarks/>
        public string AddressApi
        {
            get
            {
                return this.addressApiField;
            }
            set
            {
                this.addressApiField = value;
            }
        }

        /// <remarks/>
        public string PaymentSystemName
        {
            get
            {
                return this.paymentSystemNameField;
            }
            set
            {
                this.paymentSystemNameField = value;
            }
        }
    }


}
