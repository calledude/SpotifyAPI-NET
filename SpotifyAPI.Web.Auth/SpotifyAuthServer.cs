using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using SpotifyAPI.Web.Enums;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;

namespace SpotifyAPI.Web.Auth
{
    public abstract class SpotifyAuthServer<T> : Auth
    {
        private readonly string _folder;
        private readonly string _type;
        private WebServer _server;
        protected CancellationTokenSource _serverSource;

        internal static readonly Dictionary<string, SpotifyAuthServer<T>> Instances = new Dictionary<string, SpotifyAuthServer<T>>();

        internal SpotifyAuthServer(string type, string folder, string redirectUri, string serverUri, Scope scope = Scope.None, string state = "")
        {
            _type = type;
            _folder = folder;
            ServerUri = serverUri;
            RedirectUri = redirectUri;
            Scope = scope;
            State = string.IsNullOrEmpty(state) ? string.Join("", Guid.NewGuid().ToString("n").Take(8)) : state;
        }

        public override void Start()
        {
            Instances.Add(State, this);
            _serverSource = new CancellationTokenSource();

            _server = WebServer.Create(ServerUri);
            _server.RegisterModule(new WebApiModule());
            AdaptWebServer(_server);
            _server.RegisterModule(new ResourceFilesModule(Assembly.GetExecutingAssembly(), $"SpotifyAPI.Web.Auth.Resources.{_folder}"));
#pragma warning disable 4014
            _server.RunAsync(_serverSource.Token);
#pragma warning restore 4014
        }

        public virtual string GetUri()
        {
            StringBuilder builder = new StringBuilder("https://accounts.spotify.com/authorize/?");
            builder.Append("client_id=" + ClientId);
            builder.Append($"&response_type={_type}");
            builder.Append("&redirect_uri=" + RedirectUri);
            builder.Append("&state=" + State);
            builder.Append("&scope=" + Scope.GetStringAttribute(" "));
            builder.Append("&show_dialog=" + ShowDialog);
            return Uri.EscapeUriString(builder.ToString());
        }

        public override void Stop(int delay = 2000)
        {
            if (_serverSource == null) return;
            _serverSource.CancelAfter(delay);
            Instances.Remove(State);
        }

        public override void OpenBrowser()
        {
            string uri = GetUri();
            AuthUtil.OpenBrowser(uri);
        }
        
        internal static SpotifyAuthServer<T> GetByState(string state)
        {
            return Instances.TryGetValue(state, out SpotifyAuthServer<T> auth) ? auth : null;
        }

        protected abstract void AdaptWebServer(WebServer webServer);
    }
}