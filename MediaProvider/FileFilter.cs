// <copyright file="FileFilter.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace ZipPicViewUWP.MediaProvider
{
    /// <summary>
    /// File filter class. The filter is used to filter out unsupported file format. It can also find a cover image file from the list of file names.
    /// </summary>
    public abstract class FileFilter
    {
        /// <summary>
        /// Return whether or not the input file name is supported by the program.
        /// </summary>
        /// <param name="filename">Filename.</param>
        /// <returns><c>true</c> when the filename is an image file, <c>false</c> otherwise.</returns>
        public abstract bool IsImageFile(string filename);

        /// <summary>
        /// File a cover image from the list of file names.
        /// </summary>
        /// <param name="fileNames">Input file names.</param>
        /// <returns>The cover image file name.</returns>
        public abstract string FindCoverPage(IEnumerable<string> fileNames);
    }
}