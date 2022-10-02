using System;
using System.IO;
using System.Xml;

namespace Eocron.Serialization.Xml
{
    public interface IXmlDocumentSerializer
    {
        object DeserializeFromXmlDocument(Type type, XmlDocument document);

        XmlDocument SerializeToXmlDocument(Type type, object obj);

        XmlDocument ReadFrom(StreamReader sourceStream);

        void WriteTo(StreamWriter targetStream, XmlDocument document);
    }
}