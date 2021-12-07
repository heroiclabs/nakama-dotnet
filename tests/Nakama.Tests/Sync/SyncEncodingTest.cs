using System.Collections.Generic;
using NakamaSync;
using Xunit;

namespace Nakama.Tests.Sync
{
    public class SyncEncodingTest
    {
        private enum TestSyncEncodingEnum
        {
            One,
            Two,
            Three
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private void ShouldSerializeString()
        {
            var encoding = new SyncEncoding();
            
            var expectedValue = "Hello World";
            var serializedValue = encoding.Encode(expectedValue);
            var deserializedValue = encoding.Decode<string>(serializedValue);

            Assert.Equal(expectedValue, deserializedValue);
        }
    
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private void ShouldSerializeInt()
        {
            var encoding = new SyncEncoding();
            
            var expectedValue = 1;
            var serializedValue = encoding.Encode(expectedValue);
            var deserializedValue = encoding.Decode<int>(serializedValue);

            Assert.Equal(expectedValue, deserializedValue);
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private void ShouldSerializeFloatWithoutDecimals()
        {
            var encoding = new SyncEncoding();
            
            var expectedValue = 1f;
            var serializedValue = encoding.Encode(expectedValue);
            var deserializedValue = encoding.Decode<float>(serializedValue);

            Assert.Equal(expectedValue, deserializedValue);
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private void ShouldSerializeFloatWithDecimals()
        {
            var encoding = new SyncEncoding();
            
            var expectedValue = 1.23f;
            var serializedValue = encoding.Encode(expectedValue);
            var deserializedValue = encoding.Decode<float>(serializedValue);

            Assert.Equal(expectedValue, deserializedValue);
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private void ShouldSerializeDoubleWithoutDecimals()
        {
            var encoding = new SyncEncoding();
            
            var expectedValue = 1d;
            var serializedValue = encoding.Encode(expectedValue);
            var deserializedValue = encoding.Decode<double>(serializedValue);

            Assert.Equal(expectedValue, deserializedValue);
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private void ShouldSerializeDoubleWithDecimals()
        {
            var encoding = new SyncEncoding();
            
            var expectedValue = 1.23d;
            var serializedValue = encoding.Encode(expectedValue);
            var deserializedValue = encoding.Decode<double>(serializedValue);

            Assert.Equal(expectedValue, deserializedValue);
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private void ShouldSerializeBool()
        {
            var encoding = new SyncEncoding();
            
            var expectedValue = true;
            var serializedValue = encoding.Encode(expectedValue);
            var deserializedValue = encoding.Decode<bool>(serializedValue);
            
            Assert.Equal(expectedValue, deserializedValue);
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private void ShouldSerializeListOfObjects()
        {
            var encoding = new SyncEncoding();
            
            var expectedValue = new List<object> { "Hello", 1.23f, 1.23d, 1 };
            var serializedValue = encoding.Encode(expectedValue);
            var deserializedValue = encoding.Decode<List<object>>(serializedValue);

            Assert.Equal(expectedValue, deserializedValue);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private void ShouldSerializeListOfObjectsAndMaintainTypeIntegrityWhenCasted()
        {
            var encoding = new SyncEncoding();
            
            var expectedValue = new List<object> { "Hello", 1.23f, 1.23d, 1, TestSyncEncodingEnum.Two };
            var serializedValue = encoding.Encode(expectedValue);
            var deserializedValue = encoding.Decode<List<object>>(serializedValue);
            
            var ex1 = Record.Exception(() => (string)deserializedValue[0]);
            Assert.Null(ex1);
            
            var ex2 = Record.Exception(() => (float)deserializedValue[1]);
            Assert.Null(ex2);
            
            var ex3 = Record.Exception(() => (double)deserializedValue[2]);
            Assert.Null(ex3);
            
            var ex4 = Record.Exception(() => (int)deserializedValue[3]);
            Assert.Null(ex4);
            
            var ex5 = Record.Exception(() => (TestSyncEncodingEnum)deserializedValue[4]);
            Assert.Null(ex5);
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private void ShouldSerializeListOfStrings()
        {
            var encoding = new SyncEncoding();
            
            var expectedValue = new List<string> { "Hello", "World" };
            var serializedValue = encoding.Encode(expectedValue);
            var deserializedValue = encoding.Decode<List<string>>(serializedValue);

            Assert.Equal(expectedValue, deserializedValue);
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private void ShouldSerializeDictionaryStringString()
        {
            var encoding = new SyncEncoding();
            
            var expectedValue = new Dictionary<string, string> { { "Hello", "World" }, { "Foo", "Bar" } };
            var serializedValue = encoding.Encode(expectedValue);
            var deserializedValue = encoding.Decode<Dictionary<string, string>>(serializedValue);

            Assert.Equal(expectedValue, deserializedValue);
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private void ShouldSerializeDictionaryStringObject()
        {
            var encoding = new SyncEncoding();
            
            var expectedValue = new Dictionary<string, object> { { "Hello", 1 }, { "Foo", "Bar" } };
            var serializedValue = encoding.Encode(expectedValue);
            var deserializedValue = encoding.Decode<Dictionary<string, object>>(serializedValue);

            Assert.Equal(expectedValue, deserializedValue);
        }
    }
}