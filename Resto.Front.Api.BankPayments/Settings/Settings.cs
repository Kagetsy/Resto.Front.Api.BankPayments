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

        private SettingsKasikorn kasikornField;

        private SettingsBangkok bangkokField;

        /// <remarks/>
        public SettingsKasikorn Kasikorn
        {
            get
            {
                return this.kasikornField;
            }
            set
            {
                this.kasikornField = value;
            }
        }

        /// <remarks/>
        public SettingsBangkok Bangkok
        {
            get
            {
                return this.bangkokField;
            }
            set
            {
                this.bangkokField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SettingsKasikorn
    {

        private string idField;

        private string secretField;

        private string addressApiField;

        private string merchantIdField;

        private string partnerIdField;

        private string partnerSecretField;

        private string partnerTxnUidField;

        private byte retriesCountField;

        /// <remarks/>
        public string Id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        public string Secret
        {
            get
            {
                return this.secretField;
            }
            set
            {
                this.secretField = value;
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
        public string MerchantId
        {
            get
            {
                return this.merchantIdField;
            }
            set
            {
                this.merchantIdField = value;
            }
        }

        /// <remarks/>
        public string PartnerId
        {
            get
            {
                return this.partnerIdField;
            }
            set
            {
                this.partnerIdField = value;
            }
        }

        /// <remarks/>
        public string PartnerSecret
        {
            get
            {
                return this.partnerSecretField;
            }
            set
            {
                this.partnerSecretField = value;
            }
        }

        /// <remarks/>
        public string PartnerTxnUid
        {
            get
            {
                return this.partnerTxnUidField;
            }
            set
            {
                this.partnerTxnUidField = value;
            }
        }

        /// <remarks/>
        public byte RetriesCount
        {
            get
            {
                return this.retriesCountField;
            }
            set
            {
                this.retriesCountField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SettingsBangkok
    {

        private string idField;

        private string secretField;

        private string addressApiField;

        private ulong billerIdField;

        private string merchantNameField;

        private byte retriesCountField;

        /// <remarks/>
        public string Id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        public string Secret
        {
            get
            {
                return this.secretField;
            }
            set
            {
                this.secretField = value;
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
        public ulong BillerId
        {
            get
            {
                return this.billerIdField;
            }
            set
            {
                this.billerIdField = value;
            }
        }

        /// <remarks/>
        public string MerchantName
        {
            get
            {
                return this.merchantNameField;
            }
            set
            {
                this.merchantNameField = value;
            }
        }

        /// <remarks/>
        public byte RetriesCount
        {
            get
            {
                return this.retriesCountField;
            }
            set
            {
                this.retriesCountField = value;
            }
        }
    }



}
