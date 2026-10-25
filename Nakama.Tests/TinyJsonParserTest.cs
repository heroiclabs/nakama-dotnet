/**
 * Copyright 2018 The Nakama Authors
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

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Nakama.TinyJson;
using Xunit;

namespace Nakama.Tests
{
    public class TinyJsonParserTest
    {
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public void FromJson_JsonInput_Parsed()
        {
            const string json = @"{""some_val"": ""val1"", ""nested"": [{""another_val"": ""val2""}]}";
            ITestObject result = json.FromJson<TestObject>();

            Assert.Equal("val1", result.SomeVal);
            Assert.Equal("val2", result.Nested.First().AnotherVal);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public void FromJson_JsonInput_NumberToString()
        {
            const string json = @"{""key"":12345}";
            var obj = json.FromJson<Dictionary<string, string>>();
            
            Assert.Equal("12345", obj["key"]);
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public void FromJson_JsonInput_SingleDigitNumberToString()
        {
            const string json = @"{""key"":1}";
            var obj = json.FromJson<Dictionary<string, string>>();
            
            Assert.Equal("1", obj["key"]);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public void FromJson_JsonInput_StringToString()
        {
            const string json = @"{""key"":""12345""}";
            var obj = json.FromJson<Dictionary<string, string>>();
            
            Assert.Equal("12345", obj["key"]);
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public void ToJson_LongToUnquotedJson()
        {
            var obj = new Dictionary<string, long>();
            obj["key"] = 1234567891234;
            var json = obj.ToJson();
            
            Assert.Equal("{\"key\":1234567891234}", json);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public void FromJson_JsonInput_ParsedTwice()
        {
            const string json1 = @"{""some_val"": ""val1"", ""nested"": [{""another_val"": ""val2""}]}";
            ITestObject result1 = json1.FromJson<TestObject>();
            const string json2 = @"{""some_val"": ""val1"", ""nested"": [{""another_val"": ""val2""}]}";
            ITestObject result2 = json2.FromJson<TestObject>();

            Assert.Equal(result1.SomeVal, result2.SomeVal);
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public void FromJson_JsonInput_ParseSingleQuotesAsString()
        {
            const string json = @"{""key"":'foo'}";
            var obj = json.FromJson<Dictionary<string, string>>();
            
            Assert.Equal("foo", obj["key"]);
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public void FromJson_JsonInput_ParseSingleQuotesAsStringInArray()
        {
            const string json = @"{""key"":['foo', 'bar']}";
            var obj = json.FromJson<Dictionary<string, string[]>>();
            
            Assert.Equal(new [] { "foo", "bar" }, obj["key"]);
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public void FromJson_JsonInput_ParseBool()
        {
            const string json = @"{""key"":true}";
            var obj = json.FromJson<Dictionary<string, bool>>();
            
            Assert.Equal(true, obj["key"]);
        }

        [Fact]
        public void ToJson_DictionaryKeysWithQuotesBackslashesAndControlCharacters_ProduceValidJson()
        {
            var payload = new Dictionary<string, object>
            {
                { "say \"hi\"", 1 },
                { "C:\\saves", 2 },
                { "line\nbreak\ttab\u0001", 3 },
                { "outer", new Dictionary<string, object> { { "in\"ner", true } } }
            };

            var json = payload.ToJson();

            using var document = System.Text.Json.JsonDocument.Parse(json);
            Assert.Equal(
                new[] { "say \"hi\"", "C:\\saves", "line\nbreak\ttab\u0001", "outer" },
                document.RootElement.EnumerateObject().Select(p => p.Name).ToArray());
            Assert.Equal(
                new[] { "in\"ner" },
                document.RootElement.GetProperty("outer").EnumerateObject().Select(p => p.Name).ToArray());
        }

        [Fact]
        public void FromJson_EscapedDictionaryKeys_RoundTripToOriginalKeys()
        {
            var payload = new Dictionary<string, object>
            {
                { "say \"hi\"", 1 },
                { "C:\\saves", 2 },
                { "line\nbreak\ttab\u0001", 3 },
                { "outer", new Dictionary<string, object> { { "in\"ner", true } } }
            };

            var parsed = payload.ToJson().FromJson<Dictionary<string, object>>();

            Assert.Equal(
                new[] { "C:\\saves", "line\nbreak\ttab\u0001", "outer", "say \"hi\"" },
                parsed.Keys.OrderBy(k => k, System.StringComparer.Ordinal).ToArray());
            var nested = Assert.IsType<Dictionary<string, object>>(parsed["outer"]);
            Assert.Equal(new[] { "in\"ner" }, nested.Keys.ToArray());
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public void FromJson_DeeplyNestedArrays_Throws()
        {
            var json = new string('[', 10_000) + new string(']', 10_000);

            Assert.Throws<System.FormatException>(() => json.FromJson<object>());
            Assert.Throws<System.FormatException>(() => json.FromJson<List<List<object>>>());
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public void FromJson_DeeplyNestedObjects_Throws()
        {
            var json = string.Concat(Enumerable.Repeat("{\"a\":", 10_000)) + "1" + new string('}', 10_000);

            Assert.Throws<System.FormatException>(() => json.FromJson<object>());
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public void FromJson_ModerateNesting_Parsed()
        {
            const int depth = 100;
            var json = new string('[', depth) + new string(']', depth);

            Assert.IsType<List<object>>(json.FromJson<object>());
        }
    }

    public interface ITestObject
    {
        string SomeVal { get; }

        IEnumerable<INestedTestObject> Nested { get; }
    }

    internal class TestObject : ITestObject
    {
        [DataMember(Name="some_val")]
        public string SomeVal { get; set; }

        public IEnumerable<INestedTestObject> Nested => _nested ?? new List<NestedTestObject>(0);
        [DataMember(Name="nested")]
        // ReSharper disable once InconsistentNaming
        public List<NestedTestObject> _nested { get; set; }
    }

    public interface INestedTestObject
    {
        string AnotherVal { get; }
    }

    internal class NestedTestObject : INestedTestObject
    {
        [DataMember(Name="another_val")]
        public string AnotherVal { get; set; }
    }
}
