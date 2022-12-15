using System.IO;

namespace Chomikuj
{
    public class NewFileRequest
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public Stream FileStream { get; set; }
    }
}
