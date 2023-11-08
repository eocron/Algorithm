using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using NUnit.Framework;

namespace Eocron.Serialization.PerformanceTests
{
    public abstract class SerializationPerformanceTestsBase<TTests, TModel>
    {
        protected SerializationPerformanceTestsBase(bool prepareText = true, bool prepareBinary = true)
        {
            _converter = GetConverter();
            _model = GetTestModel();
            if (prepareText)
                _serializedText = _converter.SerializeToString(_model);
            if (prepareBinary)
                _serializedBytes = _converter.SerializeToBytes(_model);
        }

        public void DeserializeBinary()
        {
            _converter.Deserialize<TModel>(_serializedBytes);
        }

        public void DeserializeText()
        {
            _converter.Deserialize<TModel>(_serializedText);
        }

        public abstract ISerializationConverter GetConverter();

        public abstract TModel GetTestModel();

        [Test]
        [Explicit]
        public void Run()
        {
            BenchmarkRunner.Run<TTests>(new DebugInProcessConfig());
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
            _converter.SerializeToBytes(_model);
        }

        public void SerializeText()
        {
            _converter.SerializeToString(_model);
        }

        protected readonly byte[] _serializedBytes;

        protected readonly ISerializationConverter _converter;
        protected readonly string _serializedText;
        protected readonly TModel _model;
    }
}