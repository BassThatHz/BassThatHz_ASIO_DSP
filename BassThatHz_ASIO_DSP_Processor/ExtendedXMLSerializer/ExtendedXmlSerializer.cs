﻿// MIT License
// 
// Copyright (c) 2016 Wojciech Nagórski
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#nullable disable
namespace ExtendedXmlSerialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using ExtendedXmlSerialization.Cache;

    /// <summary>
    /// Extended Xml Serializer
    /// </summary>
    public class ExtendedXmlSerializer : IExtendedXmlSerializer
    {
        protected ISerializationToolsFactory _toolsFactory;

        protected readonly Dictionary<string, object> _referencesObjects = new Dictionary<string, object>();
        protected readonly Dictionary<string, object> _reservedReferencesObjects = new Dictionary<string, object>();
        /// <summary>
        /// Creates an instance of <see cref="ExtendedXmlSerializer"/>
        /// </summary>
        public ExtendedXmlSerializer()
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ExtendedXmlSerializer"/>
        /// </summary>
        /// <param name="toolsFactory">The instance of <see cref="ISerializationToolsFactory"/></param>
        public ExtendedXmlSerializer(ISerializationToolsFactory toolsFactory)
        {
            _toolsFactory = toolsFactory;
        }

        /// <summary>
        /// Gets or sets <see cref="ISerializationToolsFactory"/>
        /// </summary>
        public ISerializationToolsFactory SerializationToolsFactory
        {
            get { return _toolsFactory; }
            set { _toolsFactory = value; }
        }

        /// <summary>
        /// Serializes the specified <see cref="T:System.Object" /> and returns xml document in string
        /// </summary>
        /// <param name="o">The <see cref="T:System.Object" /> to serialize. </param>
        /// <returns>xml document in string</returns>
        public string Serialize(object o)
        {
            var def = TypeDefinitionCache.GetDefinition(o.GetType());

            string xml;
            using (var ms = new MemoryStream())
            {
                using (XmlWriter xw = XmlWriter.Create(ms, new XmlWriterSettings
                {
                    NewLineChars = Environment.NewLine,
                    Indent = true,
                    IndentChars = "  ",
                    Encoding = Encoding.UTF8,
                    DoNotEscapeUriAttributes = true
                }))
                {
                    WriteXml(xw, o, def);
                }
                ms.Position = 0;

                using var sr = new StreamReader(ms);
                xml = sr.ReadToEnd();
            }
            _referencesObjects.Clear();
            return xml;
        }

        public void WriteXmlArray(object o, XmlWriter writer, TypeDefinition def, string name)
        {
            writer.WriteStartElement(name ?? def.Name);
            List<string> toWriteReservedObject = new();
            var array =  o as Array;
            var list = o as IEnumerable;
            if (array != null || list != null)
            {
                Type type;
                if (array != null)
                {
                    type = def.Type.GetElementType();
                }
                else
                {
                    type = def.GenericArguments[0];
                }
                var conf = GetConfiguration(type);
                if (conf != null && conf.IsObjectReference)
                {
                    foreach (var item in array ?? list)
                    {
                        var objectId = conf.GetObjectId(item);

                        var key = type.FullName + "_" + objectId;
                        if (!_referencesObjects.ContainsKey(key) && !_reservedReferencesObjects.ContainsKey(key))
                        {
                            toWriteReservedObject.Add(key);
                            _reservedReferencesObjects.Add(key, item);
                        }
                    }
                }
                foreach (var item in array ?? list)
                {
                    var itemDef = TypeDefinitionCache.GetDefinition(item.GetType());
                    var writeReservedObject = false;
                    if (conf != null && conf.IsObjectReference)
                    {
                        var objectId = conf.GetObjectId(item);
                        var key = type.FullName + "_" + objectId;
                        if (toWriteReservedObject.Contains(key))
                        {
                            writeReservedObject = true;
                        }
                    }
                    WriteXml(writer, item, itemDef, writeReservedObject: writeReservedObject);
                }
               
            }
            writer.WriteEndElement();
        }

        /// <summary>
        /// Deserializes the XML document
        /// </summary>
        /// <param name="xml">The XML document</param>
        /// <param name="type">The type of returned object</param>
        /// <returns>deserialized object</returns>
        public object Deserialize(string xml, Type type)
        {
            var def = TypeDefinitionCache.GetDefinition(type);
            XDocument doc = XDocument.Parse(xml);
            var result = ReadXml(doc.Root, def);
            _referencesObjects.Clear();
            return result;
        }

        /// <summary>
        /// Deserializes the XML document
        /// </summary>
        /// <typeparam name="T">The type of returned object</typeparam>
        /// <param name="xml">The XML document</param>
        /// <returns>deserialized object</returns>
        public T Deserialize<T>(string xml)
        {
            return (T)Deserialize(xml, typeof(T));
        }

        public object ReadXml(XElement currentNode, TypeDefinition type, object instance = null)
        {
            if (type.IsPrimitive)
            {
                return PrimitiveValueTools.GetPrimitiveValue(currentNode.Value, type.Type, currentNode.Name.LocalName);
            }

            if (type.IsArray || type.IsEnumerable)
            {
                return ReadXmlArray(currentNode, type);
            }

            if (currentNode == null)
                return null;

            TypeDefinition currentNodeDef = null;
            // Retrieve type from XML (Property can be base type. In xml can be saved inherited object)
            var typeAttribute = currentNode.Attribute("type");
            if (typeAttribute != null)
            {
                var currentNodeType = TypeDefinitionCache.GetType(typeAttribute.Value);
                currentNodeDef = TypeDefinitionCache.GetDefinition(currentNodeType);
            }
            // If xml does not contain type get property type
            currentNodeDef ??= type;

            // Get configuration for type
            var configuration = GetConfiguration(currentNodeDef.Type);
            if (configuration != null)
            {
                // Run migrator if exists
                if (configuration.Version > 0)
                {
                    configuration.Map(currentNodeDef.Type, currentNode);
                }
                // run custom serializer if exists
                if (configuration.IsCustomSerializer)
                {
                    return configuration.ReadObject(currentNode);
                }
            }
            
            // Create new instance if not exists
            var currentObject = instance ?? currentNodeDef.ObjectActivator();

            if (configuration != null)
            {
                if (configuration.IsObjectReference)
                {
                    string refId = currentNode.Attribute("ref")?.Value;
                    if (!string.IsNullOrEmpty(refId))
                    {
                        var key = currentNodeDef.FullName + "_" + refId;
                        if (_referencesObjects.ContainsKey(key))
                        {
                            return _referencesObjects[key];
                        }
                        _referencesObjects.Add(key, currentObject);
                    }
                    string objectId = currentNode.Attribute("id")?.Value;
                    if (!string.IsNullOrEmpty(objectId))
                    {
                        var key = currentNodeDef.FullName + "_" + objectId;
                        if (_referencesObjects.ContainsKey(key))
                        {
                            currentObject = _referencesObjects[key];
                        }
                        else
                        {
                            _referencesObjects.Add(key, currentObject);
                        }
                    }
                }
            }

            // Read all elements
            foreach (var xElement in currentNode.Elements())
            {
                var localName = xElement.Name.LocalName;
                var value = xElement.Value;
                var propertyInfo = type.GetProperty(localName);
                if (propertyInfo == null)
                {
                    throw new InvalidOperationException("Missing property " + currentNode.Name.LocalName + "\\" + localName);
                }
                var propertyDef = TypeDefinitionCache.GetDefinition(propertyInfo.Type);
                if (xElement.HasAttributes && xElement.Attribute("type") != null)
                {
                    // If type of property is saved in xml, we need check type of object actual assigned to property. There may be a base type. 
                    Type targetType = TypeDefinitionCache.GetType(xElement.Attribute("type").Value);
                    var targetTypeDef = TypeDefinitionCache.GetDefinition(targetType);
                    var obj = propertyInfo.GetValue(currentObject);
                    if (obj == null || obj.GetType() != targetType)
                    {
                        obj = targetTypeDef.ObjectActivator();
                    }
                    var obj2 = ReadXml(xElement, targetTypeDef, obj);
                    propertyInfo.SetValue(currentObject, obj2);
                }
                else if (propertyDef.IsObjectToSerialize || propertyDef.IsArray || propertyDef.IsEnumerable)
                {
                    //If xml does not contain type but we known that it is object
                    var obj = propertyInfo.GetValue(currentObject);
                    object obj2;
                    if (propertyDef.IsArray)
                    {
                        obj2 = ReadXmlArray(xElement, propertyDef, obj);
                    }
                    else
                    {
                        obj2 = ReadXml(xElement, propertyDef, obj);
                    }
                    propertyInfo.SetValue(currentObject, obj2);
                }
                else
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }
                    object primitive = PrimitiveValueTools.GetPrimitiveValue(value, propertyInfo.Type, xElement.Name.LocalName);
                    propertyInfo.SetValue(currentObject, primitive);
                }
            }
            return currentObject;
        }

        public object ReadXmlArray(XElement currentNode, TypeDefinition type, object instance = null)
        {
            int arrayCount = currentNode.Elements().Count();
            var elements = currentNode.Elements().ToArray();

            object list = null;
            Array array = null;
            if (type.IsArray)
            {
                array = (Array) instance ?? Array.CreateInstance(type.Type.GetElementType(), arrayCount);
            }
            else
            {
                list = instance ?? type.ObjectActivator();
            }
            for (int i = 0; i < arrayCount; i++)
            {
                var element = elements[i];
                TypeDefinition cd = null;
                var ta = element.Attribute("type");
                if (ta != null)
                {
                    var currentNodeType = TypeDefinitionCache.GetType(ta.Value);
                    cd = TypeDefinitionCache.GetDefinition(currentNodeType);
                }
                if (type.IsArray)
                {
                    cd ??= TypeDefinitionCache.GetDefinition(type.Type.GetElementType());
                    array?.SetValue(ReadXml(element, cd), i);
                }
                else
                {
                    cd ??= TypeDefinitionCache.GetDefinition(type.GenericArguments[0]);
                    type.MethodAddToList(list, ReadXml(element, cd));
                }
            }
            if (type.IsArray)
            {
                return array;
            }
            return list;
        }

        public static void WriteXmlPrimitive(object o, XmlWriter xw, TypeDefinition def, string name = null)
        {
            xw.WriteStartElement(name ?? def.PrimitiveName);
            xw.WriteString(PrimitiveValueTools.SetPrimitiveValue(o, def.Type));
            xw.WriteEndElement();
        }

        public void WriteXml(XmlWriter writer, object o, TypeDefinition type, string name = null, bool writeReservedObject = false)
        {
            try
            {
                if (type.IsPrimitive)
                {
                    WriteXmlPrimitive(o, writer, type, name);
                    return;
                }
                if (type.IsArray || type.IsEnumerable)
                {
                    WriteXmlArray(o, writer, type, name);
                    return;
                }
                writer.WriteStartElement(name ?? type.Name);
                writer.WriteAttributeString("type", type.FullName);

                // Get configuration for type
                var configuration = GetConfiguration(type.Type);

                if (configuration != null)
                {
                    if (configuration.IsObjectReference)
                    {
                        var objectId = configuration.GetObjectId(o);

                        var key = type.FullName + "_" + objectId;
                        if (writeReservedObject && _reservedReferencesObjects.ContainsKey(key))
                        {
                            _ = _reservedReferencesObjects.Remove(key);
                        }
                        else if (_referencesObjects.ContainsKey(key) || _reservedReferencesObjects.ContainsKey(key))
                        {
                            writer.WriteAttributeString("ref", objectId);
                            writer.WriteEndElement();
                            return;
                        }
                        writer.WriteAttributeString("id", objectId);
                        _referencesObjects.Add(key, o);
                    }

                    if (configuration.Version > 0)
                    {
                        writer.WriteAttributeString("serializeVersion",
                            configuration.Version.ToString(CultureInfo.InvariantCulture));
                    }
                    if (configuration.IsCustomSerializer)
                    {
                        configuration.WriteObject(writer, o);
                        writer.WriteEndElement();
                        return;
                    }
                }

                var properties = type.Properties;
                foreach (var propertyInfo in properties)
                {
                    var propertyValue = propertyInfo.GetValue(o);
                    if (propertyValue == null)
                        continue;

                    var defType = TypeDefinitionCache.GetDefinition(propertyValue.GetType());

                    if (defType.IsObjectToSerialize || defType.IsArray || defType.IsEnumerable)
                    {
                        WriteXml(writer, propertyValue, defType, propertyInfo.Name);
                    }
                    else if (defType.IsEnum)
                    {
                        writer.WriteStartElement(propertyInfo.Name);
                        writer.WriteString(propertyValue.ToString());
                        writer.WriteEndElement();
                    }
                    else
                    {
                        WriteXmlPrimitive(propertyValue, writer, defType, propertyInfo.Name);
                    }
                }
                writer.WriteEndElement();
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        protected IExtendedXmlSerializerConfig GetConfiguration(Type type)
        {
            return _toolsFactory?.GetConfiguration(type);
        }
    }
}
