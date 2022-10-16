# Eocron.Serialization

This library is inteded to unify C# wide known frameworks for serialization under one interface.
The list include:

  - `XmlDocument`/`XDocument` serialization using `XmlSerializer`/`XmlObjectSerializer`
  - Yaml serialization
  - Json serialization
  - Protobuf serialization

Common interface they share is `ISerializationConverter` and all `string`/`byte`/`stream` extensions are based on this interface.
Main path of defining converter is to make it as singleton and use everywhere, so its best to avoid creating those per-call (in most cases it should not affect performance).

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
  - Multiple versions of serializers like `XmlSerializer`, `XmlObjectSerializer`, `DataContractSerializer` which work with both types of documents
  - Various markups like `XmlRoot`/`DataContract`/`ISerializable`
  - Constant problems of serializing basic types like `Dictionary`/`TimeSpan` which in one version throw error, in other it will just silently empty your fields (Say HI! to `TimeSpan` being empty in .net472).
  - Chaotic changes from Microsoft to blow up your tests (rearrangment of namespaces, adding encoding attribute, etc) and sometimes criple back-compatability (Say HI! to BOM in .net6)

I decided to unify all this architectural garbage into couple of adapters, so it is easier to configure entire serialization process,
such as initial settings on namespaces/readers/writers/serializers and XSLT transformations on document, so you choose your own pill to swallow.
This should give 100% percent coverage on schema formats and compatability with bugs-as-feature, but can sometimes lower performance tweak flexibility. But who cares? XML is slow/old and you know it. Use Json where it is possible.
