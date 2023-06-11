# Eocron.Serialization

This library is inteded to unify C# wide known frameworks for serialization under one interface.
The list include:

- `XmlDocument`/`XDocument` serialization using `XmlSerializer`/`XmlObjectSerializer`
- Yaml serialization
- Json serialization
- Protobuf serialization

Common interface they share is `ISerializationConverter` and all `string`/`byte`/`stream` extensions are based on this
interface.
Main path of defining converter is to make it as singleton and use everywhere, so its best to avoid creating those
per-call (in most cases it should not affect performance).

Example:

    public static readonly ISerializationConverter XDocument =
        new XmlSerializationConverter<XDocument>(
            new XmlAdapter<XDocument>(
                new XmlSerializerAdapter(x => new XmlSerializer(x)),
                new XDocumentAdapter()));

Or:

    public static readonly ISerializationConverter XmlDataContract =
        new XmlSerializationConverter<XmlDocument>(
            new XmlAdapter<XmlDocument>(
                new XmlObjectSerializerAdapter(x => new DataContractSerializer(x)),
                new XmlDocumentAdapter()));

Or:

    public static readonly ISerializationConverter Json = new JsonSerializationConverter(
        new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented
        });

Usage:

    var xml = XmlDataContract.SerializeToString(myObj);
    XmlDataContract.SerializeTo(myObj, stream);
    var bytes = XmlDataContract.SerializeToBytes(myObj);

## XML

Because of many problems related to XML in C# such as:

- Multiple versions of documents like `XmlDocument`, `XDocument`
- Multiple versions of serializers like `XmlSerializer`, `XmlObjectSerializer`, `DataContractSerializer` which work with
  both types of documents
- Various markups like `XmlRoot`/`DataContract`/`ISerializable`
- Constant problems of serializing basic types like `Dictionary`/`TimeSpan` which in one version throw error, in other
  it will just silently empty your fields (Say HI! to `TimeSpan` being empty in .net472).
- Chaotic changes from Microsoft to blow up your tests (rearrangment of namespaces, adding encoding attribute, etc) and
  sometimes criple back-compatability (Say HI! to BOM in .net6)

I decided to unify all this architectural garbage into couple of adapters, so it is easier to configure entire
serialization process,
such as initial settings on namespaces/readers/writers/serializers and XSLT transformations on document, so you choose
your own pill to swallow.
This should give 100% percent coverage on schema formats and compatability with bugs-as-feature, but can sometimes lower
performance tweak flexibility. But who cares? XML is slow/old and you know it. Use Json where it is possible.

## Benchmark

    BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19044.2130/21H2/November2021Update)
    AMD Ryzen 5 3600, 1 CPU, 12 logical and 6 physical cores
      [Host]     : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256
      Job-VOETLF : .NET Framework 4.8 (4.8.4515.0), X64 LegacyJIT VectorSize=256 DEBUG

    BuildConfiguration=Debug  

    |                  Method |       Mean |     Error |    StdDev |     Median |
    |------------------------ |-----------:|----------:|----------:|-----------:|
    |     ProtobufDeserialize |   3.912 us | 0.0512 us | 0.0479 us |   3.920 us |
    |       ProtobufSerialize |   3.090 us | 0.0617 us | 0.1380 us |   3.025 us |
    |         JsonDeserialize |  14.750 us | 0.2365 us | 0.3075 us |  14.596 us |
    |           JsonSerialize |   8.996 us | 0.1325 us | 0.1239 us |   9.012 us |
    | DataContractDeserialize |  41.471 us | 0.6344 us | 0.5624 us |  41.183 us |
    |   DataContractSerialize |  36.643 us | 0.7258 us | 0.8067 us |  36.399 us |
    |    XDocumentDeserialize |  34.189 us | 0.3386 us | 0.3168 us |  34.432 us |
    |      XDocumentSerialize |  20.747 us | 0.4102 us | 0.5477 us |  20.675 us |
    |  XmlDocumentDeserialize |  38.562 us | 0.7628 us | 0.9647 us |  38.449 us |
    |    XmlDocumentSerialize |  29.083 us | 0.5791 us | 0.8844 us |  28.936 us |
    |         YamlDeserialize | 114.085 us | 1.7690 us | 1.4772 us | 114.266 us |
    |           YamlSerialize | 165.450 us | 3.2628 us | 7.4968 us | 162.044 us |
