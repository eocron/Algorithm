namespace Eocron.Serialization.PerformanceTests
{
    public interface ISerializationPerformanceTests
    {
        void Deserialize();
        void Serialize();
    }
}