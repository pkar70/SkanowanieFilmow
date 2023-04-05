using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Chomikuj.Extensions;
//using RestSharp.Extensions;

namespace Chomikuj.Rest
{
    public class WebClientFileHandler : IFileHandler
    {
        //private readonly WebClient _downloadWebClient = new WebClient();
        //private readonly WebClient _uploadWebClient = new WebClient();

        private static readonly HttpClientHandler httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = true };

        private readonly HttpClient _downloadWebClient = new HttpClient(httpClientHandler);
        private readonly HttpClient _uploadWebClient = new HttpClient();

        public Stream DownloadFile(Uri url)
        {
            var byteBuff = _downloadWebClient.GetByteArrayAsync(url).Result;
            return new MemoryStream(byteBuff);
        }

        public void UploadFile(Uri url, IReadOnlyDictionary<string, string> headers, Stream fileData)
        {
            throw new NotImplementedException("to umie tylko download!");
            //foreach (var header in headers)
            //{
            //    _uploadWebClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            //}
            //_uploadWebClient.UploadData(url, fileData.ReadAsBytes());
        }
    }
}
