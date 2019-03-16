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

namespace Nakama.Tests
{
    using NUnit.Framework;
    using System.Linq;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using TinyJson;

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
    
    [TestFixture]
    public class TinyJsonParserTest
    {
        [Test]
        public void ShouldParseObject()
        {
            const string json = @"{""some_val"": ""val1"", ""nested"": [{""another_val"": ""val2""}]}";
            var result = json.FromJson<TestObject>();

            Assert.AreEqual("val1", result.SomeVal);
            Assert.AreEqual("val2", result.Nested.First().AnotherVal);
        }

        [Test]
        public void ShouldParseObjectTwice()
        {
            const string json1 = @"{""some_val"": ""val1"", ""nested"": [{""another_val"": ""val2""}]}";
            var result1 = json1.FromJson<TestObject>();
            const string json2 = @"{""some_val"": ""val1"", ""nested"": [{""another_val"": ""val2""}]}";
            var result2 = json2.FromJson<TestObject>();
            
            Assert.AreEqual(result1.SomeVal, result2.SomeVal);
        }
    }
}