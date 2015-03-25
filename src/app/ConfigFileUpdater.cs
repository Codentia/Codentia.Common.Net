using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Xml;

namespace Codentia.Common.Net
{
    /*
    public class ConfigFileUpdater
    {
        enum ConfigFileChangeType
        {
            MapToNewValue,
            KeepOldValueIfSet,
            Obsolete,
            NewUseDefault
        }

        public ConfigFileUpdater(string configFileName, string changeMapFile)
        {
            // open the existing configuration
            Configuration current = ConfigurationManager.OpenExeConfiguration(configFileName);

            // open the change map
            XmlDocument changeMap = new XmlDocument();
            changeMap.Load(changeMapFile);

            // prepare a structure for new values
            Dictionary<string, string> newAppSettings = new Dictionary<string, string>();

            // now iterate through the appsettings, applying the change rules
            IEnumerator currentSettings = current.AppSettings.Settings.AllKeys.GetEnumerator();

            while (currentSettings.MoveNext())
            {
                XmlNode changeNode = changeMap.SelectSingleNode(string.Format("/configuration/appSettings/add[key='{0}'", currentSettings.Current));
                string currentValue = current.AppSettings.Settings[currentSettings.Current.ToString()].Value;
                ConfigFileChangeType changeType = (ConfigFileChangeType)Enum.Parse(typeof(ConfigFileChangeType), changeNode.Attributes["changeType"].Value, true);

                switch (changeType)
                {
                    case ConfigFileChangeType.KeepOldValueIfSet:
                        // if set, keep old value, otherwise use default
                        break;
                    case ConfigFileChangeType.MapToNewValue:
                        // key which corresponds to a set of lookup values - convert using table provided in change map
                        break;
                    case ConfigFileChangeType.NewUseDefault:
                        // new key, use the default setting
                        newAppSettings.Add(currentSettings.Current.ToString(), changeNode.Attributes["defaultValue"].Value);
                        break;
                    case ConfigFileChangeType.Obsolete:
                        // do nothing - do not carry this key forwards
                        break;
                }
            }

            // now create any new configuration options with their default settings
        }
    }*/
}
