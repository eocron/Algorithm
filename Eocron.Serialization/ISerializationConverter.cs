using System;
using System.IO;

namespace Eocron.Serialization
{
    public interface ISerializationConverter
    {
        object DeserializeFrom(Type type, StreamReader sourceStream);

        void SerializeTo(Type type, object obj, StreamWriter targetStream);
    }
}