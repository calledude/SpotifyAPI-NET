using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using SpotifyAPI.Web.Models;
using System.Threading.Tasks;

namespace SpotifyAPI.Web.Auth.Controllers
{
    public class ImplicitGrantAuthController : WebApiController
    {
        [Route(HttpVerbs.Get, "/auth")]
        public Task GetAuth()
        {
            string state = Request.QueryString["state"];
            SpotifyAuthServer<Token> auth = ImplicitGrantAuth.GetByState(state);
            if (auth == null)
            {
                return HttpContext.SendStandardHtmlAsync(500, (tw) =>
                {
                    tw.WriteLine($"Failed - Unable to find auth request with state \"{state}\" - Please retry");
                    tw.Flush();
                });
            }

            Token token;
            string error = Request.QueryString["error"];
            if (error == null)
            {
                string accessToken = Request.QueryString["access_token"];
                string tokenType = Request.QueryString["token_type"];
                string expiresIn = Request.QueryString["expires_in"];
                token = new Token
                {
                    AccessToken = accessToken,
                    ExpiresIn = double.Parse(expiresIn),
                    TokenType = tokenType
                };
            }
            else
            {
                token = new Token
                {
                    Error = error
                };
            }

            Task.Factory.StartNew(() => auth.TriggerAuth(token));
            return HttpContext.SendStandardHtmlAsync(200, (tw) =>
            {
                tw.WriteLine("<script>window.close()</script>");
                tw.Flush();
            });
        }
    }
}
