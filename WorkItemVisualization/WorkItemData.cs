using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace WorkItemVisualization
{
    public class WorkItemData
    {
        public bool Selected
        { get; set; }

        public int ID
        { get; set; }

        public int ParentID
        { get; set; }

        public string Title
        {
            get
            {
                if (ParentID > 0)
                    return "     " + WI.Title;
                else
                    return WI.Title;
            }
        }

        public WorkItem WI
        { get; set; }
    }
}