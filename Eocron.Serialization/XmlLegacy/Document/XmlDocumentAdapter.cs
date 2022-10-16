using System;
using System.Xml;

namespace Eocron.Serialization.XmlLegacy.Document
{
    /// <summary>
    /// Adapting XmlDocument to common interface
    /// </summary>
    public sealed class XmlDocumentAdapter : IXmlDocumentAdapter<XmlDocument>
    {
        public bool EnableCompatibilityWithPreNetCore { get; set; } = true;

        public XmlWriter CreateNewDocumentAndWriter(out XmlDocument newDocument)
        {
            var document = new XmlDocument();
            newDocument = document;
            return document.CreateNavigator().AppendChild();
        }

        public XmlReader CreateReader(XmlDocument document)
        {
            return new XmlNodeReader(document);
        }

        public void WriteTo(XmlDocument document, XmlWriter writer)
        {
            document.WriteTo(writer);
        }

        public XmlDocument ReadFrom(XmlReader reader)
        {
            var document = new XmlDocument();
            document.Load(reader);
            return document;
        }

        public void AfterCreation(XmlDocument document)
        {
            if (EnableCompatibilityWithPreNetCore)
            {
                ReorderNamespaceAttributes(document);
            }
        }

        //For regress only < netcore version
        private static void ReorderNamespaceAttributes(XmlDocument doc)
        {
            var node = doc.DocumentElement;
            var xsi = node.Attributes["xmlns:xsi"];
            var xsd = node.Attributes["xmlns:xsd"];
            if (xsi != null && xsd != null)
            {
                node.Attributes.InsertAfter(xsd, xsi);
            }
        }
    }
}