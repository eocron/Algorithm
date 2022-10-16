using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Eocron.Serialization.XmlLegacy.Document
{
    /// <summary>
    /// Adapting XDocument to common interface
    /// </summary>
    public sealed class XDocumentAdapter : IXmlDocumentAdapter<XDocument>
    {
        public ReaderOptions ReaderOptions { get; set; }
        public bool EnableCompatibilityWithPreNetCore { get; set; } = true;

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

        public void WriteTo(XDocument document, XmlWriter writer)
        {
            document.WriteTo(writer);
        }

        public XDocument ReadFrom(XmlReader reader)
        {
            return XDocument.Load(reader);
        }

        public void AfterCreation(XDocument document)
        {
            if (EnableCompatibilityWithPreNetCore)
            {
                ReorderNamespaceAttributes(document);
            }
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
    }
}