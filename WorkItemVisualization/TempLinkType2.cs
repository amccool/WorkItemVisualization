using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorkItemVisualization
{
    public class TempLinkType2
    {
        public string Forward
        { get; set; }

        public string Reverse
        { get; set; }

        public override string ToString()
        {
            if (Forward == "Related")
                return "Related";
            else
                return string.Format("{0}/{1}", Forward, Reverse);
        }
    }
}