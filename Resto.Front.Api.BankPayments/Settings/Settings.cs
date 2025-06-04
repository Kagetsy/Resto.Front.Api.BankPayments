using Resto.Front.Api.BankPayments.Helpers;
using Resto.Front.Api.BankPayments.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
    public partial class Settings : ISettings
    { 
    }
}
