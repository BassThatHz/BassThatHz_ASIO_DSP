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
namespace ExtendedXmlSerialization.Cache
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    public static class ObjectAccessors
    {
        public delegate object ObjectActivator();

        public delegate object PropertyGetter(object item);

        public delegate void PropertySetter(object item, object value);

        public delegate void AddItemToCollection(object item, object value);

        public static ObjectActivator CreateObjectActivator(Type type, bool isPrimitive)
        {
            var typeInfo = type.GetTypeInfo();
            //if isClass or struct but not abstract, enum or primitive
            if (!isPrimitive && (typeInfo.IsClass || typeInfo.IsValueType) && !typeInfo.IsAbstract && !typeInfo.IsEnum && !typeInfo.IsPrimitive)
            {
                if (typeInfo.IsClass)
                {
                    //class must have constructor
                    var constructor = type.GetConstructor(Type.EmptyTypes);
                    if (constructor == null)
                        return null;
                }

                var newExp = Expression.Convert(Expression.New(type), typeof(object));

                var lambda = Expression.Lambda<ObjectActivator>(newExp);

                return lambda.Compile();
            }
                
            return null;
        }
        
        public static PropertyGetter CreatePropertyGetter(Type type, string propertyName)
        {
            // Object (type object) from witch the data are retrieved
            ParameterExpression itemObject = Expression.Parameter(typeof(object), "item");

            // Object casted to specific type using the operator "as".
            UnaryExpression itemCasted = Expression.Convert(itemObject, type);

            // Property from casted object
            MemberExpression property = Expression.PropertyOrField(itemCasted, propertyName);

            // Because we use this function also for value type we need to add conversion to object
            Expression conversion = Expression.Convert(property, typeof(object));

            LambdaExpression lambda = Expression.Lambda(typeof(PropertyGetter), conversion, itemObject);

            PropertyGetter compiled = (PropertyGetter)lambda.Compile();
            return compiled;
        }

        public static AddItemToCollection CreateMethodAdd(Type type)
        {
            // Object (type object) from witch the data are retrieved
            ParameterExpression itemObject = Expression.Parameter(typeof(object), "item");

            // Object casted to specific type using the operator "as".
            UnaryExpression itemCasted = Expression.Convert(itemObject, type);

            var arguments = type.GetGenericArguments();
            ParameterExpression value = Expression.Parameter(typeof(object), "value");
            Expression paramCasted = Expression.Convert(value, arguments[0]);

            MethodInfo method = type.GetMethod("Add");
            
            Expression conversion = Expression.Call(itemCasted, method, paramCasted);

            LambdaExpression lambda = Expression.Lambda(typeof(AddItemToCollection), conversion, itemObject, value);

            AddItemToCollection compiled = (AddItemToCollection)lambda.Compile();
            return compiled;
        }

        public static PropertySetter CreatePropertySetter(Type type, string propertyName)
        {
            // Object (type object) from witch the data are retrieved
            ParameterExpression itemObject = Expression.Parameter(typeof(object), "item");

            // Object casted to specific type using the operator "as".
            Expression itemCasted = type.GetTypeInfo().IsValueType
                ? Expression.Unbox(itemObject, type)
                : Expression.Convert(itemObject, type);
            // Property from casted object
            MemberExpression property = Expression.PropertyOrField(itemCasted, propertyName);

            // Secound parameter - value to set
            ParameterExpression value = Expression.Parameter(typeof(object), "value");

            // Because we use this function also for value type we need to add conversion to object
            Expression paramCasted = Expression.Convert(value, property.Type);

            // Assign value to property
            BinaryExpression assign = Expression.Assign(property, paramCasted);

            LambdaExpression lambda = Expression.Lambda(typeof(PropertySetter), assign, itemObject, value);

            PropertySetter compiled = (PropertySetter)lambda.Compile();
            return compiled;
        }
    }
}
