// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.Experimental.UIElements.Debugger
{
    class UxmlExporter
    {
        private const string UIElementsNamespace = "UnityEngine.Experimental.UIElements";

        [Flags]
        public enum ExportOptions
        {
            None = 0,
            NewLineOnAttributes = 1,
            StyleFields = 2,
            AutoNameElements = 4,
        }

        public static string Dump(VisualElement selectedElement, string templateId, ExportOptions options)
        {
            Dictionary<XNamespace, string> nsToPrefix = new Dictionary<XNamespace, string>()
            {
                { UIElementsNamespace, "ui" }
            };
            var doc = new XDocument();
            XElement template = new XElement("Template",
                    new XAttribute("id", templateId)
                    );
            doc.Add(template);

            Recurse(template, nsToPrefix, selectedElement, options);

            foreach (var it in nsToPrefix)
            {
                template.Add(new XAttribute(XNamespace.Xmlns + it.Value, it.Key));
            }

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                OmitXmlDeclaration = true,
                NewLineOnAttributes = (options & ExportOptions.NewLineOnAttributes) == ExportOptions.NewLineOnAttributes,
                NewLineHandling = NewLineHandling.Replace
            };

            StringBuilder sb = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
                doc.Save(writer);

            return sb.ToString();
        }

        private static void Recurse(XElement parent, Dictionary<XNamespace, string> nsToPrefix, VisualElement ve, ExportOptions options)
        {
            //todo: handle namespace
            XElement elt;

            string ns = ve.GetType().Namespace ?? "";
            string typeName = ve.typeName;
            Dictionary<string, string> attrs = new Dictionary<string, string>();

            string nsp;
            if (nsToPrefix.TryGetValue(ns, out nsp))
            {
                elt = new XElement((XNamespace)ns + typeName);
            }
            else
                elt = new XElement(typeName);

            parent.Add(elt);

            foreach (var attr in attrs)
                elt.SetAttributeValue(attr.Key, attr.Value);

            if (!String.IsNullOrEmpty(ve.name) && ve.name[0] != '_')
                elt.SetAttributeValue("name", ve.name);
            else if ((options & ExportOptions.AutoNameElements) == ExportOptions.AutoNameElements)
            {
                var genName = ve.GetType().Name + (ve.text != null ? ve.text.Replace(" ", "") : "");
                elt.SetAttributeValue("name", genName);
            }


            if (!String.IsNullOrEmpty(ve.text))
                elt.SetAttributeValue("text", ve.text);

            var classes = ve.GetClasses();
            if (classes.Any())
                elt.SetAttributeValue("class", string.Join(" ", classes.ToArray()));

            var container = ve as VisualContainer;
            if (container != null)
            {
                var childContainer = container;

                for (int i = 0; i < childContainer.childrenCount; i++)
                {
                    Recurse(elt, nsToPrefix, childContainer.GetChildAt(i), options);
                }
            }
        }
    }
}
