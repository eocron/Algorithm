using System;
using System.IO;

namespace Eocron.Serialization
{
    public interface ISerializationConverter 
    {
        object DeserializeFromStreamReader(Type type, StreamReader sourceStream);

        void SerializeToStreamWriter(Type type, object obj, StreamWriter targetStream);
    }
}
