using EmbedIO.WebApi;
using SpotifyAPI.Web.Auth.Controllers;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace SpotifyAPI.Web.Auth
{
    public class ImplicitGrantAuth : SpotifyAuthServer<Token>
    {
        public ImplicitGrantAuth() : base("token", "ImplicitGrantAuth", null, null)
        {
        }

        public ImplicitGrantAuth(string clientId, string redirectUri, string serverUri, Scope scope = Scope.None, string state = "") :
            base("token", "ImplicitGrantAuth", redirectUri, serverUri, scope, state)
        {
            ClientId = clientId;
        }

        protected override void AdaptModule(WebApiModule webApiModule)
        {
            webApiModule.RegisterController<ImplicitGrantAuthController>();
        }
    }
}
