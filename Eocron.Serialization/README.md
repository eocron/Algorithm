# Eocron.Serialization

This library is inteded to unify C# wide known frameworks for serialization under one interface.
The list include:

  - XmlDocument/XDocument serialization using XmlSerializer/XmlObjectSerializer
  - Yaml serialization
  - Json serialization
  - Protobuf serialization

Common interface they share is `ISerializationConverter` and all string/byte/stream extensions are based on this interface.
Main path of defining converter is to make it as singleton and use everywhere, so its best to avoid creating those per-call (in most cases it should not affect performance).

Example:

    public static readonly ISerializationConverter XmlDataContract =
     new XmlSerializationConverter<XmlDocument>(
         new XmlSerializerAdapter<XmlDocument>(x =>
             new DataContractSerializer(x)));

Usage as simple as:

    var xml = XmlDataContract.SerializeToString(myObj);
    XmlDataContract.SerializeTo(myObj, stream);
    var bytes = XmlDataContract.SerializeToBytes(myObj);


## XML

Because of many problems related to XML in C#, such as:

  - Multiple versions of documents like XmlDocument, XDocument
  - Multiple versions of serializers like XmlSerializer, XmlObjectSerializer which work with both types of documents
  - Various markups
  - Constant problems of serializing basic types like Dictionary/TimeSpan which in one version throw error, in other it will just silently empty your fields.
  - Chaotic changes from Microsoft which blow up testing and sometimes back-compatability. Like changing encoding, rearrangment of namespaces, etc.

I decided to unify all this garbage into single adapter, so it is easier to configure entire serialization process,
such as initial settings on namespaces/readers/writers/serializers and XSLT transformations on document.
This should give 100% percent coverage on schema formats, but can sometimes lower performance tweak flexibility. But who cares? XML is slow and you know it. Use Json.
