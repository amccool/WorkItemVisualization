using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace WorkItemVisualization
{
    public class TreeNode
    {
        List<TreeNode> _subNodes = new List<TreeNode>();

        public ImageSource Image
        { get; set; }

        public string Text
        { get; set; }

        public List<TreeNode> Items
        {
            get
            { return _subNodes; }
            set
            { _subNodes = value; }
        }

        public Object Tag
        { get; set; }
    }
}
