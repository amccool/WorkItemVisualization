using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.TestManagement.Client;

namespace WorkItemVisualization
{
    public class ProcessDGMLData
    {
        private List<WorkItemStub> _workItemStubs;
        private WorkItemStore _wis;
        private VersionControlServer _vcs;
        private ITestManagementService _tms;
        private ITestManagementTeamProject _tmp;
        private List<TestPlanInfo> _testPlans = new List<TestPlanInfo>();
        private List<string> _iterations = new List<string>();
        private List<string> _allFiles = new List<string>();
        private List<WorkItemStub> _analysisStubs;

        #region Options
        private List<TempLinkType> _selectedLinks;
        private bool _hideReverse;
        private bool _groupbyIteration;
        private bool _dependencyAnalysis;
        #endregion

        #region Process Work Items and Changesets
        /// <summary>
        /// Method for processing work items down to the changesets that are related to them
        /// </summary>
        /// <param name="wi">Work Item to process</param>
        /// <param name="outputFile">File to write the dgml to</param>
        /// <param name="vcs">Version Control Server which contains the changesets</param>
        public void ProcessWorkItemRelationships(WorkItem[] wi, 
                                                 string outputFile, 
                                                 bool hideReverse,
                                                 bool groupbyIteration,
                                                 bool dependencyAnalysis,
                                                 List<TempLinkType> selectedLinks,
                                                 VersionControlServer vcs)
        {
            string projectName = wi[0].Project.Name;

            _workItemStubs = new List<WorkItemStub>();
            _wis = wi[0].Store;
            _vcs = vcs;
            _tms = vcs.TeamProjectCollection.GetService<ITestManagementService>();
            _tmp = _tms.GetTeamProject(projectName);
            _selectedLinks = selectedLinks;

            //Store options
            _hideReverse = hideReverse;
            _groupbyIteration = groupbyIteration;
            _dependencyAnalysis = dependencyAnalysis;

            for (int i = 0; i < wi.Length; i++)
            {
                ProcessWorkItemCS(wi[i]);
            }

            WriteChangesetInfo(outputFile, projectName);
        }

        /// <summary>
        /// Determines whether or not the relationship will be hidden if
        /// we are hiding the reverse relationships
        /// </summary>
        /// <param name="link">The name of the link</param>
        /// <returns>True for hide it, false for show it</returns>
        private bool HideLink(string link)
        {
            return GetLinkType(link).IsForward;
        }

        /// <summary>
        /// Write the work item and changeset information to a dgml file
        /// </summary>
        /// <param name="outputFile">The file to write the dgml to</param>
        private void WriteChangesetInfo(string outputFile, string project)
        {
            //Create a new DGML document
            XmlTextWriter xtw = new XmlTextWriter(outputFile, Encoding.UTF8);
            xtw.Formatting = Formatting.Indented;
            xtw.WriteStartDocument();
            xtw.WriteStartElement("DirectedGraph", "http://schemas.microsoft.com/vs/2009/dgml");

            #region Write Nodes
            //Add the nodes section
            xtw.WriteStartElement("Nodes");

            //Add the iteration nodes if we are grouping by iteration
            if (_groupbyIteration)
            {
                for (int i = 0; i < _iterations.Count; i++)
                {
                    XmlHelper.WriteXMLNode(xtw, _iterations[i], _iterations[i], "Iteration", "expanded");
                }
            }

            //Add the individual work item nodes
            for (int i = 0; i < _workItemStubs.Count; i++)
            {
                XmlHelper.WriteXMLNode(xtw, _workItemStubs[i].ID.ToString(), _workItemStubs[i].Title, _workItemStubs[i].WorkItemTypeName, string.Empty, "WorkItem", _workItemStubs[i].ID.ToString());

                if (_workItemStubs[i].Changesets != null)
                {
                    //Add the changesets
                    for (int j = 0; j < _workItemStubs[i].Changesets.Count; j++)
                    {
                        string csID = _workItemStubs[i].Changesets[j].ID.ToString();
                        if (_dependencyAnalysis)
                            XmlHelper.WriteXMLNode(xtw, csID, "Changeset " + csID, "Changeset", string.Empty);
                        else
                            XmlHelper.WriteXMLNode(xtw, csID, "Changeset " + csID, "Changeset", "Collapsed");

                        if (!_dependencyAnalysis)
                        {
                            ////Add the files that are part of the changesets
                            for (int k = 0; k < _workItemStubs[i].Changesets[j].Files.Count; k++)
                            {
                                string fn = _workItemStubs[i].Changesets[j].Files[k].FileName;
                                XmlHelper.WriteXMLNode(xtw, fn + csID, fn, "File", string.Empty);
                            }
                        }
                    }
                }

                //add the test plan and test results nodes
                if (_testPlans != null)
                {
                    //Add the test plan
                    for (int j = 0; j < _testPlans.Count; j++)
                    {
                        //The ID for the purposes of the DGML is "TP" + the test plan ID
                        XmlHelper.WriteXMLNode(xtw, "TP" + _testPlans[j].TestPlanID, "Test Plan: " + _testPlans[j].TestPlanName, "Test Plan", "collapsed");

                        //Add the test runs
                        for (int k = 0; k < _testPlans[j].TestRuns.Count; k++)
                        {
                            XmlHelper.WriteXMLNode(xtw, "TestRun" + _testPlans[j].TestRuns[k].ToString(), "Test Run " + _testPlans[j].TestRuns[k].ToString(), "Test Run", "collapsed");
                        }
                    }

                    //Add the test results
                    for (int j = 0; j < _workItemStubs[i].TestResults.Count; j++)
                    {
                        //The ID for the purposes of the DGML is "TR" + the test result ID
                        string outcome = string.Format("{0}\n{1}", "Test Case: " + _workItemStubs[i].TestResults[j].TestCaseID, Enum.GetName(typeof(TestOutcome), _workItemStubs[i].TestResults[j].Outcome));
                        string trID = "TR" + _workItemStubs[i].TestResults[j].TestRunID.ToString() + _workItemStubs[i].TestResults[j].TestResultID.ToString() + _workItemStubs[i].TestResults[j].TestCaseID.ToString();
                        XmlHelper.WriteXMLNode(xtw, trID, outcome, "Test Result", string.Empty, "TestOutcome", Enum.GetName(typeof(TestOutcome), _workItemStubs[i].TestResults[j].Outcome));
                    }
                }
            }

            //Close the Nodes section
            xtw.WriteEndElement();

            #endregion

            #region Write Links
            //Add the links section
            xtw.WriteStartElement("Links");

            //Loop through the unique list of work items
            for (int i = 0; i < _workItemStubs.Count; i++)
            {
                //Add each related item to the DGML
                for (int j = 0; j < _workItemStubs[i].Related.Count; j++)
                {
                    //If we are hiding the reverse relationships, check to see if this is one and set
                    //the visibility to hidden - we don't want to skip representing it, we just don't
                    //want to show it
                    if (_hideReverse)
                    {
                        XmlHelper.WriteXMLLink(xtw, _workItemStubs[i].ID.ToString(), _workItemStubs[i].Related[j].ID.ToString(), _workItemStubs[i].Related[j].Relationship, string.Empty, HideLink(_workItemStubs[i].Related[j].Relationship));
                    }
                    else
                    {
                        XmlHelper.WriteXMLLink(xtw, _workItemStubs[i].ID.ToString(), _workItemStubs[i].Related[j].ID.ToString(), _workItemStubs[i].Related[j].Relationship, string.Empty);
                    }

                    if (_groupbyIteration)
                    {
                        XmlHelper.WriteXMLLink(xtw, _workItemStubs[i].Iteration, _workItemStubs[i].Related[j].ID.ToString(), "Contains", string.Empty);
                    }
                }

                //Add the changesets and files to the DGML
                if (_workItemStubs[i].Changesets != null)
                {
                    for (int j = 0; j < _workItemStubs[i].Changesets.Count; j++)
                    {
                        //for each changeset, get the ID
                        string csID = _workItemStubs[i].Changesets[j].ID.ToString();
                        for (int k = 0; k < _workItemStubs[i].Changesets[j].Files.Count; k++)
                        {
                            //for each file in the changeset get the file name
                            string fn = _workItemStubs[i].Changesets[j].Files[k].FileName;
                            //Add a link from the work item to the changeset
                            XmlHelper.WriteXMLLink(xtw, _workItemStubs[i].ID.ToString(), _workItemStubs[i].Changesets[j].ID.ToString());
                            //Add a contains clause to say that the given file is contained by the given changeset (temporarily removed the container as a file
                            //can't belong to two containers at once so I am just leaving it at links for right now
                            if (_dependencyAnalysis)
                            {
                                XmlHelper.WriteXMLLink(xtw, _workItemStubs[i].Changesets[j].ID.ToString(), fn, _workItemStubs[i].Changesets[j].Files[k].ChangeType.ToString(), string.Empty);
                            }
                            else
                            {
                                XmlHelper.WriteXMLLink(xtw, _workItemStubs[i].Changesets[j].ID.ToString(), fn + csID, string.Empty, string.Empty);
                                XmlHelper.WriteXMLLink(xtw, _workItemStubs[i].Changesets[j].ID.ToString(), fn + csID, "Contains", string.Empty);
                            }

                            if (_dependencyAnalysis)
                            {
                                _analysisStubs = new List<WorkItemStub>();
                                GetRootWorkItem(_workItemStubs[i]);
                                //At this point _analysisStubs should be filled
                                for (int n = 0; n < _analysisStubs.Count; n++)
                                {
                                    XmlHelper.WriteXMLLink(xtw, _analysisStubs[n].ID.ToString(), fn, "Related File", "CS" + _workItemStubs[i].Changesets[j].ID.ToString());
                                }
                            }
                        }
                    }
                }

                //Add the test plans
                for (int m = 0; m < _testPlans.Count; m++)
                {
                    for (int n = 0; n < _testPlans[m].TestRuns.Count; n++)
                    {
                        XmlHelper.WriteXMLLink(xtw, "TP" + _testPlans[m].TestPlanID.ToString(), "TestRun" + _testPlans[m].TestRuns[n].ToString(), "Contains", string.Empty);
                    }
                }

                //Add the test results
                if (_workItemStubs[i].TestResults != null)
                {
                    for (int j = 0; j < _workItemStubs[i].TestResults.Count; j++)
                    {
                        string trID = "TR" + _workItemStubs[i].TestResults[j].TestRunID.ToString() + _workItemStubs[i].TestResults[j].TestResultID.ToString() + _workItemStubs[i].TestResults[j].TestCaseID.ToString();
                        XmlHelper.WriteXMLLink(xtw, _workItemStubs[i].TestResults[j].TestCaseID.ToString(), trID);
                        XmlHelper.WriteXMLLink(xtw, "TestRun" + _workItemStubs[i].TestResults[j].TestRunID.ToString(), trID, "Contains", string.Empty);
                    }
                }
            }

            //Close the links section
            xtw.WriteEndElement();

            #endregion

            #region properties

            //Write a property node and custom properties
            string server = _wis.TeamProjectCollection.Uri.ToString().Replace("http", "mtm");  //Returns http://[server]:[port]/tfs/[collection] - need to replace http with mtm
            string url = server + string.Format("/p:{0}/testing/", project);

            xtw.WriteStartElement("Properties");
            XmlHelper.WriteXMLProperty(xtw, "TestOutcome", "Test Outcome", "System.String");
            XmlHelper.WriteXMLReference(xtw, "WorkItem", "True", url + "/testcase/open?id={0}");
            xtw.WriteEndElement();

            #endregion

            //Close the DGML document
            xtw.WriteEndElement();
            xtw.Close();

            //Open the DGML in Visual Studio
            System.Diagnostics.Process.Start(outputFile);
        }

        /// <summary>
        /// Traces the path of the work item to it's root
        /// </summary>
        /// <param name="stub">The work item stub to trace</param>
        private void GetRootWorkItem(WorkItemStub stub)
        {
            WorkItemStub s = null;

            for (int i = 0; i < stub.Related.Count; i++)
            {
                s = _workItemStubs.Find(
                    delegate(WorkItemStub wiStub)
                    {
                        return wiStub.ID == stub.Related[i].ID;
                    }
                );

                if ((s.WorkItemTypeName == "User Story") || (s.WorkItemTypeName == "Requirement"))
                    _analysisStubs.Add(s);
                else
                    GetRootWorkItem(s);
            }
        }

        /// <summary>
        /// Gets the stub associated with the work item if it exists, otherwise a new one is
        /// created, added to the collection and returned
        /// </summary>
        /// <param name="wi">The Work Item to check</param>
        /// <returns>The associated WorkItemStub</returns>
        private WorkItemStub CheckStub(WorkItem wi)
        {
            //Perform a find on the ID, if it is found, s is populated
            WorkItemStub s = _workItemStubs.Find(
                    delegate(WorkItemStub stub)
                    {
                        return stub.ID == (int)wi.Fields[CoreField.Id].Value;
                    }
                );

            //If it isn't found, create a new work item stub
            if (s == null)
            {
                s = new WorkItemStub();
                s.ID = (int)wi.Fields[CoreField.Id].Value;
                s.Title = wi.Fields[CoreField.Title].Value.ToString();
                s.WorkItemTypeName = wi.Fields[CoreField.WorkItemType].Value.ToString();
                s.Iteration = wi.Fields[CoreField.IterationPath].Value.ToString();

                //Check to see if the iteration exists in the list of iterations and add it if it doesn't
                if (!_iterations.Contains(s.Iteration))
                    _iterations.Add(s.Iteration);

                _workItemStubs.Add(s);
            }

            return s;
        }

        /// <summary>
        /// Gets a link type from the list of link types
        /// </summary>
        /// <param name="link">The name of the link end</param>
        /// <returns>The link type</returns>
        private TempLinkType GetLinkType(string link)
        {
            TempLinkType result = _selectedLinks.Find(
                delegate(TempLinkType s)
                {
                    return s.LinkType == link;
                }
            );

            return result;
        }

        /// <summary>
        /// Determines if the link for processing is selected
        /// </summary>
        /// <param name="link">The link end name</param>
        /// <returns>True if the link should be processed, otherwise false</returns>
        private bool IsLinkSelected(string link)
        {
            return GetLinkType(link).Selected;
        }

        /// <summary>
        /// The work item to process
        /// </summary>
        /// <param name="wi"></param>
        private void ProcessWorkItemCS(WorkItem wi)
        {
            //Get a reference to or add the wi to the workitemstub list
            WorkItemStub s = CheckStub(wi);

            //Loop through the work items linked to this work item
            for (int i = 0; i < wi.WorkItemLinks.Count; i++)
            {
                if (IsLinkSelected(wi.WorkItemLinks[i].LinkTypeEnd.Name))
                {
                    int targetID = wi.WorkItemLinks[i].TargetId;
                    //Check to see if the work item is in the list of work items and if not, add it by
                    //calling this method recursively. If it is in the list of work items then we've already
                    //seen this work item and we just have to figure out how it is linked to this work item
                    WorkItemStub result = _workItemStubs.Find(
                        delegate(WorkItemStub stub)
                        {
                            return stub.ID == targetID;
                        }
                    );

                    //If the work item hasn't been processed yet, process it
                    if (result == null)
                        ProcessWorkItemCS(_wis.GetWorkItem(targetID));

                    s.Related.Add(new WorkItemRelationship() { ID = targetID, Relationship = wi.WorkItemLinks[i].LinkTypeEnd.Name });
                }
            }

            if (IsLinkSelected("Changesets"))
            {
                GetChangesets(wi, s);
            }

            if (IsLinkSelected("Test Results"))
            {
                //Get the test results if this is a test case
                if (s.WorkItemTypeName == "Test Case")
                {
                    GetTestResults(s, wi.Project.Name);
                }
            }
        }
        #endregion

        /// <summary>
        /// Retrieves changesets associated with a work item
        /// </summary>
        /// <param name="wi">The work item to retrieve changesets for</param>
        /// <param name="stub">The work item stub to add them to</param>
        private void GetChangesets(WorkItem wi, WorkItemStub stub)
        {
            //Loop through all of the links in the work item. Note that this is the links
            //not the work item links so this includes things like hyperlinks, versioned items
            //and changeset links
            for (int i = 0; i < wi.Links.Count; i++)
            {
                //Determine if this is an external link which is an indication of a changeset link
                if (wi.Links[i].BaseType == BaseLinkType.ExternalLink)
                {
                    int cs_id;
                    ExternalLink ex = (ExternalLink)wi.Links[i];

                    //Try to get the changeset ID by parsing the end of the linked artifact URI
                    //For a changeset this will always be a numeric value
                    if (int.TryParse(ex.LinkedArtifactUri.Substring(ex.LinkedArtifactUri.LastIndexOf('/') + 1), out cs_id))
                    {
                        //It is a changeset, validate that we haven't processed it already
                        ChangesetStub c = stub.Changesets.Find(
                               delegate(ChangesetStub s)
                               {
                                   return s.ID == cs_id;
                               }
                           );

                        //It wasn't found, process it
                        if (c == null)
                        {
                            //It is a changeset so get the specific changeset
                            Changeset cs = _vcs.GetChangeset(cs_id);
                            ChangesetStub csStub = new ChangesetStub();
                            csStub.ID = cs_id;

                            //Loop through the files in the changeset which is represented by the Changes
                            for (int j = 0; j < cs.Changes.Count(); j++)
                            {
                                //Add the files to the changeset stub
                                FileChangeInfo fc = new FileChangeInfo();
                                fc.ChangeType = cs.Changes[j].ChangeType.ToString();
                                fc.FullPath = cs.Changes[j].Item.ServerItem;
                                csStub.Files.Add(fc);

                                //Check to see if we have added this file to the master list,
                                //if we haven't, add it
                                if (!_allFiles.Contains(fc.FileName))
                                    _allFiles.Add(fc.FileName);
                            }
                            stub.Changesets.Add(csStub);

                            //Loop through the files in the changeset which is represented by the Changes
                            for (int j = 0; j < cs.Changes.Count(); j++)
                            {
                                ChangesetFilesToWorkItems(cs.Changes[j].Item.ServerItem);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the entire history of a single file
        /// </summary>
        /// <param name="serverpath">The path to the item on the server who's history to retrieve</param>
        private void ChangesetFilesToWorkItems(string serverpath)
        {
            //This syntax retrieves the entire history of a file - but not across branches
            //as that typically won't have as big an impact on changes
            System.Collections.IEnumerable history = _vcs.QueryHistory(serverpath, VersionSpec.Latest,
                0, RecursionType.OneLevel, null, null, null, Int32.MaxValue, true, false);

            //At this point history contains every changeset the given file has been involved in
            //Next we need to get all of the work items associated with that changeset and hence that file
            //and add all items to the drawing surface (eventually).
            foreach (Changeset cs in history)
            {
                WorkItem[] wi = cs.WorkItems;
                for (int i = 0; i < wi.Length; i++)
                {
                    ProcessWorkItemCS(wi[i]);
                }
            }
        }

        /// <summary>
        /// Get the results associated with a test case
        /// </summary>
        /// <param name="stub">The work item to get the results for</param>
        private void GetTestResults(WorkItemStub stub, string project)
        {
            //Return all of the test results in which this test case was executed
            string query = string.Format("SELECT * FROM TestResult WHERE TestCaseId = {0}", stub.ID);
            int planId = 0;

            //Get the list of results for the test case
            foreach (ITestCaseResult tcr in _tmp.TestResults.Query(query))
            {
                #region Get the Test Plan
                string runQuery = string.Format("SELECT * FROM TestRun WHERE TestRunId = {0}", tcr.TestRunId);

                //There should only be one value returned so just get the first one
                foreach (ITestRun tr in _tms.QueryTestRuns(runQuery))
                {
                    planId = tr.TestPlanId;
                    break;
                }

                //Is this part of a test plan that we already have?
                TestPlanInfo plan = _testPlans.Find(
                    delegate(TestPlanInfo tpi)
                    {
                        return tpi.TestPlanID == planId;
                    }
                );

                //If not, add it
                if (plan == null)
                {
                    string planQuery = string.Format("SELECT * FROM TestPlan WHERE PlanId = {0}", planId);
                    foreach (ITestPlan testPlan in _tmp.TestPlans.Query(planQuery))
                    {
                        plan = new TestPlanInfo() { TestPlanID = planId, TestPlanName = testPlan.Name };
                        _testPlans.Add(plan);
                        break;
                    }
                }
                #endregion
                
                //Check to see if we've added the test run already
                bool found = false;
                for (int k = 0; k < plan.TestRuns.Count; k++)
                {
                    if (plan.TestRuns[k].ID == tcr.TestRunId)
                    {
                        found = true;
                    }
                }

                //If not, add it
                if (!found)
                    plan.TestRuns.Add(new TestRunStub() { ID = tcr.TestRunId, Url = GetURL(UrlType.TestRun, tcr.TestRunId, 0, project) });

                //add the results
                TestResultStub resultStub = new TestResultStub();
                resultStub.TestPlan = plan;
                resultStub.TestCaseID = tcr.TestCaseId;
                resultStub.TestResultID = tcr.TestResultId;
                resultStub.TestRunID = tcr.TestRunId;
                resultStub.Outcome = tcr.Outcome;
                resultStub.Url = GetURL(UrlType.TestResult, tcr.TestRunId, tcr.TestResultId, project);
                stub.TestResults.Add(resultStub);
            }
        }

        /// <summary>
        /// Gets the URL for using in the ref field to link to a work item in MTM
        /// </summary>
        /// <param name="urlType">The type of url to construct</param>
        /// <param name="id">The id of the item (although in the case of a test result this is the ID of the run</param>
        /// <param name="runRelativeID">This is only for a test result and is the ID of the test result</param>
        /// <param name="projectName">The name of the team project</param>
        /// <returns>a formatted url string</returns>
        private string GetURL(UrlType urlType, int id, int runRelativeID, string projectName)
        {
            //URL format is: mtm://<server name>:<port>/<tfs vdir>/<Collection name>/p:<project name>/<center group>/<group specific> 
            //An example is mtm://tfs2010:8080/tfs/DefaultCollection/p:BlogEngine.NET/testing/testplan/open?id=123
            //test case   = mtm://testServer/tfs/DefaultCollection/p:Woodgrove/testing/testcase/open?id=<ID>
            //test plan   = mtm://testServer/tfs/DefaultCollection/p:Woodgrove/testing/testplan/connect?id=<ID>
            //test result = mtm://testServer/tfs/DefaultCollection/p:Woodgrove/testing/testresult/open?id=<runrelative ID>?runid=<run id>
            //test run    = mtm://testServer/tfs/DefaultCollection/p:Woodgrove/testing/testrun/open?id=<run id>
            string server = _wis.TeamProjectCollection.Uri.ToString().Replace("http", "mtm");  //Returns http://[server]:[port]/tfs/[collection] - need to replace http with mtm
            string url = server + string.Format("/p:{0}/testing/", projectName);

            switch (urlType)
            {
                case UrlType.WorkItem:
                    url += string.Format("/testcase/open?id={0}", id);
                break;
                case UrlType.TestPlan:
                url += string.Format("/testplan/connect?id={0}", id);
                break;
                case UrlType.TestRun:
                url += string.Format("/testrun/open?id={0}", id);
                break;
                case UrlType.TestResult:
                    url += string.Format("/testresult/open?id={0}?runid={1}", runRelativeID, id);
                break;
            }

            return url;
        }
    }
}