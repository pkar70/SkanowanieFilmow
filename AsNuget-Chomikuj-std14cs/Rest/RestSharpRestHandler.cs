using System;
using System.Net;
using Chomikuj.Extensions;
using RestSharp;

namespace Chomikuj.Rest
{
    public class RestSharpRestHandler : IRestHandler
    {
        private readonly RestClient _client;
        private readonly Uri _baseUri;

        public RestSharpRestHandler(Uri uri)
        {
            _baseUri = uri;
            _client = new RestClient(uri) {CookieContainer = new CookieContainer()};
        }

        public Response Get(Request request)
        {
            var sharpRequest = GetSharpRequest(request, Method.GET);
            ReplaceBaseUrlIfNeccesarry(request.Url);
            var sharpResponse = _client.Get(sharpRequest);
            return new Response(sharpResponse.ResponseUri, sharpResponse.Content, sharpResponse.StatusCode);
        }

        public Response Post(Request request)
        {
            var sharpRequest = GetSharpRequest(request, Method.POST);
            ReplaceBaseUrlIfNeccesarry(request.Url);
            var sharpResponse = _client.Post(sharpRequest);
            return new Response(sharpResponse.ResponseUri, sharpResponse.Content, sharpResponse.StatusCode);
        }

        public Response Options(Request request)
        {
            var sharpRequest = GetSharpRequest(request, Method.OPTIONS);
            ReplaceBaseUrlIfNeccesarry(request.Url);
            var sharpResponse = _client.Options(sharpRequest);
            return new Response(sharpResponse.ResponseUri, sharpResponse.Content, sharpResponse.StatusCode);
        }

        public CookieCollection GetCookies(Uri url)
        {
            return _client.CookieContainer.GetCookies(url);
        }

        private RestRequest GetSharpRequest(Request request, Method method)
        {
            var sharpRequest = new RestRequest(request.Url, method);
            foreach (var parameter in request.StringParameters)
            {
                sharpRequest.AddParameter(parameter.Key, parameter.Value);
            }
            foreach (var parameter in request.IntParameters)
            {
                sharpRequest.AddParameter(parameter.Key, parameter.Value);
            }
            foreach (var parameter in request.BoolParameters)
            {
                sharpRequest.AddParameter(parameter.Key, parameter.Value);
            }
            foreach (var header in request.LongParameters)
            {
                sharpRequest.AddParameter(header.Key, header.Value);
            }
            foreach (var header in request.Headers)
            {
                sharpRequest.AddHeader(header.Key, header.Value);
            }
            return sharpRequest;
        }
        
        private void ReplaceBaseUrlIfNeccesarry(string url)
        {
            Uri result;
            if (!Uri.TryCreate(url, UriKind.Absolute, out result))
            {
                if(_client.BaseUrl != _baseUri)
                    _client.BaseUrl = _baseUri;
                return;
            }

            if (!_client.BaseUrl.IsBaseOf(result))
            {
                _client.BaseUrl = new Uri(result.Scheme + "://" + result.Host);
            }
        }
    }
}
