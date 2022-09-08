// <copyright file="PdfFileFIlter.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP.MediaProvider
{
    /// <summary>
    /// File filter to be used with <c>PdfMediaProvider</c>.
    /// <seealso cref="PdfMediaProvider"/>
    /// </summary>
    public class PdfFileFilter : FileFilter
    {
        /// <inheritdoc/>
        public override string FindCoverPage(string[] fileNames)
        {
            return fileNames.Length <= 0 ? null : fileNames[0];
        }

        /// <inheritdoc/>
        public override bool IsImageFile(string filename)
        {
            return true;
        }
    }
}