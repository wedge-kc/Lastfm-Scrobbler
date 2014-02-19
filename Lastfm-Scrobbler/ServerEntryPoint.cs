﻿namespace LastfmScrobbler
{
    using Api;
    using Configuration;
    using MediaBrowser.Common.Configuration;
    using MediaBrowser.Common.Net;
    using MediaBrowser.Common.ScheduledTasks;
    using MediaBrowser.Common.Security;
    using MediaBrowser.Controller.Entities.Audio;
    using MediaBrowser.Controller.Library;
    using MediaBrowser.Controller.Plugins;
    using MediaBrowser.Controller.Session;
    using MediaBrowser.Model.Entities;
    using MediaBrowser.Model.Logging;
    using MediaBrowser.Model.Serialization;
    using System.Linq;
    using System.Threading.Tasks;


    /// <summary>
    /// Class ServerEntryPoint
    /// </summary>
    public class ServerEntryPoint : IServerEntryPoint, IRequiresRegistration
    {
        private readonly ISessionManager _sessionManager;
        private readonly LastfmApiClient _apiClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IUserDataManager _userDataManager;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static ServerEntryPoint Instance { get; private set; }

        /// <summary>
        /// The _task manager
        /// </summary>
        private readonly ITaskManager _taskManager;

        /// <summary>
        /// Access to the LibraryManager of MB Server
        /// </summary>
        public ILibraryManager LibraryManager { get; private set; }

        /// <summary>
        /// Access to the SecurityManager of MB Server
        /// </summary>
        public ISecurityManager PluginSecurityManager { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerEntryPoint" /> class.
        /// </summary>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="logManager"></param>
        public ServerEntryPoint(ISessionManager sessionManager, IJsonSerializer jsonSerializer, IHttpClient httpClient, ITaskManager taskManager, ILibraryManager libraryManager, IApplicationPaths appPaths, ILogManager logManager, ISecurityManager securityManager, IUserDataManager userDataManager)
        {
            Plugin.Logger = logManager.GetLogger(Plugin.Instance.Name);

            _taskManager = taskManager;

            _sessionManager = sessionManager;
            _jsonSerializer = jsonSerializer;
            _userDataManager = userDataManager;

            _apiClient = new LastfmApiClient(httpClient, _jsonSerializer);
            
            Instance = this;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            //Bind events
            _sessionManager.PlaybackStart   += this.PlaybackStart;
            _sessionManager.PlaybackStopped += this.PlaybackStopped;

            _userDataManager.UserDataSaved += _userDataManager_UserDataSaved;
        }

        void _userDataManager_UserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            //We only care about audio
            if (!(e.Item is Audio))
                return;

            //We also only care about User rating changes
            if (!e.SaveReason.Equals(UserDataSaveReason.UpdateUserRating))
                return;

            var LastfmUser = Utils.UserHelpers.GetUser(e.UserId);

            if (LastfmUser == null)
            {
                Plugin.Logger.Debug("Could not find user");
                return;
            }

            if (string.IsNullOrWhiteSpace(LastfmUser.SessionKey))
            {
                Plugin.Logger.Info("No session key present, aborting");
                return;
            }

            var item = e.Item as Audio;

            _apiClient.LoveTrack(item, LastfmUser, e.UserData.IsFavorite);
        }

        private async void PlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            //We only care about audio
            if (!(e.Item is Audio))
                return;

            if (!e.PlayedToCompletion)
                return;

            var LastfmUser = Utils.UserHelpers.GetUser(e.Users.First());

            if (LastfmUser == null)
            {
                Plugin.Logger.Debug("Could not find user");
                return;
            }

            //User doesn't want to scrobble
            if (!LastfmUser.Options.Scrobble)
            {
                Plugin.Logger.Debug("{0} ({1}) does not want to scrobble", e.Users.FirstOrDefault().Name, LastfmUser.Username);
                return;
            }

            if (string.IsNullOrWhiteSpace(LastfmUser.SessionKey))
            {
                Plugin.Logger.Info("No session key present, aborting");
                return;
            }

            var item = e.Item as Audio;
            _apiClient.Scrobble(item, LastfmUser);
        }

        private async void PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            //We only care about audio
            if (!(e.Item is Audio))
                return;

            var LastfmUser = Utils.UserHelpers.GetUser(e.Users.First());

            if (LastfmUser == null)
            {
                Plugin.Logger.Debug("Could not find user");
                return;
            }

            //User doesn't want to scrobble
            if (!LastfmUser.Options.Scrobble)
            {
                Plugin.Logger.Debug("{0} ({1}) does not want to scrobble", e.Users.FirstOrDefault().Name, LastfmUser.Username);
                return;
            }

            if (string.IsNullOrWhiteSpace(LastfmUser.SessionKey))
            {
                Plugin.Logger.Info("No session key present, aborting");
                return;
            }

            var item = e.Item as Audio;
            _apiClient.NowPlaying(item, LastfmUser);
        }

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        /// <param name="oldConfig">The old config.</param>
        /// <param name="newConfig">The new config.</param>
        public void OnConfigurationUpdated(PluginConfiguration oldConfig, PluginConfiguration newConfig)
        {
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Loads our registration information
        ///
        /// </summary>
        /// <returns></returns>
        public async Task LoadRegistrationInfoAsync()
        {
            //Plugin.Instance.Registration = await PluginSecurityManager.GetRegistrationStatus("LastfmScrobbler", "[**MB2CompatibleFeature**]").ConfigureAwait(false);
        }
    }
}
