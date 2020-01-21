using System;

namespace SpotifyAPI.Web.Auth
{
    public class AuthFailureEventArgs : EventArgs
    {
        public static new AuthFailureEventArgs Empty { get; } = new AuthFailureEventArgs("");

        public string Error { get; }

        public AuthFailureEventArgs(string error)
        {
            Error = error;
        }
    }
}
