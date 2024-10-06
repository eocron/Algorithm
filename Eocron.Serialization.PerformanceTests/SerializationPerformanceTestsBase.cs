using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using NUnit.Framework;

namespace Eocron.Serialization.PerformanceTests
{
    [TestFixture]
    [Explicit]
    public abstract class SerializationPerformanceTestsBase<TTests, TModel>
    {
        protected SerializationPerformanceTestsBase(bool prepareText = true, bool prepareBinary = true)
        {
            Converter = GetConverter();
            Model = GetTestModel();
            if (prepareText)
                SerializedText = Converter.SerializeToString(Model);
            if (prepareBinary)
                SerializedBytes = Converter.SerializeToBytes(Model);
        }

        public void DeserializeBinary()
        {
            Converter.Deserialize<TModel>(SerializedBytes);
        }

        public void DeserializeText()
        {
            Converter.Deserialize<TModel>(SerializedText);
        }

        public abstract ISerializationConverter GetConverter();

        public abstract TModel GetTestModel();

        [Test, Category("Performance")]
        [Explicit]
        public void Run()
        {
            BenchmarkRunner.Run<TTests>(new SerializationBencmarkConfig());
        }
        
        private class SerializationBencmarkConfig : ManualConfig
        {
            public SerializationBencmarkConfig()
            {
                AddDiagnoser(MemoryDiagnoser.Default);
                AddLogger(ConsoleLogger.Default);
                AddColumn(
                    TargetMethodColumn.Method, 
                    StatisticColumn.Median, 
                    StatisticColumn.StdDev,
                    StatisticColumn.Q1, 
                    StatisticColumn.Q3, 
                    new ParamColumn("Size"));
                
            }
        }

        public void SerializeBinary()
        {
            Converter.SerializeToBytes(Model);
        }

        public void SerializeText()
        {
            Converter.SerializeToString(Model);
        }

        protected readonly byte[] SerializedBytes;

        protected readonly ISerializationConverter Converter;
        protected readonly string SerializedText;
        protected readonly TModel Model;
    }
}