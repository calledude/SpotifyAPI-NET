using System;

namespace SpotifyAPI.Web.Auth
{
    public class AuthSuccessEventArgs : EventArgs
    {
        public static new AuthSuccessEventArgs Empty { get; } = new AuthSuccessEventArgs();
    }
}
