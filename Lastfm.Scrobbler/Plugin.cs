﻿using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Lastfm.Scrobbler.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Lastfm.Scrobbler
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        /// <summary>
        /// Used to store the current api session key which is set after authenticating at plugin start
        /// </summary>
        internal string SessionKey { get; set; }
        
        
        internal static readonly SemaphoreSlim LastfmResourcePool = new SemaphoreSlim(4, 4);

        //Global logging instance
        public static ILogger Logger { get; set; }

        public PluginConfiguration PluginConfiguration
        {
            get { return Configuration; }
        }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Lastfm.Scrobbler"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "Lastfm.Scrobbler for your collection.";
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }

        /// <summary>
        /// Holds our registration information
        /// </summary>
        public MBRegistrationRecord Registration { get; set; }

        /// <summary>
        /// Updates the configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public override void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            var oldConfig = Configuration;

            base.UpdateConfiguration(configuration);

            ServerEntryPoint.Instance.OnConfigurationUpdated(oldConfig, (PluginConfiguration)configuration);
        }

    }
}
