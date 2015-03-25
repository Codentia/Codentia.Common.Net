using System;
using System.Configuration;

namespace Codentia.Common.Net.BotPlug
{
    /// <summary>
    /// BotPlugPatternConfigurationElement object
    /// </summary>
    public class BotPlugPatternConfigurationElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets the pattern.
        /// </summary>
        /// <value>
        /// The pattern.
        /// </value>
        [ConfigurationProperty("pattern", IsRequired = true)]
        public string Pattern
        {
            get
            {
                return Convert.ToString(this["pattern"]);
            }

            set
            {
                this["pattern"] = value;
            }
        }
    }
}
