using ProtoBuf;

namespace Eocron.Serialization.Tests.Models.Protobuf
{
    [ProtoContract]
    public enum ProtobufTestEnum
    {
        [ProtoEnum]
        One,
        [ProtoEnum]
        Two,
        [ProtoEnum]
        Three,
    }
}