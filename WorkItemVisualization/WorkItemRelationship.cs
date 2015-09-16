using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorkItemVisualization
{
    /// <summary>
    /// Represents the relationship between different items
    /// </summary>
    public class WorkItemRelationship
    {
        /// <summary>
        /// The ID of the target in the relationship
        /// </summary>
        public int ID
        { get; set; }

        /// <summary>
        /// The type of relationship the source has with this target. It can be any valid
        /// forward / reverse link description. An example is "Child/Parent".
        /// </summary>
        public TempLinkType2 Relationship
        { get; set; }

        /// <summary>
        /// This is just the end of the relationship such as "Parent"
        /// </summary>
        public string End
        { get; set; }

        public bool IsForward
        {
            get
            {
                return Relationship.Forward == End;
            }
        }

        public override string ToString()
        {
            return ID.ToString() + End;
        }
    }
}
