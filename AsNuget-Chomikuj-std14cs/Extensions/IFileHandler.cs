using System;
using System.Collections.Generic;
using System.IO;

namespace Chomikuj.Extensions
{
    public interface IFileHandler
    {
        Stream DownloadFile(Uri url);
        void UploadFile(Uri url, IReadOnlyDictionary<string, string> headers, Stream fileData);
    }
}