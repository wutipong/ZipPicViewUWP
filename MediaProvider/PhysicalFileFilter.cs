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
        private static string[] formats = null;
        private readonly string[] coverKeywords = new string[] { "cover", "top" };

        private static string[] ImageFileExtensions
        {
            get
            {
                if (formats == null)
                {
                    List<string> exts = new List<string>();
                    foreach (var decoderInfo in BitmapDecoder.GetDecoderInformationEnumerator())
                    {
                        exts.AddRange(decoderInfo.FileExtensions);
                    }

                    formats = exts.ToArray();
                }

                return formats;
            }
        }

        /// <inheritdoc/>
        public override string FindCoverPage(IEnumerable<string> fileNames)
        {
            if (fileNames.Count() <= 0)
            {
                return null;
            }

            foreach (var keyword in this.coverKeywords)
            {
                var name = fileNames.FirstOrDefault((s) => s.Contains(keyword, StringComparison.OrdinalIgnoreCase));
                if (name != null)
                {
                    return name;
                }
            }

            return fileNames.ElementAt(0);
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