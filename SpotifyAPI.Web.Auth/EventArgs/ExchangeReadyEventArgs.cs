using System;

namespace SpotifyAPI.Web.Auth
{
    public class ExchangeReadyEventArgs : EventArgs
    {
        public string ExchangeUri { get; set; }
    }
}
