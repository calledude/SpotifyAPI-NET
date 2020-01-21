using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System.Threading.Tasks;

namespace SpotifyAPI.Web.Auth.Controllers
{
    internal class TokenSwapAuthController : WebApiController
    {
        [Route(HttpVerbs.Get, "/auth")]
        public async Task GetAuth()
        {
            string state = Request.QueryString["state"];
            SpotifyAuthServer<AuthorizationCodeResponse> auth = TokenSwapAuth.GetByState(state);

            string code = null;
            string error = Request.QueryString["error"];
            if (error == null)
            {
                code = Request.QueryString["code"];
            }

            AuthorizationCodeResponse authcode = new AuthorizationCodeResponse
            {
                Code = code,
                Error = error
            };

            TokenSwapAuth au = (TokenSwapAuth)auth;

            auth?.TriggerAuth(await au.ExchangeCodeAsync(authcode.Code));

            await HttpContext.SendStandardHtmlAsync(200, (tw) =>
            {
                tw.WriteLine(au.HtmlResponse);
                tw.Flush();
            });
        }
    }
}