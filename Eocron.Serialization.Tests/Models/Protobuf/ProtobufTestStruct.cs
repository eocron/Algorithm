using ProtoBuf;

namespace Eocron.Serialization.Tests.Models.Protobuf
{
    [ProtoContract]
    public struct ProtobufTestStruct
    {
        [ProtoMember(1)] public int Value;
    }
}