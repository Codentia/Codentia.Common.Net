using System;
using System.Collections.Generic;
using System.Text;

namespace Codentia.Common.Net.FTP
{
    /// <summary>
    /// The modes of FTP Transfer available
    /// </summary>
    public enum FTPTransferMode
    {
        /// <summary>
        /// Transfer the file as a binary file
        /// </summary>
        Binary,

        /// <summary>
        /// Transfer the file as a text file
        /// </summary>
        ASCII
    }
}
