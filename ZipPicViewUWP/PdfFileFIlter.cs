namespace ZipPicViewUWP
{
    class PdfFileFIlter : FileFilter
    {
        public override string FindCoverPage(string[] filenames)
        {
            if (filenames.Length <= 0)
            {
                return null;
            }

            return filenames[0];
        }

        public override bool IsImageFile(string filename)
        {
            return true;
        }
    }
}
