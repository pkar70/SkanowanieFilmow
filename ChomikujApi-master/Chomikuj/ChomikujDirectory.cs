using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
//using System.Web.UI;
using Chomikuj.Rest;
//using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;

namespace Chomikuj
{
    public class ChomikujDirectory
    {
        private readonly ChomikujBase _base;

        public string Title { get; internal set; }
        public bool HasAdultContent { get; internal set; }
        public bool IsPasswordProtected { get; internal set; }
        public ChomikujDirectoryInfo Info { get { return GetInfo(); } }

        internal string Link { get; set; }
        internal string FolderId { get; set; }
        internal string ChomikId { get; set; }

        internal ChomikujDirectory(ChomikujBase @base)
        {
            _base = @base;
        }

        public IEnumerable<ChomikujDirectory> GetDirectories()
        {
            // PK: rozbite na dwie linie, by móc operowaæ na Headerach
            var request = new Request(Link);
            //request.Headers.Add("__RequestVerificationToken", _base.RestClient.GetVerificationToken());
            var response = _base.RestClient.Get(request);

            var html = new HtmlDocument();
            html.LoadHtml(response.Content);

            var ret = new List<ChomikujDirectory>();
            var currentDir = FindCurrentDirectory(html.DocumentNode);
            if (currentDir is null) return ret;

            foreach(var node in currentDir.ChildNodes)
            {
                // if (!IsSubDirectory(node)) continue;
                if (node.OuterHtml.Contains("\"T_hid\"")) continue;
                ret.Add(BuildChomikujDirectory(node.SelectSingleNode(".//a")));
            }

            return ret;
//            return currentDir.ChildNodes
//                .Where(node => !IsSubDirectory(node))
////                .Select(node => node.QuerySelector("a"))
//                .Select(node => node.SelectSingleNode(".//a"))
//                .Select(BuildChomikujDirectory);
        }

        public IEnumerable<ChomikujFile> GetFiles()
        {
            var response = _base.RestClient.Get(new Request(Link));
            var html = new HtmlDocument();
            html.LoadHtml(response.Content);
            return IsSinglePage(html) ? GetFilesFromPage(html) : GetFilesFromAllPages();
        }

        // PKAR, nie by³o takiej funkcji
        public IEnumerable<ChomikujFile> GetNewestFiles()
        {
            var response = _base.RestClient.Get(new Request(Link));
            var html = new HtmlDocument();
            html.LoadHtml(response.Content);
            return GetFilesFromPage(html) ;
        }

        public void CreateSubDirectory(NewFolderRequest newFolder)
        {
            var request = BuildNewFolderRequest(newFolder);

            var result = _base.RestClient.Post(request);
            if(result.StatusCode != HttpStatusCode.OK)
                throw new Exception("Adding folder failed");
        }

        public void DeleteSubDirectory(string name)
        {
            var dirs = GetDirectories();
            var selectedDir = dirs.SingleOrDefault(q => q.Title == name);
            if(selectedDir == null)
                throw new Exception("Folder do not exist");

            var request = new Request(ChomikujBase.DeleteFolderUrl);

            request.AddParameter("__RequestVerificationToken", _base.RestClient.GetVerificationToken());
            request.AddParameter("FolderId", selectedDir.FolderId);
            request.AddParameter("ChomikName", ChomikId);

            var result = _base.RestClient.Post(request);
            if (result.StatusCode != HttpStatusCode.OK)
                throw new Exception("Add folder failed");
        }

        public void UploadFile(NewFileRequest request)
        {
            var uploadUrl = new Uri(GetUploadUrl());
            SendAuthorizeRequest(uploadUrl);
            UploadMultiPartFile(uploadUrl, request.FileName, request.ContentType, request.FileStream);
        }

        private void UploadMultiPartFile(Uri uploadUrl, string fileName, string contentType, Stream fileStream)
        {
            var boundary = "----------" + DateTime.Now.Ticks.ToString("x");

            var headers = new Dictionary<string, string>
            {
                {"Content-Type", "multipart/form-data; boundary=" + boundary},
                {"Origin", ChomikujBase.Origin}
            };

            const string boundryTemplate = "--{0}\r\n";
            const string formdataTemplate = "Content-Disposition: form-data; name=\"files[]\"; filename=\"{0}\"\r\n";
            const string fileTypeTemplate = "Content-Type: {0}\r\n\r\n";
            const string trailerTemplate = "\r\n--{0}--\r\n";

            var streams = new[]
            {
                new MemoryStream(string.Format(boundryTemplate, boundary).ToBytes()),
                new MemoryStream(string.Format(formdataTemplate, fileName).ToBytes()),
                new MemoryStream(string.Format(fileTypeTemplate, contentType).ToBytes()),
                fileStream,
                new MemoryStream(string.Format(trailerTemplate, boundary).ToBytes())
            };

            _base.FileHandler.UploadFile(uploadUrl, headers, Helpers.CombineStreams(streams));
        }

        private void SendAuthorizeRequest(Uri uploadUrl)
        {
            var request = new Request(uploadUrl.OriginalString);
            request.Headers["Access-Control-Request-Headers"] = ChomikujBase.AccessControlRequestHeaders;
            request.Headers["Access-Control-Request-Method"] = ChomikujBase.AccessControlRequestMethod;
            request.Headers["Origin"] = ChomikujBase.Origin;

            var response = _base.RestClient.Options(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Upload failed");
            }
        }

        private string GetUploadUrl()
        {
            var request = new Request(ChomikujBase.GetUploadUrl);
            request.AddParameter("__RequestVerificationToken", _base.RestClient.GetVerificationToken());
            request.AddParameter("accountName", ChomikId);
            request.AddParameter("folderid", FolderId);

            var resp = _base.RestClient.Post(request);

            var url = resp.ToJsonDictionary()["Url"];
            var ms = _base.GetCurrentTimeStampInMiliseconds();
            return string.Format("{0}&ms={1}", url, ms);
        }

        private ChomikujDirectory BuildChomikujDirectory(HtmlNode a)
        {
            var ret = new ChomikujDirectory(_base)
            {
                Title = a.Attributes["title"].Value.Trim(),
                Link = a.Attributes["href"].Value,
                FolderId = a.Attributes["rel"].Value,
                ChomikId = ChomikId,
                //HasAdultContent = a.QuerySelectorAll(".adult").Any(),
                //IsPasswordProtected = a.QuerySelectorAll(".pass").Any()
                //HasAdultContent = a.SelectNodes(".//span[@class='adult']").Any(),
                //IsPasswordProtected = a.SelectNodes(".//span[@class='pass']").Any()
            };

            if (a.SelectNodes(".//span[@class='adult']") != null) ret.HasAdultContent = true;
            if (a.SelectNodes(".//span[@class='pass']") != null) ret.IsPasswordProtected= true;
            
            return ret;

        }

        private HtmlNode FindCurrentDirectory(HtmlNode dirsTable)
        {
            // return dirsTable.QuerySelector(string.Format("a[rel=\"{0}\"]", FolderId))

            return dirsTable.SelectSingleNode(string.Format("//a[@rel='{0}']", FolderId))
                ?.ParentNode?.ParentNode?.NextSibling?.SelectSingleNode(".//tbody");
        }

        private bool IsSubDirectory(HtmlNode node)
        {
            return node.Attributes.Contains("id");            
        }

        private Request BuildNewFolderRequest(NewFolderRequest newFolder)
        {
            var request = new Request(ChomikujBase.NewFolderUrl);

            request.AddParameter("Password", newFolder.Password);
            request.AddParameter("NewFolderSetPassword", newFolder.PasswordSecured);
            request.AddParameter("AdultContent", newFolder.AdultContent);
            request.AddParameter("FolderName", newFolder.Name);
            
            request.AddParameter("__RequestVerificationToken", _base.RestClient.GetVerificationToken());
            request.AddParameter("FolderId", FolderId);
            //request.AddParameter("ChomikId", ChomikId);
            request.AddParameter("ChomikName", ChomikId);

            return request;
        }

        private IEnumerable<ChomikujFile> GetFilesFromAllPages(SortType sortType = SortType.Date, OrderBy orderBy = OrderBy.Descending)
        {
            HtmlDocument newhtml;
            IEnumerable<ChomikujFile> files = new List<ChomikujFile>();
            int pageNr = 1;
            do
            {
                newhtml = new HtmlDocument();

                var request = GetPageRequest(pageNr++, sortType, orderBy);
                var pageContent = _base.RestClient.Post(request);
                newhtml.LoadHtml(pageContent.Content);

                files = files.Concat(GetFilesFromPage(newhtml));

            }
            while (!IsLastPage(newhtml));
            return files;
        }

        private bool IsLastPage(HtmlDocument html)
        {
            //return html.DocumentNode.QuerySelectorAll(".right.disabled").Any();
            //return html.DocumentNode.SelectNodes("//.right.disabled").Any();
            return !(html.DocumentNode.SelectSingleNode(".//span[@class='right disabled']") == null);
        }

        private Request GetPageRequest(int pageNr, SortType sortType, OrderBy orderBy)
        {
            var request = new Request(ChomikujBase.GetPageWithFilesUrl);

            request.AddParameter("__RequestVerificationToken", _base.RestClient.GetVerificationToken());
            request.AddParameter("ChomikName", ChomikId);
            request.AddParameter("folderId", FolderId);

            request.AddParameter("fileListSortType", sortType.ToString());
            request.AddParameter("fileListAscending", orderBy == OrderBy.Ascending);

            request.AddParameter("folderChanged", false);
            request.AddParameter("galleryAscending", false);
            request.AddParameter("gallerySortType", false);
            request.AddParameter("isGallery", false);

            request.AddParameter("requestedFolderMode", "");

            request.AddParameter("pageNr", pageNr);

            return request;
        }

        private IEnumerable<ChomikujFile> GetFilesFromPage(HtmlDocument html)
        {
            //var listView = html.DocumentNode.QuerySelector("#listView");
            var listView = html.DocumentNode.SelectSingleNode("//div[@id='listView']");
            if (listView == null)
                return Enumerable.Empty<ChomikujFile>();

            //return listView.QuerySelectorAll(".filerow").Select(BuildFile).ToArray();
            var nodes = listView.SelectNodes(".//div[contains(@class,'filerow')]"); // alt fileItemContainer'] | .//div[@class='filerow fileItemContainer']");
            var ret = new List<ChomikujFile>();
            foreach(var node in nodes)
            {
                ret.Add(BuildFile(node));
            }
            // return listView.SelectNodes(".//@filerow").Select(BuildFile).ToArray();
            return ret;
        }

        private bool IsSinglePage(HtmlDocument html)
        {
            // return html.DocumentNode.QuerySelector(".paginator") == null;
            //return html.DocumentNode.SelectSingleNode(".//@paginator") == null;
            return html.DocumentNode.SelectSingleNode(".//div[contains(@class,'paginator')]") == null;
        }


        private string PozbadzSieSpanClassE(HtmlNode filelink)
        {
            //< a class="expanderHeader downloadAction downloadContext" href="/pkarFotoArch/Prywatne/FotoVideo/Rodzina/Girlsy/Sylwia/551256_409476565759248_100000907249146_1179361_407476415_n,9212388601.jpg" title="551256_409476565759248_100000907249146_1179361_407476415_n" data-analytics-start-location="filesList">
            //  <span class="bold">551256_409476565759248_100000907249146_1179361_407<span class="e"> </span>476415_n</span>.jpg
            //    </a>
            var probaInside = filelink.SelectSingleNode(".//span[@class='e']");
            if (probaInside == null) return filelink.InnerText.Trim();
            probaInside.InnerHtml = "";
            string cos = filelink.InnerText.Trim();
            return cos;
        }

    private ChomikujFile BuildFile(HtmlNode q)
        {
            //var fileLink = q.QuerySelector(".expanderHeader,downloadAction,downloadContext");
            //var fileInfo = q.QuerySelector(".fileinfo").QuerySelectorAll("span").ToArray();
            //var thumbnail = q.QuerySelector(".thumbnail");
            //var description = q.QuerySelector(".filedescription");
            //var fileType = q.QuerySelector(".filename").Attributes["class"].Value.Split(' ')[1];
            //var fileSize = fileInfo[0].InnerText;
            //var additionalInfo = q.QuerySelector(".additionalInfo");
            var fileLink = q.SelectSingleNode(".//a[@class='expanderHeader downloadAction downloadContext']");
            var fileInfo = q.SelectSingleNode(".//div[@class='fileinfo tab']").SelectNodes(".//span").ToArray();
            var thumbnail = q.SelectSingleNode(".//div[@class='thumbnail']");
            var description = q.SelectSingleNode(".//span[@class='filedescription']");
            var fileType = q.SelectSingleNode(".//div[contains(@class, 'filename')]").Attributes["class"].Value.Split(' ')[1];
            var fileSize = fileInfo[0].InnerText;
            var additionalInfo = q.SelectSingleNode(".//@additionalInfo");
            return new ChomikujFile(_base)
            {
                Title = PozbadzSieSpanClassE(fileLink), //.InnerText.Trim(),
                Link = fileLink.Attributes["href"].Value,
                SizeInKb = Helpers.ParseFileSize(fileSize.Split(' ')[0], fileSize.Split(' ')[1]),
                Date = Helpers.ParseChomikujDate(fileInfo[1].InnerText),
                ThumbnailLink = thumbnail == null ? null : new Uri(thumbnail.SelectSingleNode(".//img").Attributes["src"].Value),
                // ThumbnailLink = thumbnail == null ? null : new Uri(thumbnail.QuerySelector("img").Attributes["src"].Value),
                Description = description  == null ? null : description.InnerText.Trim(),
                FileType = Helpers.GetFileType(fileType),
                IsHoarded = additionalInfo != null,
                HoardedFrom = additionalInfo == null ? null : additionalInfo.SelectSingleNode(".//a").InnerText,
                // HoardedFrom = additionalInfo == null ? null : additionalInfo.QuerySelector("a").InnerText,
            };
        }

        private ChomikujDirectoryInfo GetInfo()
        {
            var response = _base.RestClient.Get(new Request(Link));
            var html = new HtmlDocument();
            html.LoadHtml(response.Content);

            //var info = html.DocumentNode.QuerySelector(".fileInfoSmallFrame");
            //var fileTypesCount = info.QuerySelector("ul").QuerySelectorAll("span").ToArray();
            //var folderInfo = info.QuerySelector("p").QuerySelectorAll("span").ToArray();
            var info = html.DocumentNode.SelectSingleNode(".//@fileInfoSmallFrame");
            var fileTypesCount = info.SelectSingleNode(".//ul").SelectNodes(".//span").ToArray();
            var folderInfo = info.SelectSingleNode(".//p").SelectNodes(".//span").ToArray();

            var result = new ChomikujDirectoryInfo
            {
                TextFilesCount = int.Parse(fileTypesCount[0].InnerText),
                ImageFilesCount = int.Parse(fileTypesCount[1].InnerText),
                VideoFilesCount = int.Parse(fileTypesCount[2].InnerText),
                AudioFilesCount = int.Parse(fileTypesCount[3].InnerText),
                AllFilesCount = int.Parse(folderInfo[0].InnerText),
                SizeInKb = Helpers.ParseFileSize(folderInfo[1].InnerText, folderInfo[1].NextSibling.InnerText.Trim())
            };

            return result;
        }

        public void RemoveFile(ChomikujFile uploadedFile)
        {
            var request = new Request(ChomikujBase.DeleteFileUrl);

            request.AddParameter("__RequestVerificationToken", _base.RestClient.GetVerificationToken());
            request.AddParameter("ChomikName", ChomikId);
            request.AddParameter("FolderId", FolderId);
            request.AddParameter("FolderTo", 0);
            request.AddParameter("Files", uploadedFile.FileId);

            var result = _base.RestClient.Post(request);
            if(result.StatusCode != HttpStatusCode.OK)
                throw new Exception("Delete file failed.");

        }
    }
}