using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorkItemVisualization
{
    /// <summary>
    /// Represents a work item
    /// </summary>
    public class WorkItemStub
    {
        /// <summary>
        /// Stores a list of all of the other work items that this work item is related to
        /// </summary>
        private List<WorkItemRelationship> _relatedItems = new List<WorkItemRelationship>();

        /// <summary>
        /// Stores a list of all changesets that this work item is related to
        /// </summary>
        private List<ChangesetStub> _changesets = new List<ChangesetStub>();

        private List<TestResultStub> _testResults = new List<TestResultStub>();
        private List<TestPlanInfo> _testPlans = new List<TestPlanInfo>();

        /// <summary>
        /// The ID of the work item
        /// </summary>
        public int ID
        { get; set; }

        /// <summary>
        /// The title of the work item
        /// </summary>
        public string Title
        { get; set; }

        /// <summary>
        /// The type name of the work item (i.e. Bug, Task, User Story, Requirement, Review, etc.)
        /// </summary>
        public string WorkItemTypeName
        { get; set; }

        public string Iteration
        { get; set; }

        /// <summary>
        /// A list of all related work items
        /// </summary>
        public List<WorkItemRelationship> Related
        {
            get { return _relatedItems; }
        }

        /// <summary>
        /// A list of all related changesets
        /// </summary>
        public List<ChangesetStub> Changesets
        {
            get { return _changesets; }
        }

        /// <summary>
        /// This property only applies to test case work items and will be empty in all
        /// other cases
        /// </summary>
        public List<TestResultStub> TestResults
        {
            get { return _testResults; }
        }
    }
}