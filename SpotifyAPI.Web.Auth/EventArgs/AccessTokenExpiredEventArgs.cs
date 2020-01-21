using System;

namespace SpotifyAPI.Web.Auth
{
    // By defining empty EventArgs objects, you can specify additional information later on as you see fit and it won't
    // be considered a breaking change to consumers of this API.
    //
    // They don't even need to be constructed for their associated events to be invoked - just pass the static Empty property.
    public class AccessTokenExpiredEventArgs : EventArgs
    {
        public static new AccessTokenExpiredEventArgs Empty { get; } = new AccessTokenExpiredEventArgs();
    }
}
