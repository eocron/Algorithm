using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Eocron.Serialization.Tests.Models.Protobuf
{
    [ProtoContract]
    public class ProtobufTestModel
    {
        [ProtoMember(1)]
        public Dictionary<string, string> Dictionary { get; set; }
        [ProtoMember(2)]
        public TimeSpan TimeSpan { get; set; }
        [ProtoMember(3)]
        public Guid Guid { get; set; }
        [ProtoMember(4)]
        public ProtobufTestEnum Enum { get; set; }
        [ProtoMember(5)]
        public ProtobufTestModel Nullable { get; set; }
        [ProtoMember(6)]
        public int Integer { get; set; }
        [ProtoMember(7)]
        public double Double { get; set; }
        [ProtoMember(8)]
        public ProtobufTestStruct Struct { get; set; }
        [ProtoMember(9)]
        public List<int> List { get; set; }
        [ProtoMember(10)]
        public long[] Array { get; set; }
        [ProtoMember(11)]
        public List<int> EmptyList { get; set; }
        [ProtoMember(12)]
        public int[] EmptyArray { get; set; }
        [ProtoMember(13)]
        public string FooBarString { get; set; }
        [ProtoMember(14)]
        public DateTime DateTime { get; set; }
        [ProtoMember(15)]
        public ProtobufTestModel NullReference { get; set; }
        [ProtoMember(16)]
        public bool Boolean { get; set; }
        [ProtoMember(17)]
        public long Long { get; set; }
    }
}