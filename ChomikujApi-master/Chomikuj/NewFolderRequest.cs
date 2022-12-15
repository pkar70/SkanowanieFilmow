namespace Chomikuj
{
    public class NewFolderRequest
    {
        public string Password { get; set; }
        public string Name { get; set; }
        public bool PasswordSecured { get; set; }
        public bool AdultContent { get; set; }
    }
}