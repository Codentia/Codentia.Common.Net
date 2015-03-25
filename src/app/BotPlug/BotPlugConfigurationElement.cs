using System;
using System.Configuration;

namespace Codentia.Common.Net.BotPlug
{
    /// <summary>
    /// BotPlugConfigurationElement object
    /// </summary>
    public class BotPlugConfigurationElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        [ConfigurationProperty("key", IsRequired = true)]
        public string Key
        {
            get
            {
                return Convert.ToString(this["key"]);
            }

            set
            {
                this["key"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        [ConfigurationProperty("value", IsRequired = true)]
        public string Value
        {
            get
            {
                return Convert.ToString(this["value"]);
            }

            set
            {
                this["value"] = value;
            }
        }
    }
}
