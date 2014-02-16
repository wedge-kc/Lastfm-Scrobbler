﻿namespace Lastfm.Scrobbler.Models
{
    using System;

    public class LastfmUser
    {
        public string Username { get; set; }

        //We wont store the password, but instead store the session key since its a lifetime key
        public string SessionKey { get; set; }

        public Guid MediaBrowserUserId { get; set; }
    }
}
