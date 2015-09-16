using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorkItemVisualization
{
    /// <summary>
    /// Represents a changeset
    /// </summary>
    public class ChangesetStub
    {
        /// <summary>
        /// Stores the list of all files in the changeset
        /// </summary>
        List<FileChangeInfo> _files = new List<FileChangeInfo>();

        /// <summary>
        /// The changeset number
        /// </summary>
        public int ID
        { get; set; }

        /// <summary>
        /// The list of all files that are part of the changeset
        /// </summary>
        public List<FileChangeInfo> Files
        {
            get
            {
                return _files;
            }
        }
    }
}
