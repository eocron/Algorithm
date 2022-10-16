using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using NUnit.Framework;

namespace Eocron.Serialization.Tests.Performance
{
    public class AllPerformanceTests
    {
        private readonly DataContractSerializationPerformanceTests _dataContract;
        private readonly JsonSerializationPerformanceTests _json;
        private readonly ProtobufSerializationPerformanceTests _protobuf;
        private readonly XDocumentSerializationPerformanceTests _xDocument;
        private readonly XmlDocumentSerializationPerformanceTests _xmlDocument;
        private readonly YamlSerializationPerformanceTests _yaml;

        public AllPerformanceTests()
        {
            _dataContract = new DataContractSerializationPerformanceTests();
            _json = new JsonSerializationPerformanceTests();
            _protobuf = new ProtobufSerializationPerformanceTests();
            _xDocument = new XDocumentSerializationPerformanceTests();
            _xmlDocument = new XmlDocumentSerializationPerformanceTests();
            _yaml = new YamlSerializationPerformanceTests();
        }

        [Benchmark()]
        public void ProtobufDeserialize()
        {
            _protobuf.Deserialize();
        }

        [Benchmark()]
        public void ProtobufSerialize()
        {
            _protobuf.Serialize();
        }

        [Benchmark()]
        public void JsonDeserialize()
        {
            _json.Deserialize();
        }

        [Benchmark()]
        public void JsonSerialize()
        {
            _json.Serialize();
        }


        [Benchmark()]
        public void DataContractDeserialize()
        {
            _dataContract.Deserialize();
        }

        [Benchmark()]
        public void DataContractSerialize()
        {
            _dataContract.Serialize();
        }

        [Benchmark()]
        public void XDocumentDeserialize()
        {
            _xDocument.Deserialize();
        }

        [Benchmark()]
        public void XDocumentSerialize()
        {
            _xDocument.Serialize();
        }

        [Benchmark()]
        public void XmlDocumentDeserialize()
        {
            _xmlDocument.Deserialize();
        }

        [Benchmark()]
        public void XmlDocumentSerialize()
        {
            _xmlDocument.Serialize();
        }

        [Benchmark()]
        public void YamlDeserialize()
        {
            _yaml.Deserialize();
        }

        [Benchmark()]
        public void YamlSerialize()
        {
            _yaml.Serialize();
        }

        [Test]
        [Explicit]
        public void Run()
        {
            BenchmarkRunner.Run<AllPerformanceTests>(new DebugBuildConfig());
        }
    }
}
