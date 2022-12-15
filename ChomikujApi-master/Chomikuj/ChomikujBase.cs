using System;
using Chomikuj.Extensions;
using Chomikuj.Rest;

namespace Chomikuj
{
    public abstract class ChomikujBase
    {
        internal const string MainAddress = "http://chomikuj.pl/";
        internal const string LoginUrl = "action/Login/TopBarLogin";
        internal const string NewFolderUrl = "action/FolderOptions/NewFolderAction";
        internal const string DeleteFolderUrl = "action/FolderOptions/DeleteFolderAction";
        internal const string GetPageWithFilesUrl = "action/Files/FilesList";
        internal const string GetUrlToFileUrl = "action/License/DownloadContext";
        internal const string GetUrlToFileByWarningMessageUrl = "action/License/DownloadWarningAccept";
        internal const string GetUploadUrl = "/action/Upload/GetUrl/";
        internal const string DeleteFileUrl = "/action/FileDetails/DeleteFilesAction";

        internal const string AccessControlRequestHeaders = "accept, content-type";
        internal const string AccessControlRequestMethod = "POST";
        internal const string Origin = "http://chomikuj.pl";


        private IRestHandler _restClient;
        private IFileHandler _fileHandler;

        public IRestHandler RestClient
        {
            get { return _restClient ?? (_restClient = new RestSharpRestHandler(new Uri(MainAddress))); }
            set { _restClient = value; }
        }
        public IFileHandler FileHandler
        {
            get
            {
                return _fileHandler ?? (_fileHandler = new WebClientFileHandler());
            }
            set { _fileHandler = value; }
        }

        public long GetCurrentTimeStampInMiliseconds()
        {
            var jan1St1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ms = (long)((DateTime.UtcNow - jan1St1970).TotalMilliseconds);
            return ms;
        }
    }
}
