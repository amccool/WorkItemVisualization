using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.Xml;

namespace WorkItemVisualization
{
    /// <summary>
    /// Used to create the DGML to display a single work item state transition from beginning to end
    /// </summary>
    public class ProcessStateDGML
    {
        private List<string> _allStates;    //Holds a unique list of states for styling

        /// <summary>
        /// Processes the state history of a work item
        /// </summary>
        /// <param name="wi">The work item to process</param>
        public void ProcessStates(WorkItem wi, string outputFile)
        {
            _allStates = new List<string>();

            //Stores all of the state transitions
            List<StateInfo> si = new List<StateInfo>();
            string current = string.Empty;  //Current state
            string previous = string.Empty; //Previous state
            int j = 0;  //Used as a unique identifier

            //Loop through all of the work item revisions starting from the beginning
            for (int i = 0; i < wi.Revisions.Count; i++)
            {
                //Check to see if the state was the field that was changed and double-check to make sure
                //for whatever reason that it did change
                if ((wi.Revisions[i].Fields[CoreField.State] != null) &&
                    (wi.Revisions[i].Fields[CoreField.State].OriginalValue != wi.Revisions[i].Fields[CoreField.State].Value))
                {
                    j += 1;     //Start the identifier at 1
                    StateInfo s = new StateInfo();  //Used to hold the state
                    if (j == 1)
                        s.FromState = "Created";    //For the first state only, the starting state is null so set it to created
                    else
                        //The from state is the state + a number (i.e. Proposed1)
                        s.FromState = wi.Revisions[i].Fields[CoreField.State].OriginalValue.ToString() + j.ToString();

                    //The to state is the state + a number (i.e. Active2) so this way the states
                    //automatically increment (i.e. Proposed1 > Active2 > Proposed3 > Active4 > Closed5
                    //This allows us to put the same state on the form twice and make sure the order of
                    //referencing is correct.
                    s.ToState = wi.Revisions[i].Fields[CoreField.State].Value.ToString() + (j + 1).ToString();
                    s.Id = wi.Revisions[i].Fields[CoreField.State].Value.ToString();
                    s.Date = wi.Revisions[i].Fields[CoreField.ChangedDate].Value.ToString();
                    s.By = wi.Revisions[i].Fields[CoreField.ChangedBy].Value.ToString();
                    s.Reason = wi.Revisions[i].Fields[CoreField.Reason].Value.ToString();
                    si.Add(s);

                    if (!_allStates.Contains(wi.Revisions[i].Fields[CoreField.State].Value.ToString()))
                        _allStates.Add(wi.Revisions[i].Fields[CoreField.State].Value.ToString());
                }
            }

            WriteStateInfo(si, outputFile);
        }

        /// <summary>
        /// Writes the output to a dgml file and opens it
        /// </summary>
        /// <param name="si">The State Info to output</param>
        /// <param name="outputFile">The file to output it to</param>
        private void WriteStateInfo(List<StateInfo> si, string outputFile)
        {
            //Create the file and return the writer
            XmlTextWriter xtw = XmlHelper.CreateHeader(outputFile);

            //Add the nodes section
            xtw.WriteStartElement("Nodes");

            for (int i = 0; i < si.Count; i++)
            {
                XmlHelper.WriteXMLNode(true, true, xtw, si[i].ToState, si[i].GetCategory(), si[i].Id);
            }

            //Close the Nodes section
            xtw.WriteEndElement();

            //Add the links section
            xtw.WriteStartElement("Links");

            for (int i = 1; i < si.Count; i++)
            {
                XmlHelper.WriteXMLLink(xtw, si[i].FromState, si[i].ToState, si[i].Reason, string.Empty);
            }

            //Close the links section
            xtw.WriteEndElement();

            #region Write Styles
            xtw.WriteStartElement("Styles");

            string[] clr = XmlHelper.GetColorList();
            int z = 0;

            for (int i = 0; i < _allStates.Count; i++)
            {
                XmlHelper.WriteXMLNodeStyle(xtw, _allStates[i], clr[z]);
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
    }
}