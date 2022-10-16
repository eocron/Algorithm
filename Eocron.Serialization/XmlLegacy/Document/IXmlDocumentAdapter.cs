using System.Xml;

namespace Eocron.Serialization.XmlLegacy.Document
{
    public interface IXmlDocumentAdapter<TDocument>
    {
        XmlWriter CreateNewDocumentAndWriter(out TDocument newDocument);

        XmlReader CreateReader(TDocument document);

        void WriteTo(TDocument document, XmlWriter writer);

        TDocument ReadFrom(XmlReader reader);

        void AfterCreation(TDocument document);
    }
}