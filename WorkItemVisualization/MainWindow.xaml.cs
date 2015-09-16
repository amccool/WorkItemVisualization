using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.Win32;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.TestManagement.Client;
using System.Threading;
using System.ComponentModel;
using System.Diagnostics;

namespace WorkItemVisualization
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Class Scoped Values
        /// <summary>
        /// A reference to the selected TFS Server
        /// </summary>
        private TfsTeamProjectCollection _tfs;

        /// <summary>
        /// A reference to the work item store for the given TFS server
        /// Used for querying the work items
        /// </summary>
        private WorkItemStore _wis;

        /// <summary>
        /// Provides access to the individual projects
        /// </summary>
        private ICommonStructureService _css;

        /// <summary>
        /// Used to store the images used in the treeview in memory
        /// </summary>
        private Dictionary<string, ImageSource> _graphicDictionary;

        /// <summary>
        /// Stores the selected link types
        /// </summary>
        private List<TempLinkType> _selectedLinks;

        /// <summary>
        /// Used to run calls on a background thread
        /// </summary>
        private BackgroundWorker _background;
        private BackgroundWorker _graphics;

        /// <summary>
        /// Stores the work items that back the list view for
        /// processing
        /// </summary>
        private List<WorkItemData> _queryResults;
        #endregion

        /// <summary>
        /// Launches the WPF window and loads the list of registered servers
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            LoadRegisteredServers();
            txtOutput.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Output.dgml";
            LoadGraphics();
        }

        #region Load the graphics in the background
        private void LoadGraphics()
        {
            _graphics = new BackgroundWorker();
            _graphics.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_graphics_RunWorkerCompleted);
            _graphics.DoWork += new DoWorkEventHandler(_graphics_DoWork);
            _graphics.RunWorkerAsync();
        }

        void _graphics_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] img = { "Directed Link Query.png", "Flat Query.png",
                           "Tree Query.png", "Open Folder.png", "Closed Folder.png"};
            Dictionary<string, ImageSource> d = new Dictionary<string, ImageSource>();

            for (int i = 0; i < img.Length; i++)
            {
                System.IO.Stream filestream = this.GetType().Assembly.GetManifestResourceStream(string.Format("WorkItemVisualization.Graphics.{0}", img[i]));
                PngBitmapDecoder pd = new PngBitmapDecoder(filestream, BitmapCreateOptions.None, BitmapCacheOption.Default);
                ImageSource imgSource = pd.Frames[0];
                d.Add(img[i], imgSource);
            }

            e.Result = d;
        }

        void _graphics_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _graphicDictionary = (Dictionary<string, ImageSource>)e.Result;
        }

        #endregion

        #region Load the registered servers on a background thread
        /// <summary>
        /// Sets the background worker thread and starts it
        /// </summary>
        private void LoadRegisteredServers()
        {
            _background = new BackgroundWorker();
            _background.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_background_RunWorkerCompleted);
            _background.DoWork += new DoWorkEventHandler(_background_DoWork);
            _background.RunWorkerAsync();
        }

        /// <summary>
        /// Performs the actual long running work
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _background_DoWork(object sender, DoWorkEventArgs e)
        {
            RegisteredProjectCollection[] collections = RegisteredTfsConnections.GetProjectCollections();
            e.Result = collections;
        }

        /// <summary>
        /// When the work is completed, this event is raised and then the
        /// combobox is loaded and enabled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _background_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            cboServer.ItemsSource = (RegisteredProjectCollection[])e.Result;
            cboServer.DisplayMemberPath = "Uri";
            cboServer.IsEnabled = true;
        }
        #endregion

        #region Handle server issues like connecting and loading projects
        /// <summary>
        /// Connects to the Project Collection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cboServer.Text != string.Empty)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    _tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(cboServer.Text), 
                        new UICredentialsProvider());
                    _tfs.EnsureAuthenticated();
                    LoadProjects();
                    _wis = (WorkItemStore)_tfs.GetService(typeof(WorkItemStore));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Loads the list of projects and enables the project combo box
        /// </summary>
        private void LoadProjects()
        {
            //Get the list of projects and display them
            _css = (ICommonStructureService)_tfs.GetService(typeof(ICommonStructureService));
            cboProjects.ItemsSource = _css.ListProjects();
            cboProjects.IsEnabled = true;
        }

        /// <summary>
        /// Adds a new Server/Project Collection to the list of registered servers on the
        /// local machine and then re-binds the combobox to the list of registered servers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            TeamProjectPicker tpp = new TeamProjectPicker(TeamProjectPickerMode.NoProject, false);
            tpp.ShowDialog();
            LoadRegisteredServers();
        }
        #endregion

        #region Select a project and Load Treeview and List
        /// <summary>
        /// Populate the list of work item queries for the selected team project
        /// **Special thanks to Jim Lamb for his code to help do this easily**
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSelectProject_Click(object sender, RoutedEventArgs e)
        {
            List<TreeNode> _nodes = new List<TreeNode>();

            //Clear the contents of the treeview
            tvwQueries.ItemsSource = null;

            if (string.IsNullOrEmpty(cboProjects.Text))
            {
                MessageBox.Show("Please select a team project");

                return;
            }

            //Add a new root item
            tvwQueries.ItemTemplate = (DataTemplate)this.FindResource("TreeNodeX");
            TreeNode n = new TreeNode();
            n.Text = "Work Item Queries";
            n.Image = _graphicDictionary["Closed Folder.png"];
            _nodes.Add(n);

            var si = cboProjects.SelectedItem as ProjectInfo;

            var projName = si.Name;

            var spQ = from x in _wis.Projects.OfType<Project>()
                     where
                     x.Name.Equals(projName)
                     select x;

            var sp = spQ.FirstOrDefault();

            if(sp == null)
            {
                throw new Exception();
            }

            //foreach (Project item in _wis.Projects)
            //{
                foreach (var queryItem in sp.QueryHierarchy)
                {
                    if (queryItem is QueryFolder)
                    {
                        GetNodesForQueryTree(queryItem as QueryFolder, n);
                    }
                    else
                    {
                        Trace.WriteLine(queryItem);
                    }
                }
            //}


            //////Loop through the top level folders in the project query folders
            ////foreach (QueryItem queryItem in _wis.Projects[cboProjects.Text].QueryHierarchy)
            ////{
            ////    //add each folder and all nodes under it recursively
            ////    GetNodesForQueryTree(queryItem as QueryFolder, n);
            ////}

            tvwQueries.ItemsSource = _nodes;
            
            LoadLinkTypes();
        }

        /// <summary>
        /// Recursively load the treeview
        /// </summary>
        /// <param name="queryFolder">The Query Folder to get information about</param>
        /// <param name="node">The node to add the query folder information to</param>
        private void GetNodesForQueryTree(QueryFolder queryFolder, TreeNode node)
        {
            //Add the folder to the treeview
            TreeNode folderNode = new TreeNode();
            folderNode.Text = queryFolder.Name;
            folderNode.Tag = queryFolder;
            folderNode.Image = _graphicDictionary["Closed Folder.png"];
            node.Items.Add(folderNode);
            
            int lastFolder = node.Items.Count - 1;

            //Loop through all items in the list of sub-items for this folder
            foreach (QueryItem queryItem in queryFolder)
            {
                //If the item is a query folder, call this method
                if (queryItem is QueryFolder)
                {
                    GetNodesForQueryTree(queryItem as QueryFolder, node.Items[lastFolder]);
                }
                else
                {
                    try
                    {
                        //If it isn't, add the query to the list of queries
                        TreeNode itemNode = new TreeNode();
                        
                        QueryDefinition qd = _wis.GetQueryDefinition(queryItem.Id);
                        itemNode.Text = queryItem.Name;
                        itemNode.Tag = queryItem;
                        ImageSource img = null;
                        if (qd.QueryType == QueryType.OneHop)
                        { img = _graphicDictionary["Directed Link Query.png"]; }
                        else if (qd.QueryType == QueryType.List)
                        { img = _graphicDictionary["Flat Query.png"]; }
                        else
                        { img = _graphicDictionary["Tree Query.png"]; }

                        itemNode.Image = img;
                        folderNode.Items.Add(itemNode);
                    }
                    catch (ArgumentException ex)
                    {
                        /*        MessageBox.Show("Could not get the query definition because of an invalid ID. Query Information is"
                                    + " as follows:\nQuery Item ID:" + queryItem.Id + "\nQuery Item Name: " + queryItem.Name
                                    + "\nSpecific message: " + ex.Message + "\nProcessing will continue.", "Retrieve Query Definition",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                    */
                        Trace.WriteLine(ex);
                    }

                }
            }
        }

        /// <summary>
        /// Loads the link types for processing
        /// </summary>
        private void LoadLinkTypes()
        {
            var si = cboProjects.SelectedItem as ProjectInfo;

            var projName = si.Name;

            var spQ = from x in _wis.Projects.OfType<Project>()
                      where
                      x.Name.Equals(projName)
                      select x;

            var sp = spQ.FirstOrDefault();

            if (sp == null)
            {
                throw new Exception();
            }


            //WorkItemLinkTypeCollection linkTypes = _wis.Projects[cboProjects.Text].Store.WorkItemLinkTypes;
            WorkItemLinkTypeCollection linkTypes = sp.Store.WorkItemLinkTypes;

                _selectedLinks = new List<TempLinkType>();

                foreach (WorkItemLinkType t in linkTypes)
                {
                    _selectedLinks.Add(new TempLinkType() { LinkType = t.ForwardEnd.Name, Selected = true, Enabled = true, IsForward = true });
                    //If the link type is related then the reverse is also related
                    if (t.ForwardEnd.Name != "Related")
                        _selectedLinks.Add(new TempLinkType() { LinkType = t.ReverseEnd.Name, Selected = true, Enabled = true, IsForward = false });
                }

                _selectedLinks.Add(new TempLinkType() { LinkType = "Changesets", Selected = false, Enabled = true, IsForward = true });
                _selectedLinks.Add(new TempLinkType() { LinkType = "Test Results", Selected = false, Enabled = true, IsForward = true });
                //Feature not implemented yet
                //_selectedLinks.Add(new TempLinkType() { LinkType = "Show Hyperlinks", Selected = true, Enabled = true, IsForward = true });
                lstLinkTypes.ItemsSource = _selectedLinks;
        }

        #endregion

        #region Run Query

        /// <summary>
        /// Executes the selected work item query
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunQuery()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            WorkItemCollection wic;
            string finalQuery = string.Empty;

            TreeNode item = (TreeNode)tvwQueries.SelectedItem;

            if (item.Tag.GetType() == typeof(QueryDefinition))
            {
                _queryResults = new List<WorkItemData>();

                //var projectName = cboProjects.Text;
                var si = cboProjects.SelectedItem as ProjectInfo;
                var projectName = si.Name;

                QueryDefinition qd = (QueryDefinition)item.Tag;
                //Replace the @Project and @Me options if they exist in the query
                finalQuery = qd.QueryText;
                if (finalQuery.IndexOf("@project") > 0)
                    finalQuery = finalQuery.Replace("@project", "'" + projectName + "'");
                if (finalQuery.IndexOf("@Me") > 0)
                    finalQuery = finalQuery.Replace("@Me", System.Security.Principal.WindowsIdentity.GetCurrent().Name);

                Query query = new Query(_wis, finalQuery);
                if (query.IsTreeQuery || query.IsLinkQuery)
                {
                    _queryResults = GetHierarchicalResults(query);
                }
                else
                {
                    wic = query.RunQuery();
                    for (int i = 0; i < wic.Count; i++)
                    {
                        _queryResults.Add(new WorkItemData() { ID = wic[i].Id, WI = wic[i] });
                    }
                }
                LoadListView(_queryResults, query);
            }
            Mouse.OverrideCursor = null;
        }
        
        /// <summary>
        /// Returns all of the work items that are a result of a hierarchical query
        /// </summary>
        /// <param name="query">The query to execute</param>
        /// <returns>The list of WorkItemData that contains the results</returns>
        private List<WorkItemData> GetHierarchicalResults(Query query)
        {
            List<WorkItemData> temp = new List<WorkItemData>();

            //Returns the links related to this particular query - this will return
            //all of the work item ID's (source and target) returned by the qurey, but 
            //no actual work items
            WorkItemLinkInfo[] links = query.RunLinkQuery();
            
            //Get the actual work items associated with the source and target ID's from the
            //query
            Dictionary<int, WorkItem> resultWIs = null;
            List<WorkItemData> workItems = new List<WorkItemData>();

            //At this point, resultWIs have all of the work items in the query
            if (links != null)
                resultWIs = GetWorkItemsById(links, query);

            for (int wiIndex = 0; wiIndex < links.Length; wiIndex++)
            {
                WorkItemLinkInfo wiInfo = links[wiIndex];

                workItems.Add(new WorkItemData() { ID = wiInfo.TargetId, ParentID = wiInfo.SourceId, WI = resultWIs[wiInfo.TargetId] });
            }

            return workItems;
        }

        /// <summary>
        /// Batch retrieves work items based on the ID
        /// </summary>
        /// <param name="links">The list of work item ID's to retrieve the full work item for</param>
        /// <param name="q">The query</param>
        /// <returns>A dictionary where the key is the Work Item ID and the value is the Work Item</returns>
        private Dictionary<int, WorkItem> GetWorkItemsById(WorkItemLinkInfo[] links, Query q)
        {
            //Some place to store the work items and their ID's
            Dictionary<int, WorkItem> results = new Dictionary<int, WorkItem>();

            // Do batch read of work items from collected IDs
            WorkItemCollection workItems = GetWorkItemsFromLinkInfo(links, q);

            //Add the work items to the dictionary. This is necessary because when we
            //construct the layout of the results, we need to get the work items from
            //the ID's so they are ordered correctly.
            if (workItems != null)
            {
                foreach (WorkItem wi in workItems)
                    results.Add(wi.Id, wi);
            }

            return results;
        }

        /// <summary>
        /// Gets further details of the links in support of the GetWorkItemsById method
        /// </summary>
        /// <param name="links">The list of work items to get the details for</param>
        /// <param name="q">The query to execute</param>
        /// <returns>A WorkItemCollection containing all requested links</returns>
        private WorkItemCollection GetWorkItemsFromLinkInfo(WorkItemLinkInfo[] links, Query q)
        {
            //Check to see if there are any links to queryy
            if (links == null || links.GetLength(0) < 1)
                return null;

            //Holds the list of work item ID's to query for
            List<int> wisInBatch = new List<int>();

            // Get IDs for work items represented by post items:
            BatchReadParameterCollection batchReadParameters = new BatchReadParameterCollection();

            try
            {
                //Add all of the work item target ID's to the batch parameters for querying
                for (int wiIndex = 0; wiIndex < links.Length; ++wiIndex)
                {
                    int wiId = links[wiIndex].TargetId;
                    if (wiId != -1 && !wisInBatch.Contains(wiId))
                    {
                        batchReadParameters.Add(new BatchReadParameter(wiId));
                        wisInBatch.Add(wiId);
                    }
                }
                //Get the work item collection which contains all of the ID's
                return _wis.Query(batchReadParameters, "select [System.Id] from WorkItems");
            }
            catch 
            {
                return null;
            }
        }
        #endregion

        #region Load ListView
        /// <summary>
        /// Loads the listview with work items returned by the query
        /// </summary>
        /// <param name="wic">The collection of work items</param>
        /// <param name="q">The query that produced these work items</param>
        private void LoadListView(List<WorkItemData> wic, Query q)
        {
            //Set the data source of the grid to be the work item collection
            lstResults.ItemsSource = wic;

            //Clear the columns that are already in the grid
            GridView g = (GridView)lstResults.View;

            g.Columns.Clear();

            System.Windows.Controls.GridViewColumn selection = new System.Windows.Controls.GridViewColumn();
            selection.CellTemplate = (DataTemplate)this.FindResource("SelectionColumn");
            
            //Could not get this working yet
            //selection.HeaderTemplate = (DataTemplate)this.FindResource("SelectionColumn");

            g.Columns.Add(selection);

            //For each of the columns specified as a display column for the query, bind a 
            //new column
            for (int i = 0; i < q.DisplayFieldList.Count; i++)
            {
                //Create the new text column
                System.Windows.Controls.GridViewColumn c = new System.Windows.Controls.GridViewColumn();

                //Set the header to the display field name
                c.Header = q.DisplayFieldList[i].Name;

                //Create a new binding with the path to the value that is being bound
                Binding b;
                if (q.DisplayFieldList[i].Name != "Title")
                    b = new Binding("WI.Fields[" + q.DisplayFieldList[i].Name + "].Value");
                else
                    b = new Binding("Title");
                b.Mode = BindingMode.OneTime;
                c.DisplayMemberBinding = b;

                //Set the binding and add the column
                g.Columns.Add(c);
            }
        }
        #endregion

        /// <summary>
        /// Allows the user to select the output file for the generated DGML
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Title = "Select output location";
            save.FileName = "Output.dgml";
            save.AddExtension = true;
            save.Filter = "Directed Graph Markup Language (*.dgml)|*.dgml";
            save.DefaultExt = "dgml";
            save.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            save.ShowDialog();
            txtOutput.Text = save.FileName;
        }

        #region Process Button Clicks
        /// <summary>
        /// Returns the work item that has been selected in the grid
        /// </summary>
        /// <returns>A fully populated work item object</returns>
        private WorkItem[] GetSelectedWorkItems()
        {
            List<WorkItem> wi;

            if (_queryResults != null)
            {
                wi = (from w in _queryResults
                      where w.Selected == true
                      select w.WI).ToList<WorkItem>();

                return wi.ToArray<WorkItem>();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates the state transition graph in DGML
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnState_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                WorkItem[] w = GetSelectedWorkItems();
                if (w == null || w.Length == 0)
                {
                    MessageBox.Show("To run a state visualization, select a single work item first.");
                }
                else
                {
                    ProcessStateDGML data = new ProcessStateDGML();
                    data.ProcessStates(w[0], txtOutput.Text);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Unauthorized Access Exception");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Create the work item graph in DGML
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnWorkItems_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            ProcessFullAnalysis data;

            try
            {
                var si = cboProjects.SelectedItem as ProjectInfo;

                var projName = si.Name;

                //var spQ = from x in _wis.Projects.OfType<Project>()
                //          where
                //          x.Name.Equals(projName)
                //          select x;

                //var sp = spQ.FirstOrDefault();

                //if (sp == null)
                //{
                //    throw new Exception();
                //}





                //Check to see if a project is even selected, if not, skip everything else
                if (projName == string.Empty)
                {
                    MessageBox.Show("Select a Team Project before trying to process any relationships.");
                }
                else
                {
                    //There is a project selected, see if they are doing full processing
                    if (ProcessCheckbox(chkFull))
                    {
                        data = new ProcessFullAnalysis(_wis, (VersionControlServer)_tfs.GetService(typeof(VersionControlServer)),
                                projName, txtOutput.Text);
                        data.ProcessWorkItemRelationships();
                    }
                    else
                    {
                        //No, we aren't doing a full analysis, make sure something is
                        //selected to do the analysis on
                        WorkItem[] wi = GetSelectedWorkItems();

                        if ((wi == null) || (wi.Length == 0))
                        {
                            MessageBox.Show("Select one or more work items to visualize.");
                        }
                        else
                        {
                            data = new ProcessFullAnalysis(_wis, (VersionControlServer)_tfs.GetService(typeof(VersionControlServer)),
                                projName, txtOutput.Text);

                            bool groupByIteration = ProcessCheckbox(chkByIteration);
                            bool dependencyAnalysis = ProcessCheckbox(chkDependencyAnalysis);
                            bool hideLinks = ProcessCheckbox(chkHideInterveningLinks);

                            data.ProcessWorkItemRelationships(GetSelectedWorkItems(), groupByIteration, dependencyAnalysis, _selectedLinks, hideLinks);
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Unauthorized Access Exception");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
   
        /// <summary>
        /// Indicates whether or not the checkbox is checked
        /// </summary>
        /// <param name="box">The checkbox to process</param>
        /// <returns>True if it is checked or false if it is not</returns>
        private bool ProcessCheckbox(CheckBox box)
        {
            if ((box.IsChecked != null) && (box.IsChecked == true))
                return true;
            else
                return false;
        }

        #region Dependency Analysis
        //Process the dependency analysis checkbox and the related changeset checkbox
        //which must be checked
        private void chkDependencyAnalysis_Checked(object sender, RoutedEventArgs e)
        {
            ProcessDependencyAnalysisCheckbox();
        }

        private void chkDependencyAnalysis_Unchecked(object sender, RoutedEventArgs e)
        {
            ProcessDependencyAnalysisCheckbox();
        }

        /// <summary>
        /// Performs a number of operations based on whether a dependency analysis
        /// will be performed or not
        /// </summary>
        private void ProcessDependencyAnalysisCheckbox()
        {
            bool isChecked = ProcessCheckbox(chkDependencyAnalysis);

            //Check to see if the link types have been loaded. If they haven't it means
            //that the user hasn't select a team project yet.
            if (lstLinkTypes.Items.Count > 0)
            {
                //Find the underlying link type for a changesest
                TempLinkType result = _selectedLinks.Find(
                    delegate(TempLinkType s)
                    {
                        return s.LinkType == "Changesets";
                    }
                );

                //Set the underlying link type to checked  if the dependency
                //analysis is selected and disable it so the user can't uncheck it
                result.Selected = isChecked;
                result.Enabled = !isChecked;

                //*****NOT READY TO BE IMPLEMENTED YET*******
                //If it is checked, show the hide intervening links checkbox
                //if (isChecked)
                //{
                //    chkHideInterveningLinks.Visibility = System.Windows.Visibility.Visible;
                //}
                //else
                //{
                //    //Otherwise, hide it and uncheck it
                //    chkHideInterveningLinks.Visibility = System.Windows.Visibility.Hidden;
                //    chkHideInterveningLinks.IsChecked = false;
                //}

                //Refresh the links so they pick up the underlying data changes
                lstLinkTypes.Items.Refresh();
            }
            else
            {
                //This if is in here to handle the situation where the user could uncheck the dependency
                //analysis checkbox and the method is still triggered
                if (isChecked)
                {
                    MessageBox.Show("Please select a Team Project first.", "No Team Project Selected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    chkDependencyAnalysis.IsChecked = false;
                }
            }
        }
        #endregion

        #endregion

        private void tvwQueries_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((TreeNode)tvwQueries.SelectedItem).Tag != null)
                RunQuery();
        }
    }
}