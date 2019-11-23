using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace SpotifyAPI.Web.Auth
{
    public abstract class Auth
    {
        public string ClientId { get; set; }
        public string ServerUri { get; set; }
        public string RedirectUri { get; set; }
        public string State { get; set; }
        public Scope Scope { get; set; }
        public bool ShowDialog { get; set; }
        public abstract void Start();
        public abstract void Stop(int delay = 2000);
        public abstract void OpenBrowser();

        public delegate void OnAuthReceived(object sender, Token payload);
        public event OnAuthReceived AuthReceived;

        internal void TriggerAuth(Token payload)
        {
            AuthReceived?.Invoke(this, payload);
        }
    }
}
