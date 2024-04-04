﻿using System;
using System.Xml;

namespace Eocron.Serialization.Xml.XmlLegacy.Serializer
{
    public interface IXmlSerializerAdapter
    {
        object ReadObject(XmlReader reader, Type type);
        void WriteObject(XmlWriter writer, Type type, object content);
    }
}