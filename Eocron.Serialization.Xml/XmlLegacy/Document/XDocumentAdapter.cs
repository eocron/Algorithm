using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Eocron.Serialization.Xml.XmlLegacy.Document
{
    /// <summary>
    ///     Adapting XDocument to common interface
    /// </summary>
    public sealed class XDocumentAdapter : IXmlDocumentAdapter<XDocument>
    {
        public void AfterCreation(XDocument document)
        {
            if (EnableCompatibilityWithPreNetCore) ReorderNamespaceAttributes(document);
        }

        public XmlWriter CreateNewDocumentAndWriter(out XDocument newDocument)
        {
            var document = new XDocument();
            newDocument = document;
            return document.CreateWriter();
        }

        public XmlReader CreateReader(XDocument document)
        {
            return document.CreateReader(ReaderOptions);
        }

        public XDocument ReadFrom(XmlReader reader)
        {
            return XDocument.Load(reader);
        }

        public void WriteTo(XDocument document, XmlWriter writer)
        {
            document.WriteTo(writer);
        }

        //For regress only < netcore version
        private static void ReorderNamespaceAttributes(XDocument doc)
        {
            var node = doc.Root;
            var all = node.Attributes().ToList();

            var xsi = all.FindIndex(x => x.Name.LocalName == "xsi");
            var xsd = all.FindIndex(x => x.Name.LocalName == "xsd");
            if (xsi >= 0 && xsd >= 0 && xsd < xsi)
            {
                (all[xsi], all[xsd]) = (all[xsd], all[xsi]);
                node.ReplaceAttributes(all);
            }
        }

        public bool EnableCompatibilityWithPreNetCore { get; set; } = true;
        public ReaderOptions ReaderOptions { get; set; }
    }
}