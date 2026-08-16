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
            // Only create a setter when the property is writable. Some properties are read-only
            // but expose a mutable collection via their getter (e.g. public List<T> Filters { get; }).
            // In that case we should not attempt to build an assign setter (it would fail),
            // instead we leave _propertySetter null and handle merging in SetValue.
            if (propertyInfo.CanWrite && propertyInfo.GetSetMethod(true) != null && propertyInfo.GetSetMethod(true).IsPublic)
            {
                _propertySetter = ObjectAccessors.CreatePropertySetter(type, propertyInfo.Name);
            }
            else
            {
                _propertySetter = null;
            }
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

        /// <summary>
        /// Immutable (target type -&gt; Add method) pair cached for the read-only-collection merge
        /// path in <see cref="SetValue"/>. It is stored as a single object reference so publishing
        /// it is one atomic write - a racing thread either sees the whole previous entry or the
        /// whole new one, never a torn type/method pair. A miss simply re-resolves, so a benign
        /// race costs one extra reflection lookup and nothing else.
        /// </summary>
        protected sealed class AddMethodCacheEntry
        {
            public AddMethodCacheEntry(Type targetType, MethodInfo? addMethod)
            {
                this.TargetType = targetType;
                this.AddMethod = addMethod;
            }

            public readonly Type TargetType;
            public readonly MethodInfo? AddMethod;
        }

        protected AddMethodCacheEntry? _addMethodCache;


        public string Name { get; protected set; }
        public Type Type { get; protected set; }

        public object GetValue(object obj)
        {
            return _getter(obj);
        }

        public void SetValue(object obj, object value)
        {
            if (_propertySetter != null)
            {
                _propertySetter.Invoke(obj, value);
                return;
            }

            // If there's no setter, try to merge into the existing collection instance exposed by the getter.
            if (value == null)
                return;

            var existing = _getter(obj);
            if (existing == null)
                return;

            var enumerable = value as System.Collections.IEnumerable;
            if (enumerable == null)
                return;

            // Allocation: this used to call GetType().GetMethod("Add") on EVERY invocation (a
            // reflection lookup that allocates internally) and then allocate a fresh one-element
            // object[] for EVERY item added. The Add method is now cached per target type, and a
            // single argument buffer is reused for the whole merge - MethodInfo.Invoke copies the
            // arguments in, so reusing the buffer within one call is safe.
            var Local_ExistingType = existing.GetType();
            var Local_Cache = this._addMethodCache;
            MethodInfo? Local_AddMethod;
            if (Local_Cache != null && Local_Cache.TargetType == Local_ExistingType)
            {
                Local_AddMethod = Local_Cache.AddMethod;
            }
            else
            {
                Local_AddMethod = Local_ExistingType.GetMethod("Add");
                this._addMethodCache = new AddMethodCacheEntry(Local_ExistingType, Local_AddMethod);
            }

            if (Local_AddMethod == null)
                return;

            var Local_Args = new object?[1];
            if (enumerable is System.Collections.IList Local_SourceList)
            {
                // Indexed access avoids the boxed enumerator that foreach over the non-generic
                // IEnumerable would allocate for List<T>.
                for (int Local_i = 0; Local_i < Local_SourceList.Count; Local_i++)
                {
                    Local_Args[0] = Local_SourceList[Local_i];
                    _ = Local_AddMethod.Invoke(existing, Local_Args);
                }
                return;
            }

            foreach (var item in enumerable)
            {
                Local_Args[0] = item;
                _ = Local_AddMethod.Invoke(existing, Local_Args);
            }
        }
    }
}
