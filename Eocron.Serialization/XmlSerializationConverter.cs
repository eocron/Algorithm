using System;
using System.Globalization;
using System.IO;
using YAXLib;
using YAXLib.Enums;
using YAXLib.Options;

namespace Eocron.Serialization
{
    public sealed class XmlSerializationConverter : ISerializationConverter
    {
        public static SerializerOptions DefaultSerializerOptions = new SerializerOptions()
        {
            SerializationOptions = YAXSerializationOptions.ThrowUponSerializingCyclingReferences |
                                   YAXSerializationOptions.DontSerializeNullObjects |
                                   YAXSerializationOptions.SuppressMetadataAttributes,
            Culture = CultureInfo.InvariantCulture,
            ExceptionBehavior = YAXExceptionTypes.Error,
            ExceptionHandlingPolicies = YAXExceptionHandlingPolicies.ThrowWarningsAndErrors
        };

        private readonly SerializerOptions _options;

        public XmlSerializationConverter(SerializerOptions options = null)
        {
            _options = options ?? DefaultSerializerOptions;
        }

        public object DeserializeFrom(Type type, StreamReader sourceStream)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            return GetSerializer(type).Deserialize(sourceStream);
        }

        private YAXSerializer GetSerializer(Type type)
        {
            return _options == null ? new YAXSerializer(type) : new YAXSerializer(type, _options);
        }

        public void SerializeTo(Type type, object obj, StreamWriter targetStream)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (targetStream == null)
                throw new ArgumentNullException(nameof(targetStream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            GetSerializer(type).Serialize(obj, targetStream);
        }
    }
}