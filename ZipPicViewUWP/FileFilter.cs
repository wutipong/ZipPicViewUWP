namespace ZipPicViewUWP
{
    public abstract class FileFilter
    {
        public abstract bool IsImageFile(string filename);

        public abstract string FindCoverPage(string[] filenames);
    }
}
