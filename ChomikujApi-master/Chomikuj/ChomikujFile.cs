using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public bool AddComment(string comment)
        {
            var request = GetUrlToAddComment();
            request.AddParameter("Text", comment);
            var result = _base.RestClient.Post(request);
            var deserializedResult = result.ToJsonDictionary();
            // if (deserializedResult["IsSuccess"])
            return deserializedResult["IsSuccess"] == "true";
        }

        public List<ChomikComment> GetComments()
        {
            // http://chomikuj.pl/action/fileDetails/Index/8515445705?TimeStamp=1671110762435
            // zwraca stronê, któr¹ trzeba by³oby zinterpretowaæ
            var request = GetUrlToGetComments();
            var result = _base.RestClient.Post(request);

            if(result.StatusCode != System.Net.HttpStatusCode.OK) return null;

            var html = new HtmlDocument();
            html.LoadHtml(result.Content);

            var ret = new List<ChomikComment>();

            var coms = html.DocumentNode.SelectNodes("//div[contains(@class, 'messageRow')]");
            if(coms is null) return null;
            foreach(var com in coms)
            {
                var datenode = com.SelectSingleNode(".//span[@class='grayDate']");
                if(datenode is null) continue;
                string sData = datenode.InnerText.Trim();
                string sComment = com.SelectSingleNode(".//p[@class='messageExpander']").InnerText.Trim();
                string sUser = com.SelectSingleNode(".//a").Attributes["title"].Value;
                ret.Add(new ChomikComment(sUser, sData, sComment));
            }

            return ret;
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

        private Request GetUrlToAddComment()
        {
            var request = new Request(ChomikujBase.AddCommentUrl);
            request.AddParameter("fileId", FileId);
            request.AddParameter("__RequestVerificationToken", _base.RestClient.GetVerificationToken());
            return request;
        }

        private Request GetUrlToGetComments()
        {
            var request = new Request(ChomikujBase.GetCommentsUrl + FileId);
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

    public class ChomikComment
    {
        public string User { get; set; }
        public string When { get; set; }
        public string Tekst { get; set; }

        public ChomikComment(string sUser, string sWhen, string sTekst)
        {
            User = sUser; When = sWhen; Tekst = sTekst;
        }
    }

}