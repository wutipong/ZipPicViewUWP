namespace ZipPicViewUWP.Utility
{
    public static class ImageSizeExtension
    {
        public static (double width, double height) ResizeToHeight(double expectedHeight, double width, double height)
        {
            return ((expectedHeight * width) / height, expectedHeight);
        }

        public static (double width, double height) ResizeToWidth(double expectedWidth, double width, double height)
        {
            return (expectedWidth, (expectedWidth * height) / width);
        }
    }
}