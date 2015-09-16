using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorkItemVisualization
{
    /// <summary>
    /// Stores the information needed to process the link information
    /// This essentially stores the users options in terms of what they want to see
    /// </summary>
    public class TempLinkType
    {
        /// <summary>
        /// Determines if the checkbox in the list box is enabled
        /// </summary>
        public bool Enabled
        { get; set; }

        /// <summary>
        /// Stores whether or not the item in the checkbox has been selected
        /// </summary>
        public bool Selected
        { get; set; }

        /// <summary>
        /// Stores the name of the link type end
        /// </summary>
        public string LinkType
        { get; set; }

        /// <summary>
        /// Stores whether or not this is the forward end of the relationship
        /// </summary>
        public bool IsForward
        { get; set; }
    }
}
