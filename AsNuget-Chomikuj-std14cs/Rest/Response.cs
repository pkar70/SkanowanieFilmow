using System;
using System.Net;

namespace Chomikuj.Rest
{
    public class Response
    {
        public Response(Uri responseUri, string content, HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
            Content = content;
            ResponseUri = responseUri;
        }

        public HttpStatusCode StatusCode { get; private set; }
        public Uri ResponseUri { get; private set; }
        public string Content { get; private set; }
    }
}