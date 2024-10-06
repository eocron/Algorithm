using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Eocron.Serialization.Tests.Models.XmlLegacy
{
    /// <summary>
    ///     Represents an XML serializable collection of keys and values.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    [Serializable]
    [XmlRoot("dictionary")]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="SerializableDictionary&lt;TKey, TValue&gt;" /> class.
        /// </summary>
        public SerializableDictionary()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="SerializableDictionary&lt;TKey, TValue&gt;" /> class.
        /// </summary>
        /// <param name="info">
        ///     A
        ///     <see cref="T:System.Runtime.Serialization.SerializationInfo" /> object
        ///     containing the information required to serialize the
        ///     <see cref="T:System.Collections.Generic.Dictionary`2" />.
        /// </param>
        /// <param name="context">
        ///     A
        ///     <see cref="T:System.Runtime.Serialization.StreamingContext" /> structure
        ///     containing the source and destination of the serialized stream
        ///     associated with the
        ///     <see cref="T:System.Collections.Generic.Dictionary`2" />.
        /// </param>
#pragma warning disable SYSLIB0051
        protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
#pragma warning restore SYSLIB0051
        {
        }

        /// <summary>
        ///     Gets the XML schema for the XML serialization.
        /// </summary>
        /// <returns>An XML schema for the serialized object.</returns>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        ///     Deserializes the object from XML.
        /// </summary>
        /// <param name="reader">The XML representation of the object.</param>
        public void ReadXml(XmlReader reader)
        {
            var wasEmpty = reader.IsEmptyElement;

            reader.Read();
            if (wasEmpty) return;

            try
            {
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    ReadItem(reader);
                    reader.MoveToContent();
                }
            }
            finally
            {
                reader.ReadEndElement();
            }
        }

        /// <summary>
        ///     Serializes this instance to XML.
        /// </summary>
        /// <param name="writer">The XML writer to serialize to.</param>
        public void WriteXml(XmlWriter writer)
        {
            foreach (var keyValuePair in this) WriteItem(writer, keyValuePair);
        }

        /// <summary>
        ///     Deserializes the dictionary item.
        /// </summary>
        /// <param name="reader">The XML representation of the object.</param>
        private void ReadItem(XmlReader reader)
        {
            reader.ReadStartElement(ItemTagName);
            try
            {
                Add(ReadKey(reader), ReadValue(reader));
            }
            finally
            {
                reader.ReadEndElement();
            }
        }

        /// <summary>
        ///     Deserializes the dictionary item's key.
        /// </summary>
        /// <param name="reader">The XML representation of the object.</param>
        /// <returns>The dictionary item's key.</returns>
        private TKey ReadKey(XmlReader reader)
        {
            reader.ReadStartElement(KeyTagName);
            try
            {
                return (TKey)KeySerializer.Deserialize(reader);
            }
            finally
            {
                reader.ReadEndElement();
            }
        }

        /// <summary>
        ///     Deserializes the dictionary item's value.
        /// </summary>
        /// <param name="reader">The XML representation of the object.</param>
        /// <returns>The dictionary item's value.</returns>
        private TValue ReadValue(XmlReader reader)
        {
            reader.ReadStartElement(ValueTagName);
            try
            {
                return (TValue)ValueSerializer.Deserialize(reader);
            }
            finally
            {
                reader.ReadEndElement();
            }
        }

        /// <summary>
        ///     Serializes the dictionary item.
        /// </summary>
        /// <param name="writer">The XML writer to serialize to.</param>
        /// <param name="keyValuePair">The key/value pair.</param>
        private void WriteItem(XmlWriter writer, KeyValuePair<TKey, TValue> keyValuePair)
        {
            writer.WriteStartElement(ItemTagName);
            try
            {
                WriteKey(writer, keyValuePair.Key);
                WriteValue(writer, keyValuePair.Value);
            }
            finally
            {
                writer.WriteEndElement();
            }
        }

        /// <summary>
        ///     Serializes the dictionary item's key.
        /// </summary>
        /// <param name="writer">The XML writer to serialize to.</param>
        /// <param name="key">The dictionary item's key.</param>
        private void WriteKey(XmlWriter writer, TKey key)
        {
            writer.WriteStartElement(KeyTagName);
            try
            {
                KeySerializer.Serialize(writer, key);
            }
            finally
            {
                writer.WriteEndElement();
            }
        }

        /// <summary>
        ///     Serializes the dictionary item's value.
        /// </summary>
        /// <param name="writer">The XML writer to serialize to.</param>
        /// <param name="value">The dictionary item's value.</param>
        private void WriteValue(XmlWriter writer, TValue value)
        {
            writer.WriteStartElement(ValueTagName);
            try
            {
                ValueSerializer.Serialize(writer, value);
            }
            finally
            {
                writer.WriteEndElement();
            }
        }

        /// <summary>
        ///     Gets the XML tag name for an item.
        /// </summary>
        protected virtual string ItemTagName => DefaultItemTag;

        /// <summary>
        ///     Gets the XML tag name for a key.
        /// </summary>
        protected virtual string KeyTagName => DefaultKeyTag;

        /// <summary>
        ///     Gets the XML tag name for a value.
        /// </summary>
        protected virtual string ValueTagName => DefaultValueTag;

        /// <summary>
        ///     The default XML tag name for an item.
        /// </summary>
        private const string DefaultItemTag = "item";

        /// <summary>
        ///     The default XML tag name for a key.
        /// </summary>
        private const string DefaultKeyTag = "key";

        /// <summary>
        ///     The default XML tag name for a value.
        /// </summary>
        private const string DefaultValueTag = "value";

        /// <summary>
        ///     The XML serializer for the key type.
        /// </summary>
        private static readonly XmlSerializer KeySerializer = new XmlSerializer(typeof(TKey));

        /// <summary>
        ///     The XML serializer for the value type.
        /// </summary>
        private static readonly XmlSerializer ValueSerializer = new XmlSerializer(typeof(TValue));
    }
}