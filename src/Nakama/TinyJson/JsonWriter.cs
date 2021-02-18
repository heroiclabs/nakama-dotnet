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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Nakama.TinyJson
{
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
            if (type == typeof(string) || type == typeof(char))
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
            else if (type == typeof(byte) || type == typeof(sbyte))
            {
                stringBuilder.Append(item);
            }
            else if (type == typeof(short) || type == typeof(ushort))
            {
                stringBuilder.Append(item);
            }
            else if (type == typeof(int) || type == typeof(uint))
            {
                stringBuilder.Append(item);
            }
            else if (type == typeof(long) || type == typeof(ulong))
            {
                stringBuilder.Append(item);
            }
            else if (type == typeof(float))
            {
                stringBuilder.Append(((float) item).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (type == typeof(double))
            {
                stringBuilder.Append(((double) item).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (type == typeof (decimal)) {
                stringBuilder.Append (((decimal) item).ToString (System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (type == typeof(bool))
            {
                stringBuilder.Append((bool) item ? "true" : "false");
            }
            else if (type == typeof(System.DateTime))
            {
                stringBuilder.Append("\"" + ((DateTime)item).ToString("yyyy-MM-ddTHH:mm:ss.fffffffK") + "\"");
            }
            else if (type == typeof(System.Guid))
            {
                stringBuilder.Append("\"" + item + "\"");
            }
            else if (type.IsEnum)
            {
                stringBuilder.Append('"');
                stringBuilder.Append(item);
                stringBuilder.Append('"');
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
                var fieldInfos =
                    type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                foreach (var t in fieldInfos)
                {
                    if (t.IsDefined(typeof(IgnoreDataMemberAttribute), true))
                        continue;

                    var value = t.GetValue(item);
                    if (value == null) continue;
                    if (isFirst)
                        isFirst = false;
                    else
                        stringBuilder.Append(',');
                    stringBuilder.Append('\"');
                    stringBuilder.Append(GetMemberName(t));
                    stringBuilder.Append("\":");
                    AppendValue(stringBuilder, value);
                }

                var propertyInfo =
                    type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                foreach (var t in propertyInfo)
                {
                    if (!t.CanRead || t.IsDefined(typeof(IgnoreDataMemberAttribute), true))
                        continue;

                    var value = t.GetValue(item, null);
                    if (value == null) continue;
                    if (isFirst)
                        isFirst = false;
                    else
                        stringBuilder.Append(',');
                    stringBuilder.Append('\"');
                    stringBuilder.Append(GetMemberName(t));
                    stringBuilder.Append("\":");
                    AppendValue(stringBuilder, value);
                }

                stringBuilder.Append('}');
            }
        }

        private static string GetMemberName(MemberInfo member)
        {
            if (!member.IsDefined(typeof(DataMemberAttribute), true)) return member.Name;
            var dataMemberAttribute =
                (DataMemberAttribute) Attribute.GetCustomAttribute(member, typeof(DataMemberAttribute), true);
            return !string.IsNullOrEmpty(dataMemberAttribute.Name) ? dataMemberAttribute.Name : member.Name;
        }
    }
}
