using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Chomikuj.Extensions;
using RestSharp.Extensions;

namespace Chomikuj.Rest
{
    public class WebClientFileHandler : IFileHandler
    {
        private readonly WebClient _downloadWebClient = new WebClient();
        private readonly WebClient _uploadWebClient = new WebClient();

        public Stream DownloadFile(Uri url)
        {
            return new MemoryStream(_downloadWebClient.DownloadData(url));
        }

        public void UploadFile(Uri url, IReadOnlyDictionary<string, string> headers, Stream fileData)
        {
            foreach (var header in headers)
            {
                _uploadWebClient.Headers[header.Key] = header.Value;
            }
            _uploadWebClient.UploadData(url, fileData.ReadAsBytes());
        }
    }
}
