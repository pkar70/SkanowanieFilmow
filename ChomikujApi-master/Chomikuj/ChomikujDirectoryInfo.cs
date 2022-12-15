namespace Chomikuj
{
    public class ChomikujDirectoryInfo
    {
        internal ChomikujDirectoryInfo()
        {
            
        }

        public int TextFilesCount { get; internal set; }
        public int VideoFilesCount { get; internal set; }
        public int ImageFilesCount { get; internal set; }
        public int AudioFilesCount { get; internal set; }
        public int AllFilesCount { get; internal set; }
        public double SizeInKb { get; internal set; }
    }
}