using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.TestManagement.Client;

namespace WorkItemVisualization
{
    public class TestResultStub
    {
        /// <summary>
        /// ID of the parent test case
        /// </summary>
        public int TestCaseID
        { get; set; }

        /// <summary>
        /// ID of the test result
        /// </summary>
        public int TestResultID
        { get; set; }

        /// <summary>
        /// The run ID
        /// </summary>
        public int TestRunID
        { get; set; }

        /// <summary>
        /// Returns the related test plan info
        /// </summary>
        public TestPlanInfo TestPlan
        { get; set; }

        /// <summary>
        /// Returns true if the test passed
        /// </summary>
        public TestOutcome Outcome
        { get; set; }

        public string Url
        { get; set; }
    }
}