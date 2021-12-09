/**
* Copyright 2021 The Nakama Authors
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Nakama.TinyJson;

namespace NakamaSync
{
    public enum SyncEncodingFormat
    {
        Binary,
        Json
    }

    public class SyncEncoding
    {
        private readonly SyncEncodingFormat _format;

        public SyncEncoding(SyncEncodingFormat format = SyncEncodingFormat.Binary)
        {
            _format = format;
        }

        public T Decode<T>(byte[] data)
        {
            if (_format == SyncEncodingFormat.Json)
            {
                return System.Text.Encoding.UTF8.GetString(data).FromJson<T>();
            }

            using (var ms = new MemoryStream(data))
            {
                var formatter = new BinaryFormatter();
                var obj = formatter.Deserialize(ms);

                try
                {
                    var castedObj = (T)obj;
                    return castedObj;
                }
                catch (Exception ex)
                {
                    throw new InvalidCastException(
                        $"Unable to cast object of type {obj.GetType()} as type {typeof(T)}", ex);
                }
            }
        }

        public byte[] Encode(object data)
        {
            if (_format == SyncEncodingFormat.Json)
            {
                return System.Text.Encoding.UTF8.GetBytes(data.ToJson());
            }

            if (!data.GetType().IsSerializable)
            {
                throw new InvalidOperationException($"Type {data.GetType()} is not marked as Serializable");
            }

            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, data);
                return ms.ToArray();
            }
        }
    }
}
