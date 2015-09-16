using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorkItemVisualization
{
    public class TestPlanInfo
    {
        private List<TestRunStub> _testRuns = new List<TestRunStub>();

        public int TestPlanID
        { get; set; }

        public string TestPlanName
        { get; set; }

        public List<TestRunStub> TestRuns
        {
            get { return _testRuns; }
        }

        public string Url
        { get; set; }
    }
}
