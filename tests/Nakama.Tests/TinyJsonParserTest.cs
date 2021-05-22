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
        public void FromJson_JsonInput_ParsedTwice()
        {
            const string json1 = @"{""some_val"": ""val1"", ""nested"": [{""another_val"": ""val2""}]}";
            ITestObject result1 = json1.FromJson<TestObject>();
            const string json2 = @"{""some_val"": ""val1"", ""nested"": [{""another_val"": ""val2""}]}";
            ITestObject result2 = json2.FromJson<TestObject>();

            Assert.Equal(result1.SomeVal, result2.SomeVal);
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
