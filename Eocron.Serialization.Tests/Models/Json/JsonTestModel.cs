namespace Eocron.Serialization.Tests.Models.Json;

public class JsonTestModel
{
    public Dictionary<string, string> Dictionary { get; set; }

    public TimeSpan TimeSpan { get; set; }

    public Guid Guid { get; set; }

    public JsonTestEnum Enum { get; set; }
    
    public JsonTestModel Nullable { get; set; }

    public int Integer { get; set; }

    public double Double { get; set; }

    public JsonTestStruct Struct { get; set; }

    public List<int> List { get; set; }
    
    public long[] Array { get; set; }

    public List<int> EmptyList { get; set; }

    public int[] EmptyArray { get; set; }
    
    public string FooBarString { get; set; }

    public DateTime DateTime { get; set; }

    public JsonTestModel NullReference { get; set; }

    public bool Boolean { get; set; }

    public long Long { get; set; }
}