// <copyright file="PdfFileFIlter.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    /// <summary>
    /// File filter to be used with <c>PdfMediaProvider</c>.
    /// <seealso cref="PdfMediaProvider"/>
    /// </summary>
    internal class PdfFileFIlter : FileFilter
    {
        /// <inheritdoc/>
        public override string FindCoverPage(string[] filenames)
        {
            if (filenames.Length <= 0)
            {
                return null;
            }

            return filenames[0];
        }

        /// <inheritdoc/>
        public override bool IsImageFile(string filename)
        {
            return true;
        }
    }
}