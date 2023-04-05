using System;
using System.Net;
using System.Net.Http;
using Chomikuj.Extensions;
//using RestSharp;

namespace Chomikuj.Rest
{
    public class RestSharpRestHandler : IRestHandler
    {

        private const string _defaultHttpAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4321.0 Safari/537.36 Edg/88.0.702.0";

        private readonly HttpClient _client;
        private readonly Uri _baseUri;
        private static CookieContainer _cookies = new CookieContainer();
        private static HttpClientHandler _httphandler = null;

        public RestSharpRestHandler(Uri uri)
        {
            _baseUri = uri;
#if false
            _client = new RestClient(uri) {CookieContainer = new CookieContainer()};
#else

            if (_client == null)
            {
                _httphandler = new HttpClientHandler() { CookieContainer = _cookies, AllowAutoRedirect=true };
                _client = new HttpClient(_httphandler);
                _client.DefaultRequestHeaders.UserAgent.TryParseAdd(_defaultHttpAgent);
                _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));
            }
#endif
        }

        public Response Get(Request request)
        {
            string reqUri = GetHttpQueryString(request);
            var sharpRequest = GetSharpRequestHeaders(reqUri, request, HttpMethod.Get);
            ReplaceBaseUrlIfNeccesarry(request.Url);
            var sharpResponse = _client.SendAsync(sharpRequest).Result;
            var stringResponse = sharpResponse.Content.ReadAsStringAsync().Result;
            return new Response(sharpResponse.RequestMessage.RequestUri, stringResponse, sharpResponse.StatusCode);
        }

        public Response Post(Request request)
        {
            var sharpRequest = GetSharpRequestHeaders("", request, HttpMethod.Post);
            ReplaceBaseUrlIfNeccesarry(request.Url);
            sharpRequest.Content = new StringContent(GetHttpQueryString(request), null, "application/x-www-form-urlencoded");
            var sharpResponse = _client.SendAsync(sharpRequest).Result;
            var stringResponse = sharpResponse.Content.ReadAsStringAsync().Result;
            return new Response(sharpResponse.RequestMessage.RequestUri, stringResponse, sharpResponse.StatusCode);
        }

        public Response Options(Request request)
        {
            string reqUri = GetHttpQueryString(request);
            var sharpRequest = GetSharpRequestHeaders(reqUri, request, HttpMethod.Get);
            ReplaceBaseUrlIfNeccesarry(request.Url);
            var sharpResponse = _client.SendAsync(sharpRequest).Result;
            var stringResponse = sharpResponse.Content.ReadAsStringAsync().Result;
            return new Response(sharpResponse.RequestMessage.RequestUri, stringResponse, sharpResponse.StatusCode);
        }

        public CookieCollection GetCookies(Uri url)
        {
            return _cookies.GetCookies(url);
        }

        private string GetHttpQueryString(Request request)
        {
            string queryParam = "";

            foreach (var parameter in request.StringParameters)
            {
                queryParam = queryParam + "&" + parameter.Key + "=" + parameter.Value;
            }
            foreach (var parameter in request.IntParameters)
            {
                queryParam = queryParam + "&" + parameter.Key + "=" + parameter.Value;
            }
            foreach (var parameter in request.BoolParameters)
            {
                queryParam = queryParam + "&" + parameter.Key + "=" + parameter.Value;
            }

            if (queryParam == "") return queryParam;
            // obcinamy pierwsze &
            return queryParam.Substring(1);
            //var encodedItems = formData.Select(i => $"{WebUtility.UrlEncode(i.Key)}={WebUtility.UrlEncode(i.Value)}" /*.Replace("%20", "+")*/);
            //var encodedContent = new StringContent(string.Join("&", encodedItems), null, "application/x-www-form-urlencoded");
            //var a = new FormUrlEncodedContent()
            //return queryParam;
        }

        private HttpRequestMessage GetSharpRequestHeaders(string uriQuery, Request request, HttpMethod method)
        {

            HttpRequestMessage sharpRequest; 

            if (method == HttpMethod.Get)
            {
                sharpRequest = new HttpRequestMessage(method, request.Url + "?" + uriQuery);
            }
            else
            {
                sharpRequest = new HttpRequestMessage(method, request.Url);
            }

            foreach (var header in request.LongParameters)
            {
                sharpRequest.Headers.Add(header.Key, header.Value.ToString());
            }
            foreach (var header in request.Headers)
            {
                sharpRequest.Headers.Add(header.Key, header.Value);
            }

            //sharpRequest.Headers.Add(header.Key, header.Value);

            return sharpRequest;
        }
        
        private void ReplaceBaseUrlIfNeccesarry(string url)
        {
            Uri result;
            if (!Uri.TryCreate(url, UriKind.Absolute, out result))
            {
                if (_client.BaseAddress!= _baseUri)
                    _client.BaseAddress = _baseUri;
                return;
            }

            if (!_client.BaseAddress.IsBaseOf(result))
            {
                _client.BaseAddress = new Uri(result.Scheme + "://" + result.Host);
            }
        }
    }
}
