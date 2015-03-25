using System.Configuration;

namespace Codentia.Common.Net.BotPlug
{
    /// <summary>
    /// BotPlug Configuration Format
    /// </summary>
    public class BotPlugConfiguration : ConfigurationSection
    {
        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        [ConfigurationProperty("Settings")]
        public BotPlugConfigurationCollection Settings
        {
            get
            {
                return (BotPlugConfigurationCollection)this["Settings"];
            }

            set
            {
                this["Settings"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the IP allowed.
        /// </summary>
        /// <value>
        /// The IP allowed.
        /// </value>
        [ConfigurationProperty("IPAllowed")]
        public BotPlugPatternConfigurationCollection IPAllowed
        {
            get
            {
                return (BotPlugPatternConfigurationCollection)this["IPAllowed"];
            }

            set
            {
                this["IPAllowed"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the IP denied.
        /// </summary>
        /// <value>
        /// The IP denied.
        /// </value>
        [ConfigurationProperty("IPDenied")]
        public BotPlugPatternConfigurationCollection IPDenied
        {
            get
            {
                return (BotPlugPatternConfigurationCollection)this["IPDenied"];
            }

            set
            {
                this["IPDenied"] = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the URL denied.
        /// </summary>
        /// <value>
        /// The URL denied.
        /// </value>
        [ConfigurationProperty("URLDenied")]
        public BotPlugPatternConfigurationCollection URLDenied
        {
            get
            {
                return (BotPlugPatternConfigurationCollection)this["URLDenied"];
            }

            set
            {
                this["URLDenied"] = value;
            }
        }

        /// <summary>
        /// Gets the config.
        /// </summary>
        /// <returns>DbConnectionConfiguration object</returns>
        public static BotPlugConfiguration GetConfig()
        {
            return (BotPlugConfiguration)ConfigurationManager.GetSection("BotPlug");
        }
    }
}
