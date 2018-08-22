// The MIT License (MIT)
//
// Copyright (c) 2018 Alex Parker
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Nakama.TinyJson
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;

    // Really simple JSON writer
    // - Outputs JSON structures from an object
    // - Really simple API (new List<int> { 1, 2, 3 }).ToJson() == "[1,2,3]"
    // - Will only output public fields and property getters on objects
    public static class JsonWriter
    {
        public static string ToJson(this object item)
        {
            var stringBuilder = new StringBuilder();
            AppendValue(stringBuilder, item);
            return stringBuilder.ToString();
        }

        private static void AppendValue(StringBuilder stringBuilder, object item)
        {
            if (item == null)
            {
                stringBuilder.Append("null");
                return;
            }

            var type = item.GetType();
            if (type == typeof(string))
            {
                stringBuilder.Append('"');
                var str = (string) item;
                foreach (var t in str)
                    if (t < ' ' || t == '"' || t == '\\')
                    {
                        stringBuilder.Append('\\');
                        var j = "\"\\\n\r\t\b\f".IndexOf(t);
                        if (j >= 0)
                            stringBuilder.Append("\"\\nrtbf"[j]);
                        else
                            stringBuilder.AppendFormat("u{0:X4}", (uint) t);
                    }
                    else
                        stringBuilder.Append(t);

                stringBuilder.Append('"');
            }
            else if (type == typeof(byte) || type == typeof(int))
            {
                stringBuilder.Append(item.ToString());
            }
            else if (type == typeof(float))
            {
                stringBuilder.Append(((float) item).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (type == typeof(double))
            {
                stringBuilder.Append(((double) item).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (type == typeof(bool))
            {
                stringBuilder.Append((bool) item ? "true" : "false");
            }
            else if (item is IList)
            {
                stringBuilder.Append('[');
                var isFirst = true;
                var list = (IList) item;
                foreach (var t in list)
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        stringBuilder.Append(',');
                    AppendValue(stringBuilder, t);
                }

                stringBuilder.Append(']');
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var keyType = type.GetGenericArguments()[0];

                //Refuse to output dictionary keys that aren't of type string
                if (keyType != typeof(string))
                {
                    stringBuilder.Append("{}");
                    return;
                }

                stringBuilder.Append('{');
                var dict = item as IDictionary;
                var isFirst = true;
                foreach (var key in dict.Keys)
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        stringBuilder.Append(',');
                    stringBuilder.Append('\"');
                    stringBuilder.Append((string) key);
                    stringBuilder.Append("\":");
                    AppendValue(stringBuilder, dict[key]);
                }

                stringBuilder.Append('}');
            }
            else
            {
                stringBuilder.Append('{');

                var isFirst = true;

                foreach (var fieldInfo in ReflectionCacher.GetCachedFieldInfosForType(type))
                {
                    var value = fieldInfo.GetValue(item);
                    if (value == null) continue;
                    if (isFirst)
                        isFirst = false;
                    else
                        stringBuilder.Append(',');
                    stringBuilder.Append('\"');
                    stringBuilder.Append(ReflectionCacher.GetCachedMemberInfoName(fieldInfo));
                    stringBuilder.Append("\":");
                    AppendValue(stringBuilder, value);
                }

                foreach (var propertyInfo in ReflectionCacher.GetCachedPropertInfosForType(type))
                {
                    var value = propertyInfo.GetValue(item);
                    if (value == null) continue;
                    if (isFirst)
                        isFirst = false;
                    else
                        stringBuilder.Append(',');
                    stringBuilder.Append('\"');
                    stringBuilder.Append(ReflectionCacher.GetCachedMemberInfoName(propertyInfo));
                    stringBuilder.Append("\":");
                    AppendValue(stringBuilder, value);
                }

                stringBuilder.Append('}');
            }
        }

        private static string GetMemberName(MemberInfo member)
        {
            var dataMemberAttribute = member.GetCustomAttribute<DataMemberAttribute>();
            if (dataMemberAttribute != null && dataMemberAttribute.IsNameSetExplicitly)
                return dataMemberAttribute.Name;
            return member.Name;
        }

        private static class ReflectionCacher
        {
            private static ConcurrentDictionary<Type, ConcurrentBag<FieldInfo>> _cachedFieldInfo = new ConcurrentDictionary<Type, ConcurrentBag<FieldInfo>>();
            private static ConcurrentDictionary<Type, ConcurrentBag<PropertyInfo>> _cachedPropertyInfo = new ConcurrentDictionary<Type, ConcurrentBag<PropertyInfo>>();
            private static ConcurrentDictionary<MemberInfo, string> _cachedMemberNames = new ConcurrentDictionary<MemberInfo, string>();

            public static ConcurrentBag<FieldInfo> GetCachedFieldInfosForType(Type t)
            {
                ConcurrentBag<FieldInfo> output;
                if (_cachedFieldInfo.TryGetValue(t, out output))
                {
                    return output;
                }
                else
                {
                    ConcurrentBag<FieldInfo> fieldInfo = new ConcurrentBag<FieldInfo>(t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy).Where(a => a.GetCustomAttribute<IgnoreDataMemberAttribute>() == null));

                    _cachedFieldInfo.TryAdd(t, fieldInfo);

                    foreach (var memberInfo in fieldInfo)
                        _cachedMemberNames.TryAdd(memberInfo, GetMemberName(memberInfo));

                    return fieldInfo;
                }
            }

            public static ConcurrentBag<PropertyInfo> GetCachedPropertInfosForType(Type t)
            {
                ConcurrentBag<PropertyInfo> output;

                if (_cachedPropertyInfo.TryGetValue(t, out output))
                {
                    return output;
                }
                else
                {
                    ConcurrentBag<PropertyInfo> propertyInfo = new ConcurrentBag<PropertyInfo>(t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy).Where(a => a.CanRead && a.GetCustomAttribute<IgnoreDataMemberAttribute>() == null));

                    _cachedPropertyInfo.TryAdd(t, propertyInfo);

                    foreach (var memberInfo in propertyInfo)
                        _cachedMemberNames.TryAdd(memberInfo, GetMemberName(memberInfo));

                    return propertyInfo;
                }
            }

            public static string GetCachedMemberInfoName(MemberInfo member)
            {
                return _cachedMemberNames[member];
            }

            private static string GetMemberName(MemberInfo member)
            {
                var dataMemberAttribute = member.GetCustomAttribute<DataMemberAttribute>();
                if (dataMemberAttribute != null && dataMemberAttribute.IsNameSetExplicitly)
                    return dataMemberAttribute.Name;
                return member.Name;
            }
        }
    }
}
