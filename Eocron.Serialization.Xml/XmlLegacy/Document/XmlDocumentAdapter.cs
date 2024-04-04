using System.Xml;

namespace Eocron.Serialization.Xml.XmlLegacy.Document
{
    /// <summary>
    ///     Adapting XmlDocument to common interface
    /// </summary>
    public sealed class XmlDocumentAdapter : IXmlDocumentAdapter<XmlDocument>
    {
        public void AfterCreation(XmlDocument document)
        {
            if (EnableCompatibilityWithPreNetCore) ReorderNamespaceAttributes(document);
        }

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

        public XmlDocument ReadFrom(XmlReader reader)
        {
            var document = new XmlDocument();
            document.Load(reader);
            return document;
        }

        public void WriteTo(XmlDocument document, XmlWriter writer)
        {
            document.WriteTo(writer);
        }

        //For regress only < netcore version
        private static void ReorderNamespaceAttributes(XmlDocument doc)
        {
            var node = doc.DocumentElement;
            var xsi = node.Attributes["xmlns:xsi"];
            var xsd = node.Attributes["xmlns:xsd"];
            if (xsi != null && xsd != null) node.Attributes.InsertAfter(xsd, xsi);
        }

        public bool EnableCompatibilityWithPreNetCore { get; set; } = true;
    }
}