using System;
using System.IO;

namespace Eocron.Serialization.XmlLegacy
{
    public interface IXmlAdapter<TDocument>
    {
        object DeserializeFromDocument(Type type, TDocument document);

        TDocument ReadDocumentFrom(StreamReader sourceStream);
        TDocument SerializeToDocument(Type type, object content);

        void WriteDocumentTo(StreamWriter targetStream, TDocument document);
    }
}