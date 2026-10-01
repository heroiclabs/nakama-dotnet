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
        [Fact]
        public void FromJson_JsonInput_Parsed()
        {
            string json = @"{""some_val"": ""val1"", ""nested"": [{""another_val"": ""val2""}], ""enum_val"": 1}";
            ITestObject result = json.FromJson<TestObject>();

            Assert.Equal("val1", result.SomeVal);
            Assert.Equal("val2", result.Nested.First().AnotherVal);
            Assert.Equal(TestEnum.FieldTwo, result.EnumVal);
            
            json = @"{""some_val"": ""val1"", ""nested"": [{""another_val"": ""val2""}], ""enum_val"": ""FieldTwo""}";
            result = json.FromJson<TestObject>();

            Assert.Equal(TestEnum.FieldTwo, result.EnumVal);
        }

        [Fact]
        public void FromJson_JsonInput_NumberToString()
        {
            const string json = @"{""key"":12345}";
            var obj = json.FromJson<Dictionary<string, string>>();
            
            Assert.Equal("12345", obj["key"]);
        }
        
        [Fact]
        public void FromJson_JsonInput_LongNumberToString()
        {
            const string json = @"{""key"": 9223372036854775807}";
            var obj = json.FromJson<Dictionary<string, long>>();
            
            Assert.Equal(9223372036854775807L, obj["key"]);
        }
        
        [Fact]
        public void FromJson_JsonInput_SingleDigitNumberToString()
        {
            const string json = @"{""key"":1}";
            var obj = json.FromJson<Dictionary<string, string>>();
            
            Assert.Equal("1", obj["key"]);
        }

        [Fact]
        public void FromJson_JsonInput_StringToString()
        {
            const string json = @"{""key"":""12345""}";
            var obj = json.FromJson<Dictionary<string, string>>();
            
            Assert.Equal("12345", obj["key"]);
        }
        
        [Fact]
        public void ToJson_LongToUnquotedJson()
        {
            var obj = new Dictionary<string, long>();
            obj["key"] = 1234567891234;
            var json = obj.ToJson();
            
            Assert.Equal("{\"key\":1234567891234}", json);
        }

        [Fact]
        public void FromJson_JsonInput_ParsedTwice()
        {
            const string json1 = @"{""some_val"": ""val1"", ""nested"": [{""another_val"": ""val2""}]}";
            ITestObject result1 = json1.FromJson<TestObject>();
            const string json2 = @"{""some_val"": ""val1"", ""nested"": [{""another_val"": ""val2""}]}";
            ITestObject result2 = json2.FromJson<TestObject>();

            Assert.Equal(result1.SomeVal, result2.SomeVal);
        }
        
        [Fact]
        public void FromJson_JsonInput_ParseSingleQuotesAsString()
        {
            const string json = @"{""key"":'foo'}";
            var obj = json.FromJson<Dictionary<string, string>>();
            
            Assert.Equal("foo", obj["key"]);
        }
        
        [Fact]
        public void FromJson_JsonInput_ParseSingleQuotesAsStringInArray()
        {
            const string json = @"{""key"":['foo', 'bar']}";
            var obj = json.FromJson<Dictionary<string, string[]>>();
            
            Assert.Equal(new [] { "foo", "bar" }, obj["key"]);
        }
        
        [Fact]
        public void FromJson_JsonInput_ParseBool()
        {
            const string json = @"{""key"":true}";
            var obj = json.FromJson<Dictionary<string, bool>>();
            
            Assert.Equal(true, obj["key"]);
        }
    }

    internal enum TestEnum
    {
        FieldOne,
        FieldTwo,
        FieldThree,
    }

    internal interface ITestObject
    {
        string SomeVal { get; }

        IEnumerable<INestedTestObject> Nested { get; }

        TestEnum EnumVal { get; }
    }

    internal class TestObject : ITestObject
    {
        [DataMember(Name="some_val")]
        public string SomeVal { get; set; }

        public IEnumerable<INestedTestObject> Nested => _nested ?? new List<NestedTestObject>(0);
        [DataMember(Name="nested")]
        // ReSharper disable once InconsistentNaming
        public List<NestedTestObject> _nested { get; set; }
        
        [DataMember(Name = "enum_val")]
        public TestEnum EnumVal { get; set; }
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
