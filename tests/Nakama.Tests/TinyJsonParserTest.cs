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
using System;

namespace Nakama.Tests
{
    public class TinyJsonParserTest
    {
        [Fact]
        public void FromJson_JsonInput_Parsed()
        {
            const string json = @"{""some_val"": ""val1"", ""nested"": [{""another_val"": ""val2"", ""date_val"":""2/16/2021 3:28:34 PM""}]}";
            ITestObject result = json.FromJson<TestObject>();

            Assert.Equal("val1", result.SomeVal);
            Assert.Equal("val2", result.Nested.First().AnotherVal);
            Assert.Equal(Convert.ToDateTime("2/16/2021 3:28:34 PM"), result.Nested.First().DateVal);
        }

        [Fact]
        public void FromJson_JsonInput_ParsedTwice()
        {
            const string json1 = @"{""some_val"": ""val1"", ""nested"": [{""another_val"": ""val2"", ""date_val"":""2/16/2021 3:28:34 PM""}]}";
            ITestObject result1 = json1.FromJson<TestObject>();
            const string json2 = @"{""some_val"": ""val1"", ""nested"": [{""another_val"": ""val2"", ""date_val"":""2/16/2021 3:28:34 PM""}]}";
            ITestObject result2 = json2.FromJson<TestObject>();

            Assert.Equal(result1.SomeVal, result2.SomeVal);
        }
        
        [Fact]
        public void FromObject_ToObject()
        {
            TestObjectWithAllTypes testObject = new TestObjectWithAllTypes();
            testObject.UUID = Guid.NewGuid();
            testObject.IntVal = 1;
            testObject.DateTimeVal = DateTime.Now;
            testObject.BoolVal = true;
            testObject.FloatVal = 1.1F;
            testObject.Stringval = "Good Work";

            string json = testObject.ToJson();

            TestObjectWithAllTypes testObjectFromJson = json.FromJson<TestObjectWithAllTypes>();

            Assert.Equal(testObject.UUID, testObjectFromJson.UUID);
            Assert.Equal(testObject.IntVal, testObjectFromJson.IntVal);
            Assert.Equal(testObject.DateTimeVal, testObjectFromJson.DateTimeVal);
            Assert.Equal(testObject.BoolVal, testObjectFromJson.BoolVal);
            Assert.Equal(testObject.FloatVal, testObjectFromJson.FloatVal);
            Assert.Equal(testObject.Stringval, testObjectFromJson.Stringval);
        }
    }

    public interface ITestObject
    {
        string SomeVal { get; }

        IEnumerable<INestedTestObject> Nested { get; }
    }

    internal class TestObject : ITestObject
    {
        [DataMember(Name = "some_val")]
        public string SomeVal { get; set; }

        public IEnumerable<INestedTestObject> Nested => _nested ?? new List<NestedTestObject>(0);
        [DataMember(Name = "nested")]
        // ReSharper disable once InconsistentNaming
        public List<NestedTestObject> _nested { get; set; }
    }

    public interface INestedTestObject
    {
        string AnotherVal { get; }
        DateTime DateVal { get; }
    }

    internal class NestedTestObject : INestedTestObject
    {
        [DataMember(Name = "another_val")]
        public string AnotherVal { get; set; }

        [DataMember(Name = "date_val")]
        public DateTime DateVal { get; set; }
    }
    
    internal class TestObjectWithAllTypes
    {
        public Guid UUID { get; set; }
        public int IntVal { get; set; }
        public DateTime DateTimeVal { get; set; }
        public bool BoolVal { get; set; }
        public float FloatVal { get; set; }
        public string Stringval { get; set; }
    }
}
