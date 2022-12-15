using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chomikuj.Rest;
//using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;

namespace Chomikuj
{
    public class ChomikujFile
    {
        private readonly ChomikujBase _base;

        internal ChomikujFile(ChomikujBase @base)
        {
            _base = @base;
        }

        public string Title { get; internal set; }
        public double SizeInKb { get; internal set; }
        public DateTime Date { get; internal set; }
        public Uri ThumbnailLink { get; internal set; }
        public string Description { get; internal set; }
        public ChomikujFileType FileType { get; internal set; }
        public bool IsHoarded { get; internal set; }
        public string HoardedFrom { get; internal set; }

        internal string Link { get; set; }
        internal string FileId
        {
            get
            {
                var rightSide = Link.Split(',')[1];
                return rightSide.Split('.')[0];
            }
        }

        public Stream GetStream()
        {
            return _base.FileHandler.DownloadFile(new Uri(GetUrlToFile()));
        }

        public string GetUrlToFile()
        {
            var request = GetUrlToFileRequest();
            var result = _base.RestClient.Post(request);
            var deserializedResult = result.ToJsonDictionary();

            return WarningPopupIsShown(deserializedResult)
                ? deserializedResult["redirectUrl"]
                : GetUrlToFileFromWarningPopup(deserializedResult);
        }

        private Request GetUrlToFileRequest()
        {
            var request = new Request(ChomikujBase.GetUrlToFileUrl);
            request.AddParameter("fileId", FileId);
            request.AddParameter("__RequestVerificationToken", _base.RestClient.GetVerificationToken());
            return request;
        }

        private bool WarningPopupIsShown(IReadOnlyDictionary<string, string> deserializedResult)
        {
            return deserializedResult.ContainsKey("redirectUrl");
        }

        private string GetUrlToFileFromWarningPopup(IReadOnlyDictionary<string, string> deserializedResult)
        {
            var htmlContent = deserializedResult["Content"];
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(htmlContent);

            var parameters = htmlDocument.DocumentNode
                // .QuerySelectorAll("input[type='hidden']")
                .SelectNodes("//input[@type='hidden']")
                .ToDictionary(
                q => q.Attributes["name"].Value,
                q => q.Attributes["value"].Value);

            parameters.Add("__RequestVerificationToken", _base.RestClient.GetVerificationToken());

            var request = new Request(ChomikujBase.GetUrlToFileByWarningMessageUrl);
            foreach (var parameter in parameters)
            {
                request.AddParameter(parameter.Key, parameter.Value);
            }

            return _base.RestClient.Post(request).ToJsonDictionary()["redirectUrl"];
        }
    }
}