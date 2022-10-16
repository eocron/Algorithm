using System;
using System.IO;

namespace Eocron.Serialization.XmlLegacy
{
    public interface IXmlAdapter<TDocument>
    {
        TDocument SerializeToDocument(Type type, object content);

        object DeserializeFromDocument(Type type, TDocument document);

        TDocument ReadDocumentFrom(StreamReader sourceStream);

        void WriteDocumentTo(StreamWriter targetStream, TDocument document);
    }
}