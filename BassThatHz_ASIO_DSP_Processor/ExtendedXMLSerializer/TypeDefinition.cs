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
namespace ExtendedXmlSerialization.Cache
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Serialization;

    public struct Test
    {
        public int X;
    }

    public class TypeDefinition
    {

        public TypeDefinition(Type type)
        {
            Type = type;
            Name = type.Name;

            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType)
            {
                Type[] types = type.GetGenericArguments();
                Name = Name.Replace("`" + types.Length, "Of" + string.Join("", types.Select(p => p.Name)));
            }


            FullName = type.FullName;

            IsPrimitive = IsTypPrimitive(type);
            if (IsPrimitive)
            {
                PrimitiveName = GetPrimitiveName(type);
            }

            IsArray = typeInfo.IsArray;

            if (!IsPrimitive && typeof(IEnumerable).IsAssignableFrom(type))
            {

                IsEnumerable = true;
            }

            if (IsEnumerable)
            {
                var elementType = type.GetElementType();
                if (elementType != null)
                {
                    Name = "ArrayOf" + elementType?.Name;
                }
                else
                {
                    Type[] types = type.GetGenericArguments();
                    Name = "ArrayOf" + string.Join("", types.Select(p => p.Name));
                }
                if (typeInfo.IsGenericType)
                {
                    GenericArguments = type.GetGenericArguments();
                    MethodAddToList = ObjectAccessors.CreateMethodAdd(type);
                }
            }

            IsClass = !typeInfo.IsPrimitive && !typeInfo.IsValueType && !IsPrimitive && !typeInfo.IsEnum &&
                      !(type == typeof(string));

            IsObjectToSerialize = // !typeInfo.IsPrimitive && !typeInfo.IsValueType &&
                !IsPrimitive &&
                !typeInfo.IsEnum && !(type == typeof(string)) &&
                //not generic or generic but not List<> and Set<>
                (!typeInfo.IsGenericType ||
                 typeInfo.IsGenericType && !typeof(IEnumerable).IsAssignableFrom(type));
            if (IsObjectToSerialize)
            {
                Properties = GetPropertieToSerialze(type);
            }
            IsEnum = typeInfo.IsEnum;

            ObjectActivator = ObjectAccessors.CreateObjectActivator(type, IsPrimitive);
        }

        public ObjectAccessors.AddItemToCollection MethodAddToList { get; set; }


        protected static List<PropertieDefinition> GetPropertieToSerialze(Type type)
        {
            var result = new List<PropertieDefinition>();
            var properties = type.GetProperties();
            foreach (PropertyInfo propertyInfo in properties)
            {
                // Skip indexers
                if (propertyInfo.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                if (IsIgnoredMember(propertyInfo))
                {
                    continue;
                }

                // Include property if it's writable (has a public setter) OR if it's a read-only
                // property that exposes a mutable collection via its getter (e.g. List<T> Filters { get; }).
                bool include = false;
                if (propertyInfo.CanWrite && propertyInfo.GetSetMethod(true) != null && propertyInfo.GetSetMethod(true).IsPublic)
                {
                    include = true;
                }
                else if (propertyInfo.CanRead && typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyInfo.PropertyType) && propertyInfo.PropertyType != typeof(string))
                {
                    include = true;
                }

                if (!include)
                    continue;

                result.Add(new PropertieDefinition(type, propertyInfo));
            }

            var fields = type.GetFields();
            foreach (FieldInfo field in fields)
            {
                if (field.IsLiteral && !field.IsInitOnly)
                {
                    continue;
                }
                if (field.IsInitOnly)
                {
                    continue;
                }
                if (IsIgnoredMember(field))
                {
                    continue;
                }

                result.Add(new PropertieDefinition(type, field));
            }
            return result;
        }

        /// <summary>
        /// Returns true when a member is explicitly marked "do not persist".
        /// </summary>
        /// <param name="member">The property or field being considered for serialization.</param>
        /// <returns>true if the member must be excluded from the serialized output.</returns>
        /// <remarks>
        /// Only <see cref="XmlIgnoreAttribute"/> used to be honored here, so
        /// <see cref="System.Runtime.Serialization.IgnoreDataMemberAttribute"/> - and the
        /// field-level <see cref="NonSerializedAttribute"/> - were silently ignored and the
        /// members carrying them were written into every saved config.
        /// <para>
        /// Every member marked that way in this codebase is COMPUTED RUNTIME STATE, never a user
        /// setting (Limiter.CompressionApplied / PeakValue / IsBrickwall and DEQ.GainApplied are
        /// written by Transform and only read back by the meter UI; MixerInput.ChannelName is
        /// derived from the live ASIO device list), so honoring the attributes loses no user data.
        /// </para>
        /// <para>
        /// Configs written by older builds DO still contain those elements, and
        /// <c>ExtendedXmlSerializer.ReadXml</c> throws on an element it cannot map to a member,
        /// so <c>CommonFunctions.RemoveDeprecatedXMLInputTags</c> /
        /// <c>RemoveDeprecatedXMLOutputTags</c> strip them before deserialization.
        /// </para>
        /// <para>
        /// <see cref="NonSerializedAttribute"/> is only legal on fields; testing for it on
        /// properties as well is harmless and keeps the rule in one place. It is a pseudo custom
        /// attribute (a metadata flag), but the .NET runtime reconstitutes it from
        /// <c>GetCustomAttributes</c>, so no <c>FieldInfo.IsNotSerialized</c> fallback is needed -
        /// and that property is obsolete under SYSLIB0050. Covered by
        /// <c>Test_IgnoreAttributes.Serializer_HonorsEveryIgnoreAttribute_OnFieldsAndProperties</c>.
        /// </para>
        /// </remarks>
        protected static bool IsIgnoredMember(MemberInfo member)
        {
            // Allocation: the previous `GetCustomAttributes(false).Any(a => a is XmlIgnoreAttribute)`
            // allocated a delegate per member. TypeDefinition instances are cached per type, so this
            // runs once per type, but the indexed loop is both allocation-free and clearer.
            var Local_Attributes = member.GetCustomAttributes(false);
            for (int Local_i = 0; Local_i < Local_Attributes.Length; Local_i++)
            {
                var Local_Attribute = Local_Attributes[Local_i];
                if (Local_Attribute is XmlIgnoreAttribute
                    || Local_Attribute is System.Runtime.Serialization.IgnoreDataMemberAttribute
                    || Local_Attribute is NonSerializedAttribute)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsPrimitive { get; protected set; }
        public bool IsArray { get; protected set; }
        public bool IsEnumerable { get; protected set; }
        public Type[] GenericArguments { get; set; }

        public List<PropertieDefinition> Properties { get; protected set; }
        public Type Type { get; protected set; }
        public string Name { get; protected set; }
        public string FullName { get; protected set; }
        public bool IsObjectToSerialize { get; protected set; }
        public bool IsClass { get; protected set; }
        public bool IsEnum { get; protected set; }

        public string PrimitiveName { get; protected set; }
        public ObjectAccessors.ObjectActivator ObjectActivator { get; protected set; }

        public PropertieDefinition GetProperty(string name)
        {
            // Allocation: this is called once per XML element per deserialize (and therefore on
            // every CommonFunctions.DeepClone). The previous Properties.FirstOrDefault(p => p.Name
            // == name) captured `name`, so it allocated a closure object AND a delegate on every
            // single call. An indexed loop over the (small) property list is allocation-free and
            // returns exactly the same "first match, else null" result.
            var Local_Properties = this.Properties;
            if (Local_Properties == null)
                return null;

            for (int Local_i = 0; Local_i < Local_Properties.Count; Local_i++)
            {
                var Local_Property = Local_Properties[Local_i];
                if (Local_Property.Name == name)
                    return Local_Property;
            }
            return null;
        }

        protected static bool IsTypPrimitive(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    if (type == typeof(Guid))
                    {
                        return true;
                    }
                    if (type == typeof(TimeSpan))
                    {
                        return true;
                    }
                    if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return true;
                    }
                    return false;
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                case TypeCode.String:
                    return true;
                default:
                    return false;
            }
        }

        protected static string GetPrimitiveName(Type type)
        {

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
            }
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return "boolean";
                case TypeCode.Char:
                    return "char";
                case TypeCode.SByte:
                    return "byte";
                case TypeCode.Byte:
                    return "unsignedByte";
                case TypeCode.Int16:
                    return "short";
                case TypeCode.UInt16:
                    return "unsignedShort";
                case TypeCode.Int32:
                    return "int";
                case TypeCode.UInt32:
                    return "unsignedInt";
                case TypeCode.Int64:
                    return "long";
                case TypeCode.UInt64:
                    return "unsignedLong";
                case TypeCode.Single:
                    return "float";
                case TypeCode.Double:
                    return "double";
                case TypeCode.Decimal:
                    return "decimal";
                case TypeCode.DateTime:
                    return "dateTime";
                case TypeCode.String:
                    return "string";
                default:
                    if (type == typeof(Guid))
                    {
                        return "guid";
                    }
                    if (type == typeof(TimeSpan))
                    {
                        return "TimeSpan";
                    }

                    throw new InvalidOperationException("Unknown primitive type " + type.FullName);
            }
        }
    }
}
