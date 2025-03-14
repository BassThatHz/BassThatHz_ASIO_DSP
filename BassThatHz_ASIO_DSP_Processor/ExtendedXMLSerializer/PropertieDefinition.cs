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
namespace ExtendedXmlSerialization.Cache
{
    using System;
    using System.Reflection;

    public class PropertieDefinition
    {
        public PropertieDefinition(Type type, PropertyInfo propertyInfo)
        {
            Name = propertyInfo.Name;
            Type = propertyInfo.PropertyType;
            _getter = ObjectAccessors.CreatePropertyGetter(type, propertyInfo.Name);
            _propertySetter = ObjectAccessors.CreatePropertySetter(type, propertyInfo.Name);
        }

        public PropertieDefinition(Type type, FieldInfo fieldInfo)
        {
            Name = fieldInfo.Name;
            Type = fieldInfo.FieldType;
            _getter = ObjectAccessors.CreatePropertyGetter(type, fieldInfo.Name);
            _propertySetter = ObjectAccessors.CreatePropertySetter(type, fieldInfo.Name);
        }

        protected readonly ObjectAccessors.PropertyGetter _getter;
        protected readonly ObjectAccessors.PropertySetter _propertySetter;
        
        public string Name { get; protected set; }
        public Type Type { get; protected set; }

        public object GetValue(object obj)
        {
            return _getter(obj);
        }

        public void SetValue(object obj, object value)
        {
            _propertySetter?.Invoke(obj, value);
        }
    }
}
