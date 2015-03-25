using System.Configuration;

namespace Codentia.Common.Net.BotPlug
{
    /// <summary>
    /// BotPlugConfigurationCollection Collection
    /// </summary>
    public class BotPlugConfigurationCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Gets or sets a property, attribute, or child element of this configuration element.
        /// </summary>
        /// <param name="index">the index of the element</param>
        /// <returns>The specified property, attribute, or child element</returns>
        public BotPlugConfigurationElement this[int index]
        {
            get
            {
                return (BotPlugConfigurationElement)this.BaseGet(index);
            }

            set
            {
                if (this.Count > index && this.BaseGet(index) != null)
                {
                    this.BaseRemoveAt(index);
                }

                this.BaseAdd(index, value);
            }
        }

        /// <summary>
        /// Gets or sets a property, attribute, or child element of this configuration element.
        /// </summary>
        /// <param name="index">index of the element</param>
        /// <returns>The specified property, attribute, or child element</returns>
        public new BotPlugConfigurationElement this[string index]
        {
            get
            {
                return (BotPlugConfigurationElement)this.BaseGet(index);
            }

            set
            {
                if (this.BaseGet(index) != null)
                {
                    this.BaseRemove(index);
                }

                this.BaseAdd(value);
            }
        }

        /// <summary>
        /// When overridden in a derived class, creates a new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </summary>
        /// <returns>
        /// A new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new BotPlugConfigurationElement();
        }

        /// <summary>
        /// Gets the element key for a specified configuration element when overridden in a derived class.
        /// </summary>
        /// <param name="element">The <see cref="T:System.Configuration.ConfigurationElement"/> to return the key for.</param>
        /// <returns>
        /// An <see cref="T:System.Object"/> that acts as the key for the specified <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((BotPlugConfigurationElement)element).Key;
        }
    }
}
