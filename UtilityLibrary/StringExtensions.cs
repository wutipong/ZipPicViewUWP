using System;

namespace ZipPicViewUWP.Utility
{
    public static class StringExtensions
    {
        public static string ExtractFilename(this string path)
        {
            var index = path.LastIndexOfAny(new[] { '\\', '/' });
            return index >= 0 ? path.Substring(index + 1) : path;
        }

        public static string Ellipses(this string input, int length)
        {
            return input.Length > length ? 
                $"{input.Substring(0, length / 2 - 3)} … {input.Substring(input.Length - length / 2)}" : 
                input;
        }
    }
}