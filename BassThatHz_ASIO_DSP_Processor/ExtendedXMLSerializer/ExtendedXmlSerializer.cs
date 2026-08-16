// MIT License
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
        /// Shared, immutable writer settings. These were previously rebuilt on every Serialize()
        /// call - and Serialize() backs CommonFunctions.DeepClone&lt;T&gt;, so it is not a one-time
        /// path. XmlWriter.Create never mutates the instance it is handed (it clones internally
        /// when it needs a writable copy), so a single shared instance is safe to reuse and to
        /// share across threads.
        /// </summary>
        protected static readonly XmlWriterSettings DefaultWriterSettings = new XmlWriterSettings
        {
            NewLineChars = Environment.NewLine,
            Indent = true,
            IndentChars = "  ",
            Encoding = Encoding.UTF8,
            DoNotEscapeUriAttributes = true
        };
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
                using (XmlWriter xw = XmlWriter.Create(ms, DefaultWriterSettings))
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
            // Allocation: this list used to be built unconditionally for every array/list element
            // written, even though it is only ever touched on the object-reference path. It is now
            // created lazily so the common (no IsObjectReference configuration) case allocates
            // nothing here at all.
            List<string> toWriteReservedObject = null;
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
                    toWriteReservedObject = new List<string>();
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
                        if (toWriteReservedObject != null && toWriteReservedObject.Contains(key))
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
                        if (_referencesObjects.TryGetValue(key, out var Local_Existing))
                        {
                            return Local_Existing;
                        }
                        _referencesObjects.Add(key, currentObject);
                    }
                    string objectId = currentNode.Attribute("id")?.Value;
                    if (!string.IsNullOrEmpty(objectId))
                    {
                        var key = currentNodeDef.FullName + "_" + objectId;
                        if (_referencesObjects.TryGetValue(key, out var Local_Existing))
                        {
                            currentObject = Local_Existing;
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
                // XElement.Attribute(name) is a linear scan of the attribute list; it was being
                // walked twice per element. Resolve it once.
                var Local_TypeAttribute = xElement.HasAttributes ? xElement.Attribute("type") : null;
                if (Local_TypeAttribute != null)
                {
                    // If type of property is saved in xml, we need check type of object actual assigned to property. There may be a base type.
                    Type targetType = TypeDefinitionCache.GetType(Local_TypeAttribute.Value);
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
                        // The element IS present (this loop only ever sees present elements), it
                        // just has empty text. For a string property that means the value really
                        // was "" and must be restored - skipping it silently left the property at
                        // its default and lost data across a save/load round trip.
                        // Other primitives (int, double, bool, enums, ...) cannot be parsed from an
                        // empty string, so those are still skipped and keep their default.
                        if (propertyInfo.Type == typeof(string))
                        {
                            propertyInfo.SetValue(currentObject, string.Empty);
                        }
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
            // Allocation: this used to enumerate currentNode.Elements() TWICE - once for Count()
            // and again for ToArray() - allocating two LINQ iterators and walking the whole child
            // list twice. Materialise once and take the length from the array.
            var elements = currentNode.Elements().ToArray();
            int arrayCount = elements.Length;

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
            // NOTE: this method deliberately does NOT catch exceptions. It used to be wrapped in
            // `catch (Exception ex) { _ = ex; }`, which silently abandoned serialization part-way
            // through and handed the caller a TRUNCATED config file that looked like a successful
            // save. This class persists the user's whole DSP configuration (and backs
            // CommonFunctions.DeepClone<T>), so a failure here must surface to the caller.
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

        protected IExtendedXmlSerializerConfig GetConfiguration(Type type)
        {
            return _toolsFactory?.GetConfiguration(type);
        }
    }
}
