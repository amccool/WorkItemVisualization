using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorkItemVisualization
{
    /// <summary>
    /// Represents a file that has changed
    /// </summary>
    public class FileChangeInfo
    {
        /// <summary>
        /// Full server path of the file
        /// <example>"$/BlogEngine.NET/src/Dev/BlogEngine1_5/BlogEngineWeb/login.aspx.cs"</example>
        /// </summary>
        public string FullPath
        { get; set; }

        /// <summary>
        /// Returns just the file. Using the FullPath example, just login.aspx.cs would be returned
        /// </summary>
        public string FileName
        {
            get { return FullPath.Substring(FullPath.LastIndexOf('/') + 1); }
        }

        /// <summary>
        /// This is either Add, Edit, Branch or Delete
        /// </summary>
        public string ChangeType
        { get; set; }
    }
}
