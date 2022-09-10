// <copyright file="PhysicalFileFilter.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP.MediaProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Windows.Graphics.Imaging;

    /// <summary>
    /// A file filter to be used with FileSystemMediaProvider.
    /// <seealso cref="FileSystemMediaProvider"/>
    /// </summary>
    public class PhysicalFileFilter : FileFilter
    {
        private static IEnumerable<string> formats;
        private readonly string[] coverKeywords = { "cover", "top" };

        private static IEnumerable<string> ImageFileExtensions
        {
            get
            {
                if (formats != null)
                {
                    return formats;
                }

                var extensions = new List<string>();
                foreach (var decoderInfo in BitmapDecoder.GetDecoderInformationEnumerator())
                {
                    extensions.AddRange(decoderInfo.FileExtensions);
                }

                formats = extensions;

                return formats;
            }
        }

        /// <inheritdoc/>
        public override string FindCoverPage(IEnumerable<string> fileNames)
        {
            var nameArray = fileNames as string[] ?? fileNames.ToArray();
            if (nameArray.Length == 0)
            {
                return string.Empty;
            }

            foreach (var keyword in this.coverKeywords)
            {
                var name = nameArray.FirstOrDefault(s => s.Contains(keyword, StringComparison.OrdinalIgnoreCase));
                if (name != null)
                {
                    return name;
                }
            }

            return nameArray[0];
        }

        /// <inheritdoc/>
        public override bool IsImageFile(string filename)
        {
            foreach (var format in ImageFileExtensions)
            {
                if (filename.ToLower().EndsWith(format.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }
    }
}