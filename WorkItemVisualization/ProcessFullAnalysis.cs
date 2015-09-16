using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.TestManagement.Client;
using System.Diagnostics;
using Microsoft.TeamFoundation;
using System.Drawing;

namespace WorkItemVisualization
{
    public class ProcessFullAnalysis
    {
        #region private members
        private List<WorkItemStub> _workItemStubs;  //Stores the distinct list of all work items to be written to the dgml
        private WorkItemStore _wis;
        private VersionControlServer _vcs;
        private ITestManagementService _tms;
        private ITestManagementTeamProject _tmp;
        private List<TestPlanInfo> _testPlans = new List<TestPlanInfo>();   //Stores a distinct list of test plans
        private List<string> _iterations = new List<string>();              //Stores a distinct list of iterations
        private List<string> _allFiles = new List<string>();                //Stores a distinct list of files
        private List<TempLinkType2> _links;         //Stores all link types
        private string _projectName;                //The name of the team project
        private string _outputFile;                 //The DGML file to write to
        private bool _groupByIteration = false;     //Are we grouping by iteration?
        private bool _dependendyAnalysis = false;   //Are we doing a dependency analysis?
        private bool _hideLinks = false;            //If we are doing a dependency analysis, should we hide the intervening nodes and links
        private bool _full;                         //Did the user select full processing
        private List<string> _allNodeTypes;         //Stores a distinct list of all node types for use in styling the nodes
        private List<TempLinkType> _selectedLinks;  //This is the list of links that the user selected on the UI
        private WorkItemStub _tempStub;              //Used for dependency analysis to hold the stub which contains the changeset(s)
        #endregion

        public ProcessFullAnalysis(WorkItemStore wis, VersionControlServer vcs, string projectName, string outputFile)
        {
            _wis = wis;
            _vcs = vcs;
            _tms = vcs.TeamProjectCollection.GetService<ITestManagementService>();
            _tmp = _tms.GetTeamProject(projectName);
            _projectName = projectName;
            _outputFile = outputFile;
        }

        #region Process Work Items and Changesets
        /// <summary>
        /// Method for processing work items down to the changesets that are related to them
        /// </summary>
        /// <param name="wi">Work Item to process</param>
        /// <param name="outputFile">File to write the dgml to</param>
        /// <param name="vcs">Version Control Server which contains the changesets</param>
        public void ProcessWorkItemRelationships()
        {
            _full = true;

            //Pull every work item for the project
            Query q = new Query(_wis, string.Format("SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], "
                + "[System.State] FROM WorkItems WHERE [System.TeamProject] = '{0}'", _projectName));
            WorkItemCollection wic = q.RunQuery();

            WorkItem[] workItems = new WorkItem[wic.Count];

            for (int i = 0; i < wic.Count; i++)
                workItems[i] = wic[i];

            Process(workItems);
        }

        public void ProcessWorkItemRelationships(WorkItem[] wi, bool groupbyIteration, bool dependencyAnalysis, List<TempLinkType> selectedLinks, bool hideLinks)
        {
            _full = false;
            _groupByIteration = groupbyIteration;
            _dependendyAnalysis = dependencyAnalysis;
            _selectedLinks = selectedLinks;
            _hideLinks = hideLinks;

            Process(wi);
        }

        private void Process(WorkItem[] workItems)
        {
            _workItemStubs = new List<WorkItemStub>();
            _allNodeTypes = new List<string>();

            //Get the full list of link types for assigning category relationships
            _links = LoadLinkTypes(_projectName);

            //Process all work items
            for (int i = 0; i < workItems.Length; i++)
            {
                ProcessWorkItemCS(workItems[i]);
            }

            //The colors for all other node types (i.e. not work item types)
            _allNodeTypes.Add("Changeset");
            _allNodeTypes.Add("Iteration");
            _allNodeTypes.Add("File");
            _allNodeTypes.Add("Test Plan");
            _allNodeTypes.Add("Test Run");
            _allNodeTypes.Add("Test Result");

            //Process the changesets so they are also related to the actual root work item
            if (_dependendyAnalysis)
            {
                //Only process work items that have changesets
                for (int i = 0; i < _workItemStubs.Count; i++)
                {
                    if (_workItemStubs[i].Changesets.Count > 0)
                    {
                        _tempStub = _workItemStubs[i];
                        GetWorkItemParent(_tempStub);

                    }
                }
            }

            //Output the results
            WriteChangesetInfo(_outputFile, _projectName);
        }

        /// <summary>
        /// Finds the link type based on an end name
        /// </summary>
        /// <param name="value">The end to use in the search (such as Parent)</param>
        /// <returns>The name of the relationship (such as Parent/Child)</returns>
        private TempLinkType2 GetRelationship(string value)
        {
            TempLinkType2 s = _links.Find(
                delegate(TempLinkType2 stub)
                {
                    return stub.Reverse == value;
                }
            );

            if (s == null)
            {
                s = _links.Find(
                    delegate(TempLinkType2 stub)
                    {
                        return stub.Forward == value;
                    }
                );
            }

            return s;
        }

        /// <summary>
        /// Write the work item and changeset information to a dgml file
        /// </summary>
        /// <param name="outputFile">The file to write the dgml to</param>
        private void WriteChangesetInfo(string outputFile, string project)
        {
            //Create a new DGML document
            XmlTextWriter xtw = XmlHelper.CreateHeader(outputFile);

            #region Write Nodes
            //Add the nodes section
            xtw.WriteStartElement("Nodes");

            //These two arrays are used to store the single category for both files and changesets
            string[] cs = new string[] { "Has Work Items/Has Changesets" };
            string[] file = new string[] { "In Changeset/Has Files", "Associated Work Items/Has Files" };

            //Add the iteration nodes if we are grouping by iteration
            if (_groupByIteration)
            {
                for (int i = 0; i < _iterations.Count; i++)
                {
                    XmlHelper.WriteXMLNode(true, true, xtw, _iterations[i], _iterations[i], "Iteration", "Expanded");
                }
            }

            //Add the individual work item nodes
            for (int i = 0; i < _workItemStubs.Count; i++)
            {
                //Stores the unique list of categories associated with this node
                List<string> categories = new List<string>();

                for (int l = 0; l < _workItemStubs[i].Related.Count; l++)
                {
                    //Check to see if the category has been added to the array and if not add it
                    if (!categories.Contains(_workItemStubs[i].Related[l].Relationship.ToString()))
                        categories.Add(_workItemStubs[i].Related[l].Relationship.ToString());
                }

                //At this point we have all the categories, write them
                XmlHelper.WriteXMLNode(true, true, xtw, _workItemStubs[i].ID.ToString(), _workItemStubs[i].Title, 
                    _workItemStubs[i].WorkItemTypeName, categories.ToArray(), "WorkItem", _workItemStubs[i].ID.ToString());

                if (_workItemStubs[i].Changesets != null)
                {
                    //Add the changesets
                    for (int j = 0; j < _workItemStubs[i].Changesets.Count; j++)
                    {
                        string csID = "CS" + _workItemStubs[i].Changesets[j].ID.ToString(); //Create the changeset ID
                        
                        if (!_dependendyAnalysis)
                        {
                            //Only add the changeset as a group if this is not a dependency analysis graph
                            XmlHelper.WriteXMLNode(true, true, xtw, csID, csID, "Changeset", "Expanded", cs);

                            //Add all of the files related to each changeset
                            for (int k = 0; k < _workItemStubs[i].Changesets[j].Files.Count; k++)
                            {
                                string fileId = _workItemStubs[i].Changesets[j].Files[k].FileName + ";" + csID;
                                XmlHelper.WriteXMLNode(true, true, xtw, _workItemStubs[i].Changesets[j].Files[k].FullPath + ";" + csID, fileId, "File");
                            }
                        }
                        else
                        {
                            XmlHelper.WriteXMLNode(true, true, xtw, csID, csID, "Changeset");
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
                        XmlHelper.WriteXMLNode(true, true, xtw, "TP" + _testPlans[j].TestPlanID, 
                            "Test Plan: " + _testPlans[j].TestPlanName, "Test Plan", "Collapsed");

                        //Add the test runs
                        for (int k = 0; k < _testPlans[j].TestRuns.Count; k++)
                        {
                            XmlHelper.WriteXMLNode(true, true, xtw, "TestRun" + _testPlans[j].TestRuns[k].ToString(), 
                                "Test Run " + _testPlans[j].TestRuns[k].ToString(), "Test Run", "Collapsed");
                        }
                    }

                    //Add the test results
                    for (int j = 0; j < _workItemStubs[i].TestResults.Count; j++)
                    {
                        //The ID for the purposes of the DGML is "TR" + the test result ID
                        string outcome = string.Format("{0}\n{1}", "Test Case: " + _workItemStubs[i].TestResults[j].TestCaseID, Enum.GetName(typeof(TestOutcome), _workItemStubs[i].TestResults[j].Outcome));
                        string trID = "TR" + _workItemStubs[i].TestResults[j].TestRunID.ToString() + _workItemStubs[i].TestResults[j].TestResultID.ToString() + _workItemStubs[i].TestResults[j].TestCaseID.ToString();
                        XmlHelper.WriteXMLNode(true, true, xtw, trID, outcome, "Test Result", "TestOutcome", Enum.GetName(typeof(TestOutcome), _workItemStubs[i].TestResults[j].Outcome));
                    }
                }
            }

            //We only add a discrete list of files if we are running a dependency analysis - otherwise all files
            //are added as part of their changesets above and are broken out into distinct files
            if (_dependendyAnalysis)
            {
                for (int i = 0; i < _allFiles.Count; i++)
                {
                    XmlHelper.WriteXMLNode(true, true, xtw, _allFiles[i], _allFiles[i], "File", file);
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
                if (_groupByIteration)
                {
                    XmlHelper.WriteXMLLink(xtw, _workItemStubs[i].Iteration, _workItemStubs[i].ID.ToString(), "Contains", string.Empty);
                }

                //Add each related item to the DGML
                for (int j = 0; j < _workItemStubs[i].Related.Count; j++)
                {
                    XmlHelper.WriteXMLLink(xtw, _workItemStubs[i].ID.ToString(), _workItemStubs[i].Related[j].ID.ToString(), _workItemStubs[i].Related[j].Relationship.ToString(), string.Empty);

                    if (_groupByIteration)
                    {
                        XmlHelper.WriteXMLLink(xtw, _workItemStubs[i].Iteration, _workItemStubs[i].Related[j].ID.ToString(), "Contains", string.Empty);
                    }
                }

                //Add the changesets and files to the DGML
                if (_workItemStubs[i].Changesets != null)
                {
                    for (int j = 0; j < _workItemStubs[i].Changesets.Count; j++)
                    {
                        //for each changeset, get the ID and add a link from the work item to the changeset
                        string csID = "CS" + _workItemStubs[i].Changesets[j].ID.ToString();
                        XmlHelper.WriteXMLLink(xtw, _workItemStubs[i].ID.ToString(), csID, "Has Work Items/Has Changesets", string.Empty);
                            
                        for (int k = 0; k < _workItemStubs[i].Changesets[j].Files.Count; k++)
                        {
                            if (!_dependendyAnalysis)
                            {
                                //Add a link from the changeset to the file
                                XmlHelper.WriteXMLLink(xtw, csID, _workItemStubs[i].Changesets[j].Files[k].FullPath + ";" + csID, "In Changeset/Has Files", string.Empty);
                            }
                            else
                            {
                                XmlHelper.WriteXMLLink(xtw, _workItemStubs[i].ID.ToString(), _workItemStubs[i].Changesets[j].Files[k].FileName, "Associated Work Items/Has Files", string.Empty);
                                XmlHelper.WriteXMLLink(xtw, csID, _workItemStubs[i].Changesets[j].Files[k].FileName, "In Changeset/Has Files", string.Empty);
                            }
                        }   //End of file loop
                    }   //End of changeset loop
                }   //End of if statement

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

            #region Write Categories
            
            xtw.WriteStartElement("Categories");
            for (int i = 0; i < _links.Count; i++)
            {
                if (_links[i].Forward != "Related")
                    XmlHelper.WriteNavigationCategory(xtw, _links[i].ToString(), _links[i].Forward, _links[i].Reverse, "True");
                else
                    XmlHelper.WriteNavigationCategory(xtw, _links[i].ToString(), _links[i].Forward, _links[i].Reverse, "False");
            }
            xtw.WriteEndElement();
            #endregion

            #region Write Properties

            //Write a property node and custom properties for hyperlink usage
            string server = _wis.TeamProjectCollection.Uri.ToString().Replace("http", "mtm");  //Returns http://[server]:[port]/tfs/[collection] - need to replace http with mtm
            string url = server + string.Format("/p:{0}/testing/", project);

            xtw.WriteStartElement("Properties");
            XmlHelper.WriteXMLProperty(xtw, "TestOutcome", "Test Outcome", "System.String");
            XmlHelper.WriteXMLReference(xtw, "WorkItem", "True", url + "/testcase/open?id={0}");
            xtw.WriteEndElement();

            #endregion

            #region Write Styles
            xtw.WriteStartElement("Styles");

            string[] clr = XmlHelper.GetColorList();
            int z = 0;

            for (int i = 0; i < _allNodeTypes.Count; i++)
            {
                XmlHelper.WriteXMLNodeStyle(xtw, _allNodeTypes[i], clr[z]);
                if (z == clr.Length)
                    z = 0;
                else
                    z++;
            }

            xtw.WriteEndElement();
            #endregion

            //Close the DGML document
            xtw.WriteEndElement();
            xtw.Close();

            //Open the DGML in Visual Studio
            System.Diagnostics.Process.Start(outputFile);
        }

        /// <summary>
        /// Loads the link types for processing
        /// </summary>
        private List<TempLinkType2> LoadLinkTypes(string project)
        {
            List<TempLinkType2> links = new List<TempLinkType2>();

            WorkItemLinkTypeCollection linkTypes = _wis.Projects[project].Store.WorkItemLinkTypes;

            foreach (WorkItemLinkType t in linkTypes)
            {
                links.Add(new TempLinkType2() { Reverse = t.ForwardEnd.Name, Forward = t.ReverseEnd.Name });
            }

            links.Add(new TempLinkType2() { Forward = "Has Work Items", Reverse = "Has Changesets" });
            links.Add(new TempLinkType2() { Forward = "Test Results", Reverse = "Test Results" });
            links.Add(new TempLinkType2() { Forward = "In Changeset", Reverse = "Has Files" });

            //Used for dependency analysis only
            links.Add(new TempLinkType2() { Forward = "Associated Work Items", Reverse = "Has Files" });
            return links;
        }

        /// <summary>
        /// Checks to see if the work item stub exists
        /// </summary>
        /// <param name="id">The ID of the work item to find</param>
        /// <returns>The work item stub or null if it isn't found</returns>
        private WorkItemStub GetStub(int id)
        {
            //Perform a find on the ID, if it is found, s is populated
            WorkItemStub s = _workItemStubs.Find(
                    delegate(WorkItemStub stub)
                    {
                        return stub.ID == id;
                    }
                );

            return s;
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
            WorkItemStub s = GetStub((int)wi.Fields[CoreField.Id].Value);

            //If it isn't found, create a new work item stub
            if (s == null)
            {
                s = new WorkItemStub();
                s.ID = (int)wi.Fields[CoreField.Id].Value;
                s.Title = wi.Fields[CoreField.Title].Value.ToString();
                s.WorkItemTypeName = wi.Fields[CoreField.WorkItemType].Value.ToString();
                s.Iteration = wi.Fields[CoreField.IterationPath].Value.ToString();

                if (!_allNodeTypes.Contains(s.WorkItemTypeName))
                    _allNodeTypes.Add(s.WorkItemTypeName);

                //Check to see if the iteration exists in the list of iterations and add it if it doesn't
                if (!_iterations.Contains(s.Iteration))
                    _iterations.Add(s.Iteration);

                _workItemStubs.Add(s);
            }

            return s;
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
                if (_full)
                {
                    ProcessLinks(wi, wi.WorkItemLinks[i], s);
                }
                else
                {
                    if (IsLinkSelected(wi.WorkItemLinks[i].LinkTypeEnd.Name))
                    {
                        ProcessLinks(wi, wi.WorkItemLinks[i], s);
                    }
                }
            }

            if (_full)
                GetChangesets(wi, s);
            else
            {
                if (IsLinkSelected("Changesets"))
                    GetChangesets(wi, s);
            }

            if (s.WorkItemTypeName == "Test Case")
            {
                if (_full)
                {
                    GetTestResults(s, wi.Project.Name);
                }
                else
                {
                    if (IsLinkSelected("Test Results"))
                        GetTestResults(s, wi.Project.Name);
                }
            } 
        }

        /// <summary>
        /// Processes a relationship between two work items
        /// </summary>
        /// <param name="wi">The source work item</param>
        /// <param name="wiLink">The link to the target work item</param>
        /// <param name="s">The work item stub that corrosponds to the source work item</param>
        private void ProcessLinks(WorkItem wi, WorkItemLink wiLink, WorkItemStub s)
        {
            int targetID = wiLink.TargetId;
            //Check to see if the work item that is related to this one is in the list of work items and if not, add it by
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

            //Check to see if the ID and relationship match (we can have more than one relationship between two work items
            //for example, we could have a parent/child relationship and also a predecessor/successor relationship
            WorkItemRelationship result1 = s.Related.Find(
                delegate(WorkItemRelationship stub)
                {
                    return stub.ToString() == targetID.ToString() + wiLink.LinkTypeEnd.Name;
                }
            );

            if (result1 == null)
            {
                bool found = false;
                //Before we add this relationship in, make sure we have never added this relationship anywhere at all
                //so we make sure each relationship (forward/reverse end of the same relationship) is unique in the
                //entire result set.
                for (int j = 0; j < _workItemStubs.Count; j++)
                {
                    if (_workItemStubs[j].ID == targetID)
                    {
                        for (int k = 0; k < _workItemStubs[j].Related.Count; k++)
                        {
                            if (_workItemStubs[j].Related[k].ID == wi.Id)
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    if (found)
                        break;
                }

                //If we didn't find an existing relationship, make sure we add it to the 
                //work item at the reverse end of the relationship. For example, in a Parent/
                //Child relationship the Child is the Forward end of the relationship and
                //Parent is the reverse end of the relationship. We need to add this relationship
                //to the Parent to say that the parent has children so when the links are
                //written out to the DGML they point the right direction
                if (!found)
                {
                    //get the relationship
                    TempLinkType2 t = GetRelationship(wiLink.LinkTypeEnd.Name);
                    //Determine if this is the forward or reverse end of the relationship
                    if (t.Reverse == wiLink.LinkTypeEnd.Name)
                    {
                        //This is the reverse end of the relationship so add it, otherwise skip it altogether
                        s.Related.Add(new WorkItemRelationship() { ID = targetID, End = wiLink.LinkTypeEnd.Name, Relationship = GetRelationship(wiLink.LinkTypeEnd.Name) });
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// This method takes a work item and examines it's relationships to determine what
        /// the root work item is. The use of this method is in dependency analysis which
        /// takes the following ReqA --> TaskB --> ChangesetC and changes it to 
        /// ReqA --> Changeset C. This base work item has changesets otherwise we would
        /// not be processing it. For our purposes we would be starting with TaskB and
        /// looking for ReqA or some other parent
        /// </summary>
        /// <param name="wi">The work item to discover the parent of</param>
        private void GetWorkItemParent(WorkItemStub stub)
        {
            //The rules for discovery are as follows:
            //1. Walk the parent/child relationship from the reverse to the forward node
            //2. Check the related work item ONLY when going from a Bug to a Test Case
            //   - never the reverse or you get trapped in a recursive loop
            //3. Check the affected by/affects from the reverse to the forward (in other
            //   words the standard relationship is a Requiremenet is Affected By a
            //   Change Request which has an associated changeset or child task
            
            WorkItemStub s = null;  //stores the related work item stubs
            bool found = false;     //indicates if we found a valid link to persue

            //Because of how the relationships are constructed, only one end of the relationship has
            //the actual link to the relationship. For example, Story1 has relationships with Task1
            //and Task2 but Task1 and Task2 don't know anything about Story1. So, when we are going
            //through this list, we have to determine which other work items are related to the work
            //item we're looking at right now.

            //NOTE I may have to come back to this because it may be a fallacy that just because
            //story1 is related to task1 and task2 doesn't mean that there isn't a "story3" that
            //is the parent of Story1
            //This returns a list of parent work items that are related to thsi work item.
            List<WorkItemStub> r = GetRelatedWorkItems(stub);
            for (int i = 0; i < r.Count; i++)
            {
                s = GetStub(r[i].ID);
                GetWorkItemParent(s);
                found = true;
            }
        
            //If we haven't found any other links that are parent/child then this
            //must be the root that we are associating the changesets with
            if (!found)
            {
                if (_tempStub != stub)
                {
                    //Associate the changeset with this work item
                    for (int k = 0; k < _tempStub.Changesets.Count; k++)
                    {
                        stub.Changesets.Add(_tempStub.Changesets[k]);
                    }
                }
            }
        }

        /// <summary>
        /// Gets all work item stubs related to a given work item stub
        /// </summary>
        /// <param name="stub">The work item to get relationships for</param>
        /// <returns>A list of related work item stubs</returns>
        private List<WorkItemStub> GetRelatedWorkItems(WorkItemStub stub)
        {
            List<WorkItemStub> related = new List<WorkItemStub>();

            for (int i = 0; i < _workItemStubs.Count; i++)
            {
                for (int k = 0; k < _workItemStubs[i].Related.Count; k++)
                {
                    if ((_workItemStubs[i].Related[k].ID == stub.ID) && (_workItemStubs[i].Related[k].End == "Child"))
                    {
                        related.Add(_workItemStubs[i]);
                    }
                }
            }
            return related;
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

                    //We have a valid changeset link type
                    if (ex.ArtifactLinkType.Equals(_wis.RegisteredLinkTypes[ArtifactLinkIds.Changeset]))
                    {
                        //Get the ID of the changeset
                        ArtifactId artifact = LinkingUtilities.DecodeUri(ex.LinkedArtifactUri);
                        cs_id = Convert.ToInt32(artifact.ToolSpecificId);

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