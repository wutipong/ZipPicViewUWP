// <copyright file="PdfFileFIlter.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;

namespace ZipPicViewUWP.MediaProvider
{
    /// <summary>
    /// File filter to be used with <c>PdfMediaProvider</c>.
    /// <seealso cref="PdfMediaProvider"/>
    /// </summary>
    public class PdfFileFilter : FileFilter
    {
        /// <inheritdoc/>
        public override string FindCoverPage(IEnumerable<string> fileNames)
        {
            return !fileNames.Any() ? null : fileNames.ElementAt(0);
        }

        /// <inheritdoc/>
        public override bool IsImageFile(string filename)
        {
            return true;
        }
    }
}