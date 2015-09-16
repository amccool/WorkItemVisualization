using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorkItemVisualization
{
    /// <summary>
    /// Represents a single state
    /// </summary>
    public class StateInfo
    {
        public string Id
        { get; set; }

        /// <summary>
        /// The state that was transitioned to
        /// </summary>
        public string ToState
        { get; set; }

        /// <summary>
        /// Date and time of the state
        /// </summary>
        public string Date
        { get; set; }

        /// <summary>
        /// Who moved it to this state
        /// </summary>
        public string By
        { get; set; }

        /// <summary>
        /// Why they moved it to this state
        /// </summary>
        public string Reason
        { get; set; }

        /// <summary>
        /// The state that was transitioned from
        /// </summary>
        public string FromState
        { get; set; }

        /// <summary>
        /// Used to populate the node on the DGML diagram
        /// </summary>
        /// <returns>Four rows of text - ID, Reason, By and Date</returns>
        public string GetCategory()
        {
            return string.Format("{0}\n{1}\n{2}\n{3}", Id, Reason, By, Date);
        }
    }
}
