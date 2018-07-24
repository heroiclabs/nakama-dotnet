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

namespace Nakama.Tests.Api
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class StorageTest
    {
        private IClient _client;

        // ReSharper disable RedundantArgumentDefaultValue
        [SetUp]
        public void SetUp()
        {
            _client = new Client("defaultkey", "127.0.0.1", 7350, false);
        }

        [Test]
        public async Task ShouldWriteStorageObjects()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var collection = $"{Guid.NewGuid()}";
            var result = await _client.WriteStorageObjectsAsync(session, new WriteStorageObject
            {
                Collection = collection,
                Key = "a",
                Value = "{}"
            });

            Assert.NotNull(result);
            Assert.That(
                result.Acks.Count(a =>
                    a.Collection.Equals(collection) && a.Key.Equals("a") && a.UserId.Equals(session.UserId)),
                Is.EqualTo(1));
        }

        [Test]
        public async Task ShouldWriteStorageObjectsVersion()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var collection = $"{Guid.NewGuid()}";
            var result = await _client.WriteStorageObjectsAsync(session, new WriteStorageObject
            {
                Collection = collection,
                Key = "a",
                Value = "{}"
            });
            Assert.NotNull(result);
            Assert.IsNotEmpty(result.Acks);

            var version = result.Acks.GetEnumerator().Current?.Version;
            Assert.DoesNotThrowAsync(() => _client.WriteStorageObjectsAsync(session, new WriteStorageObject
            {
                Collection = collection,
                Key = "a",
                Value = "{\"some\": \"newvalue\"}",
                Version = version
            }));
        }

        [Test]
        public async Task ShouldWriteStorageObjectFirstVersion()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var collection = $"{Guid.NewGuid()}";
            var result = await _client.WriteStorageObjectsAsync(session, new WriteStorageObject
            {
                Collection = collection,
                Key = "a",
                Value = "{}",
                Version = "*"
            });

            Assert.NotNull(result);
            Assert.That(result.Acks, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task ShouldWriteStorageObjectsEmpty()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var result = await _client.WriteStorageObjectsAsync(session);

            Assert.NotNull(result);
            Assert.IsEmpty(result.Acks);
        }

        [Test]
        public async Task ShouldNotWriteStorageObjects()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var ex = Assert.ThrowsAsync<ApiResponseException>(() => _client.WriteStorageObjectsAsync(session,
                new WriteStorageObject
                {
                    Collection = "collection",
                    Key = "key",
                    Value = "invalid"
                }));
            Assert.NotNull(ex);
            Assert.AreEqual(HttpStatusCode.BadRequest, ex.StatusCode);
        }

        [Test]
        public async Task ShouldNotWriteStorageObjectsNotFirstVersion()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var aobject = new WriteStorageObject
            {
                Collection = $"{Guid.NewGuid()}",
                Key = "key",
                Value = "{}",
                Version = "*"
            };
            await _client.WriteStorageObjectsAsync(session, aobject);

            var ex = Assert.ThrowsAsync<ApiResponseException>(() => _client.WriteStorageObjectsAsync(session, aobject));
            Assert.NotNull(ex);
            Assert.AreEqual(HttpStatusCode.BadRequest, ex.StatusCode);
        }

        [Test]
        public async Task ShouldNotWriteStorageObjectsBadVersion()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var aobject = new WriteStorageObject
            {
                Collection = $"{Guid.NewGuid()}",
                Key = "key",
                Value = "{}"
            };
            var result = await _client.WriteStorageObjectsAsync(session, aobject);
            Assert.NotNull(result);
            Assert.IsNotEmpty(result.Acks);

            aobject.Version = "*";
            aobject.Value = "{\"some\":\"newvalue\"}";
            var ex = Assert.ThrowsAsync<ApiResponseException>(() => _client.WriteStorageObjectsAsync(session, aobject));
            Assert.AreEqual(HttpStatusCode.BadRequest, ex.StatusCode);
        }

        [Test]
        public async Task ShouldReadStorageObjects()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var collection = $"{Guid.NewGuid()}";
            await _client.WriteStorageObjectsAsync(session, new WriteStorageObject
            {
                Collection = collection,
                Key = "a",
                Value = "{}"
            });
            var result = await _client.ReadStorageObjectsAsync(session, new StorageObjectId
            {
                Collection = collection,
                Key = "a",
                UserId = session.UserId
            });

            Assert.NotNull(result);
            Assert.IsNotEmpty(result.Objects);
            Assert.That(
                result.Objects.Count(o =>
                    o.Collection.Equals(collection) && o.Key.Equals("a") && o.UserId.Equals(session.UserId)),
                Is.EqualTo(1));
        }

        [Test]
        public async Task ShouldReadStorageObjectsEmpty()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var result = await _client.ReadStorageObjectsAsync(session);

            Assert.NotNull(result);
            Assert.IsEmpty(result.Objects);
        }

        [Test]
        public async Task ShouldReadStorageObjectsEmptyCollection()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var result = await _client.ReadStorageObjectsAsync(session, new StorageObjectId
            {
                Collection = $"{Guid.NewGuid()}",
                Key = "a",
                UserId = session.UserId
            });

            Assert.NotNull(result);
            Assert.IsEmpty(result.Objects);
        }

        [Test]
        public async Task ShouldReadStorageObjectsWrongKey()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var collection = $"{Guid.NewGuid()}";
            await _client.WriteStorageObjectsAsync(session, new WriteStorageObject
            {
                Collection = collection,
                Key = "a",
                Value = "{}"
            });
            var result = await _client.ReadStorageObjectsAsync(session, new StorageObjectId
            {
                Collection = collection,
                Key = "wrong",
                UserId = session.UserId
            });

            Assert.NotNull(result);
            Assert.IsEmpty(result.Objects);
        }

        [Test]
        public async Task ShouldReadStorageObjectsWrongUser()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var collection = $"{Guid.NewGuid()}";
            const string key = "a";
            await _client.WriteStorageObjectsAsync(session, new WriteStorageObject
            {
                Collection = collection,
                Key = key,
                Value = "{}"
            });
            var result = await _client.ReadStorageObjectsAsync(session, new StorageObjectId
            {
                Collection = collection,
                Key = key,
                UserId = $"{Guid.NewGuid()}"
            });

            Assert.NotNull(result);
            Assert.IsEmpty(result.Objects);
        }

        [Test]
        public async Task ShouldDeleteStorageObjects()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var collection = $"{Guid.NewGuid()}";
            const string key = "a";
            await _client.WriteStorageObjectsAsync(session, new WriteStorageObject
            {
                Collection = collection,
                Key = key,
                Value = "{}"
            });
            var objectId = new StorageObjectId
            {
                Collection = collection,
                Key = key,
                UserId = session.UserId
            };
            await _client.DeleteStorageObjectsAsync(session, objectId);
            var result = await _client.ReadStorageObjectsAsync(session, objectId);

            Assert.NotNull(result);
            Assert.IsEmpty(result.Objects);
        }

        [Test]
        public async Task ShouldDeleteStorageObjestsVersion()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var collection = $"{Guid.NewGuid()}";
            const string key = "a";
            var result = await _client.WriteStorageObjectsAsync(session, new WriteStorageObject
            {
                Collection = collection,
                Key = key,
                Value = "{}"
            });
            Assert.NotNull(result);
            Assert.IsNotEmpty(result.Acks);

            var version = result.Acks.GetEnumerator().Current?.Version;
            Assert.DoesNotThrowAsync(() => _client.DeleteStorageObjectsAsync(session, new StorageObjectId
            {
                Collection = collection,
                Key = key,
                Version = version
            }));
        }

        [Test]
        public async Task ShouldNotDeleteStorageObjectsBadVersion()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var collection = $"{Guid.NewGuid()}";
            const string key = "a";
            var result = await _client.WriteStorageObjectsAsync(session, new WriteStorageObject
            {
                Collection = collection,
                Key = key,
                Value = "{}"
            });
            Assert.NotNull(result);
            Assert.IsNotEmpty(result.Acks);

            var ex = Assert.ThrowsAsync<ApiResponseException>(() => _client.DeleteStorageObjectsAsync(session,
                new StorageObjectId
                {
                    Collection = collection,
                    Key = key,
                    Version = "invalid"
                }));
            Assert.AreEqual(HttpStatusCode.BadRequest, ex.StatusCode);
        }

        [Test]
        public async Task ShouldListStorageObjects()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var collection = $"{Guid.NewGuid()}";

            await _client.WriteStorageObjectsAsync(session, new WriteStorageObject
            {
                Collection = collection,
                Key = "a",
                Value = "{}",
                PermissionRead = 2
            }, new WriteStorageObject
            {
                Collection = collection,
                Key = "b",
                Value = "{}",
                PermissionRead = 2
            });

            var list = await _client.ListStorageObjects(session, collection, 10);
            Assert.NotNull(list);
            Assert.That(list.Objects, Has.Count.EqualTo(2));
        }
    }
}
