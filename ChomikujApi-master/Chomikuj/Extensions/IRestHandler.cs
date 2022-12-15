using System;
using System.Net;
using Chomikuj.Rest;

namespace Chomikuj.Extensions
{
    public interface IRestHandler
    {
        Response Get(Request request);
        Response Post(Request request);
        Response Options(Request request);
        CookieCollection GetCookies(Uri url);
    }
}