using System;
using System.IO;
using Eocron.Serialization;

namespace Eocron.Serialization.Security
{
    public abstract class BinarySerializationConverterBase : ISerializationConverter
    {
        public object DeserializeFrom(Type type, StreamReader sourceStream)
        {
            var br = new BinaryReader(sourceStream.BaseStream);
            return DeserializeFrom(type, br);
        }
        
        public void SerializeTo(Type type, object obj, StreamWriter targetStream)
        {
            var bw = new BinaryWriter(targetStream.BaseStream);
            SerializeTo(type, obj, bw);
            bw.Flush();
        }
        
        protected abstract object DeserializeFrom(Type type, BinaryReader reader);

        protected abstract void SerializeTo(Type type, object obj, BinaryWriter writer);
    }
}