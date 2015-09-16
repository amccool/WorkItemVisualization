using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Drawing;

namespace WorkItemVisualization
{
    public static class XmlHelper
    {
        #region Header Writer
        /// <summary>
        /// Creates the output file and returns the file writer
        /// </summary>
        /// <param name="outputFile">The full path to the file to create</param>
        /// <returns>The XML Text Writer used to write to the file</returns>
        public static XmlTextWriter CreateHeader(string outputFile)
        {
            XmlTextWriter xtw = new XmlTextWriter(outputFile, Encoding.UTF8);
            xtw.Formatting = Formatting.Indented;
            xtw.WriteStartDocument();
            xtw.WriteStartElement("DirectedGraph", "http://schemas.microsoft.com/vs/2009/dgml");
            xtw.WriteAttributeString("GraphDirection", "LeftToRight");

            return xtw;
        }
        #endregion

        #region Property Writers
        /// <summary>
        /// Writes a property tool
        /// </summary>
        /// <param name="xtw">XML Text Writer</param>
        /// <param name="id">ID of the property</param>
        /// <param name="label">Labe of the Property</param>
        /// <param name="dataType">Data type in the form of "System.String" for example</param>
        public static void WriteXMLProperty(XmlTextWriter xtw, string id, string label, string dataType)
        {
            xtw.WriteStartElement("Property");
            xtw.WriteAttributeString("Id", id);
            if (label != string.Empty)
                xtw.WriteAttributeString("Label", label);
            xtw.WriteAttributeString("DataType", dataType);
            xtw.WriteEndElement();
        }

        /// <summary>
        /// Writes a property specifically to use as a link
        /// </summary>
        /// <param name="xtw">XML Text Writer</param>
        /// <param name="id">The ID of the reference template</param>
        /// <param name="isReference">Is this a reference or not (pass in True or False)</param>
        /// <param name="template">The url to be opened - can have a single parameter</param>
        public static void WriteXMLReference(XmlTextWriter xtw, string id, string isReference, string template)
        {
            xtw.WriteStartElement("Property");
            xtw.WriteAttributeString("Id", id);
            xtw.WriteAttributeString("IsReference", isReference);
            xtw.WriteAttributeString("ReferenceTemplate", template);
            xtw.WriteEndElement();
        }
        #endregion

        #region Node Writers
        /// <summary>
        /// All of these methods write an XML Node to the output file
        /// </summary>
        /// <param name="start">Determines whether or not to write the start element</param>
        /// <param name="end">Determines whether or not to write the end element</param>
        /// <param name="xtw">The XML Text Writer</param>
        /// <param name="id">The ID of the node</param>
        /// <param name="label">The label of the node</param>
        public static void WriteXMLNode(bool start, bool end, XmlTextWriter xtw, string id, string label)
        {
            //Only write a starting element if we are supposed to
            if(start)
                xtw.WriteStartElement("Node");

            xtw.WriteAttributeString("Id", id);
            xtw.WriteAttributeString("Label", label);

            if(end)
                xtw.WriteEndElement();
        }

        /// <summary>
        /// All of these methods write an XML Node to the output file
        /// </summary>
        /// <param name="start">Determines whether or not to write the start element</param>
        /// <param name="end">Determines whether or not to write the end element</param>
        /// <param name="xtw">The XML Text Writer</param>
        /// <param name="id">The ID of the node</param>
        /// <param name="label">The label of the node</param>
        /// <param name="category">The primary category of the node</param>
        public static void WriteXMLNode(bool start, bool end, XmlTextWriter xtw, string id, string label, string category)
        {
            //Only write a starting element if we are supposed to
            if (start)
                xtw.WriteStartElement("Node");

            WriteXMLNode(false, false, xtw, id, label);
            xtw.WriteAttributeString("Category", category);

            if (end)
                xtw.WriteEndElement();
        }

        /// <summary>
        /// All of these methods write an XML Node to the output file
        /// </summary>
        /// <param name="start">Determines whether or not to write the start element</param>
        /// <param name="end">Determines whether or not to write the end element</param>
        /// <param name="xtw">The XML Text Writer</param>
        /// <param name="id">The ID of the node</param>
        /// <param name="label">The label of the node</param>
        /// <param name="category">The primary category of the node</param>
        /// <param name="secondaryProperty">Writes a secondary property</param>
        /// <param name="secondaryPropertyValue">Writes the value of the secondary property</param>
        public static void WriteXMLNode(bool start, bool end, XmlTextWriter xtw, string id, string label, string category, 
            string secondaryProperty, string secondaryPropertyValue)
        {
            //Only write a starting element if we are supposed to
            if (start)
                xtw.WriteStartElement("Node");

            WriteXMLNode(false, false, xtw, id, label, category);
            xtw.WriteAttributeString(secondaryProperty, secondaryPropertyValue);

            if (end)
                xtw.WriteEndElement();
        }

        /// <summary>
        /// All of these methods write an XML Node to the output file
        /// </summary>
        /// <param name="start">Determines whether or not to write the start element</param>
        /// <param name="end">Determines whether or not to write the end element</param>
        /// <param name="xtw">The XML Text Writer</param>
        /// <param name="id">The ID of the node</param>
        /// <param name="label">The label of the node</param>
        /// <param name="category">The primary category of the node</param>
        /// <param name="group">The group can either be Expanded or Collapsed and is only used for container nodes</param>
        public static void WriteXMLNode(bool start, bool end, XmlTextWriter xtw, string id, string label, string category, string group)
        {
            //Only write a starting element if we are supposed to
            if (start)
                xtw.WriteStartElement("Node");

            WriteXMLNode(false, false, xtw, id, label, category);
            xtw.WriteAttributeString("Group", group);

            if (end)
                xtw.WriteEndElement();
        }

        public static void WriteXMLNode(bool start, bool end, XmlTextWriter xtw, string id, string label, string category, string group, string[] categories)
        {
            //Only write a starting element if we are supposed to
            if (start)
                xtw.WriteStartElement("Node");

            WriteXMLNode(false, false, xtw, id, label, category, group);
            WriteCategories(xtw, categories);

            if (end)
                xtw.WriteEndElement();
        }

        /// <summary>
        /// All of these methods write an XML Node to the output file
        /// </summary>
        /// <param name="start">Determines whether or not to write the start element</param>
        /// <param name="end">Determines whether or not to write the end element</param>
        /// <param name="xtw">The XML Text Writer</param>
        /// <param name="id">The ID of the node</param>
        /// <param name="label">The label of the node</param>
        /// <param name="categories">An array of additional categories used for link navigation purposes</param>
        public static void WriteXMLNode(bool start, bool end, XmlTextWriter xtw, string id, string label, string[] categories)
        {
            //Only write a starting element if we are supposed to
            if (start)
                xtw.WriteStartElement("Node");

            WriteXMLNode(false, false, xtw, id, label);
            WriteCategories(xtw, categories);

            if (end)
                xtw.WriteEndElement();
        }

        /// <summary>
        /// All of these methods write an XML Node to the output file
        /// </summary>
        /// <param name="start">Determines whether or not to write the start element</param>
        /// <param name="end">Determines whether or not to write the end element</param>
        /// <param name="xtw">The XML Text Writer</param>
        /// <param name="id">The ID of the node</param>
        /// <param name="label">The label of the node</param>
        /// <param name="primaryCategory">Used when there is a primary category and a secondary list of categories</param>
        /// <param name="categories">An array of additional categories used for link navigation purposes</param>
        public static void WriteXMLNode(bool start, bool end, XmlTextWriter xtw, string id, string label, string primaryCategory, string[] categories)
        {
            //Only write a starting element if we are supposed to
            if (start)
                xtw.WriteStartElement("Node");

            WriteXMLNode(false, false, xtw, id, label, primaryCategory);
            WriteCategories(xtw, categories);

            if (end)
                xtw.WriteEndElement();
        }

        /// <summary>
        /// Used when all options of the node have to be written
        /// </summary>
        /// <param name="start">Determines whether or not to write the start element</param>
        /// <param name="end">Determines whether or not to write the end element</param>
        /// <param name="xtw">The XML Text Writer</param>
        /// <param name="id">The ID of the node</param>
        /// <param name="label">The label of the node</param>
        /// <param name="primaryCategory">Used when there is a primary category and a secondary list of categories</param>
        /// <param name="categories">An array of additional categories used for link navigation purposes</param>
        /// <param name="secondaryProperty">Writes a secondary property</param>
        /// <param name="secondaryPropertyValue">Writes the value of the secondary property</param>
        public static void WriteXMLNode(bool start, bool end, XmlTextWriter xtw, string id, string label, 
            string primaryCategory, string[] categories, string secondaryProperty, string secondaryPropertyValue)
        {
            //Only write a starting element if we are supposed to
            if (start)
                xtw.WriteStartElement("Node");

            WriteXMLNode(false, false, xtw, id, label, primaryCategory);
            xtw.WriteAttributeString(secondaryProperty, secondaryPropertyValue);
            WriteCategories(xtw, categories);

            if (end)
                xtw.WriteEndElement();
        }

        /// <summary>
        /// Writes the array of categories for the node
        /// </summary>
        /// <param name="xtw">XML Text Writer to use</param>
        /// <param name="categories">The array of categories to write</param>
        private static void WriteCategories(XmlTextWriter xtw, string[] categories)
        {
            for (int i = 0; i < categories.Length; i++)
            {
                xtw.WriteStartElement("Category");
                xtw.WriteAttributeString("Ref", categories[i]);
                xtw.WriteEndElement();
            }
        }
        #endregion

        #region Link Writers
        public static void WriteXMLLink(XmlTextWriter xtw, string source, string target)
        {
            xtw.WriteStartElement("Link");
            xtw.WriteAttributeString("Source", source);
            xtw.WriteAttributeString("Target", target);
            xtw.WriteEndElement();
        }

        /// <summary>
        /// Writes a link node to the links section
        /// </summary>
        /// <param name="xtw">The XML Writer to use</param>
        /// <param name="source">The ID of the source work item</param>
        /// <param name="target">The ID of the target work item</param>
        /// <param name="category">The link category</param>
        public static void WriteXMLLink(XmlTextWriter xtw, string source, string target, string category, string label)
        {
            xtw.WriteStartElement("Link");
            xtw.WriteAttributeString("Source", source);
            xtw.WriteAttributeString("Target", target);
            if (category != string.Empty)
                xtw.WriteAttributeString("Category", category);
            if (label != string.Empty)
                xtw.WriteAttributeString("Label", label);

            xtw.WriteEndElement();
        }

        public static void WriteXMLLink(XmlTextWriter xtw, string source, string target, string category, string label, bool visible)
        {
            xtw.WriteStartElement("Link");
            xtw.WriteAttributeString("Source", source);
            xtw.WriteAttributeString("Target", target);
            if (category != string.Empty)
                xtw.WriteAttributeString("Category", category);
            if (label != string.Empty)
                xtw.WriteAttributeString("Label", label);
            if (!visible)
                xtw.WriteAttributeString("Visibility", "Hidden");
            xtw.WriteEndElement();
        }

        #endregion

        #region Category Writers
        /// <summary>
        /// Writes the categories that define node navigation
        /// </summary>
        /// <param name="xtw">The XML Text Writer used to write to the file</param>
        /// <param name="id">The ID of the category used in references</param>
        /// <param name="incoming">The name of the incoming node navigation (for a user story this may be Parent because you
        /// navigate to this node through another node which is a child of the user story)</param>
        /// <param name="outgoing">The name of the outgoing node navigateion (for a user story this may be Child because you
        /// navigate from the user story to the child of the user story)</param>
        /// <param name="contains">Determines if this is a contains relationship. This will be no only for Related at this point</param>
        /// <example><Category Id="Parent/Child" CanBeDataDriven="True" CanLinkedNodesBeDataDriven="True" IncomingActionLabel="Parent" IsContainment="True" OutgoingActionLabel="Child" /></example>
        public static void WriteNavigationCategory(XmlTextWriter xtw, string id, string incoming, string outgoing, string contains)
        {
            xtw.WriteStartElement("Category");
            xtw.WriteAttributeString("Id", id);
            xtw.WriteAttributeString("CanBeDataDriven", "True");
            xtw.WriteAttributeString("CanLinkedNodesBeDataDriven", "True");
            xtw.WriteAttributeString("IsContainment", contains);
            xtw.WriteAttributeString("InboundName", incoming);
            xtw.WriteAttributeString("OutboundName", outgoing);
            xtw.WriteAttributeString("IncomingActionLabel", incoming);
            xtw.WriteAttributeString("OutgoingActionLabel", outgoing);
            xtw.WriteEndElement();
        }
        #endregion

        /*
         * <Styles>
    <Style TargetType="Node" GroupLabel="Changeset" ValueLabel="True">
      <Condition Expression="HasCategory('Changeset')" />
      <Setter Property="Background" Value="#FFFFFF80" />
    </Style>
    <Style TargetType="Node" GroupLabel="Bug" ValueLabel="True">
      <Condition Expression="HasCategory('Bug')" />
      <Setter Property="Background" Value="#FFFF8080" />
    </Style>
  </Styles>
         * */
        #region Style Writers
        /// <summary>
        /// Adds a style to provide coloring to the background of a group of nodes
        /// </summary>
        /// <param name="xtw"></param>
        /// <param name="groupLabel">The group to apply this style to</param>
        /// <param name="backgroundColor">The color to apply</param>
        public static void WriteXMLNodeStyle(XmlTextWriter xtw, string groupLabel, string backgroundColor)
        {
            xtw.WriteStartElement("Style");
            xtw.WriteAttributeString("TargetType", "Node");
            xtw.WriteAttributeString("GroupLabel", groupLabel);
            xtw.WriteAttributeString("ValueLabel", "True");
            
            xtw.WriteStartElement("Condition");
            xtw.WriteAttributeString("Expression", string.Format("HasCategory('{0}')", groupLabel));
            xtw.WriteEndElement();  //Close the condition tag

            xtw.WriteStartElement("Setter");
            xtw.WriteAttributeString("Property", "Background");
            xtw.WriteAttributeString("Value", backgroundColor);
            xtw.WriteEndElement();  //Close the Setter tag

            xtw.WriteEndElement();  //Close the style tag
        }
        #endregion

        /// <summary>
        /// Gets  a list of colors to use when styling the nodes
        /// </summary>
        /// <returns>An array of colors</returns>
        public static string[] GetColorList()
        {
            List<string> colorList = new List<string>();
            colorList.Add(ColorTranslator.ToHtml(Color.LightBlue));
            colorList.Add(ColorTranslator.ToHtml(Color.LightPink));
            colorList.Add(ColorTranslator.ToHtml(Color.LightGreen));
            colorList.Add(ColorTranslator.ToHtml(Color.LightGoldenrodYellow));
            colorList.Add(ColorTranslator.ToHtml(Color.Lavender));
            colorList.Add(ColorTranslator.ToHtml(Color.LimeGreen));
            colorList.Add(ColorTranslator.ToHtml(Color.MediumSlateBlue));
            colorList.Add(ColorTranslator.ToHtml(Color.MistyRose));
            colorList.Add(ColorTranslator.ToHtml(Color.OliveDrab));
            colorList.Add(ColorTranslator.ToHtml(Color.OrangeRed));
            colorList.Add(ColorTranslator.ToHtml(Color.Red));
            colorList.Add(ColorTranslator.ToHtml(Color.PaleTurquoise));
            colorList.Add(ColorTranslator.ToHtml(Color.PeachPuff));
            colorList.Add(ColorTranslator.ToHtml(Color.Peru));
            colorList.Add(ColorTranslator.ToHtml(Color.RosyBrown));
            colorList.Add(ColorTranslator.ToHtml(Color.SeaGreen));
            colorList.Add(ColorTranslator.ToHtml(Color.Sienna));
            colorList.Add(ColorTranslator.ToHtml(Color.Silver));
            colorList.Add(ColorTranslator.ToHtml(Color.Tan));
            colorList.Add(ColorTranslator.ToHtml(Color.Azure));
            colorList.Add(ColorTranslator.ToHtml(Color.Maroon));
            colorList.Add(ColorTranslator.ToHtml(Color.Purple));
            return colorList.ToArray();
        }
    }
}