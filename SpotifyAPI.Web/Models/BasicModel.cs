using Newtonsoft.Json;
using SpotifyAPI.Web.Enums;
using System;
using System.Net;

namespace SpotifyAPI.Web.Models
{
    public abstract class BasicModel
    {
        internal static event Action<Error> OnError;
        private Error error;

        [JsonProperty("error")]
        public Error Error
        {
            get
            {
                return error;
            }
            set
            {
                if (value != null)
                {
                    OnError?.Invoke(value);
                }
                error = value;
            }
        }

        private ResponseInfo _info;

        internal void AddResponseInfo(ResponseInfo info) => _info = info;

        public string Header(string key) => _info.Headers?.Get(key);

        public WebHeaderCollection Headers() => _info.Headers;

        public HttpStatusCode StatusCode() => _info.StatusCode;
    }
}