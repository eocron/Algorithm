using System.Xml;

namespace Eocron.Serialization.Xml.XmlLegacy.Document
{
    public interface IXmlDocumentAdapter<TDocument>
    {
        void AfterCreation(TDocument document);
        XmlWriter CreateNewDocumentAndWriter(out TDocument newDocument);

        XmlReader CreateReader(TDocument document);

        TDocument ReadFrom(XmlReader reader);

        void WriteTo(TDocument document, XmlWriter writer);
    }
}