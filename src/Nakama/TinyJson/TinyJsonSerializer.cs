/**
 * Copyright 2020 The Nakama Authors
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

namespace Nakama.TinyJson
{
    /// <summary>
    /// This class is an integration between the <see cref="IJsonSerializer.cs"> interface
    /// and TinyJson.
    /// </summary>
    public class TinyJsonSerializer : IJsonSerializer
    {
        public T FromJson<T>(string json)
        {
            return JsonParser.FromJson<T>(json);
        }

        public string ToJson(object obj)
        {
            return JsonWriter.ToJson(obj);
        }
    }
}
