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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NakamaSync;

namespace Nakama.Tests
{
    public class SyncTestEnvironment
    {
        private const int _RAND_GUEST_SEED = 1;

        public int CreatorIndex { get; }

        private readonly Random _randomGuestGenerator = new Random(_RAND_GUEST_SEED);
        private readonly List<SyncTestUserEnvironment> _syncTestUserEnvironments;

        public SyncTestEnvironment(
            SyncOpcodes opcodes,
            int numClients,
            int numSharedVars,
            int creatorIndex,
            VarIdGenerator idGenerator = null)
        {
            CreatorIndex = creatorIndex;

            var envs = new Dictionary<string, SyncTestUserEnvironment>();

            for (int i = 0; i < numClients; i++)
            {
                ISession session = _syncTestUserEnvironments[i].Session;
                envs[session.UserId] = new SyncTestUserEnvironment($"{Guid.NewGuid()}", opcodes, numClients, idGenerator ?? SyncTestSharedVars.DefaultVarIdGenerator);
            }
        }

        public async Task StartViaMatchmaker()
        {

        }

        public void Start()
        {
            var parallelTasks = new List<Task>();

            for (int i = 0; i < _syncTestUserEnvironments.Count; i++)
            {
                var matchmakerTask = _syncTestUserEnvironments[i].StartMatchViaMatchmaker(_syncTestUserEnvironments.Count, DefaultErrorHandler());
                parallelTasks.Add(matchmakerTask);
            }

            Task.WaitAll(parallelTasks.ToArray());
        }

        public void Dispose()
        {
            var disposeTasks = new List<Task>();

            foreach (SyncTestUserEnvironment userEnv in _syncTestUserEnvironments)
            {
                disposeTasks.Add(userEnv.Dispose());
            }

            Task.WaitAll(disposeTasks.ToArray());
        }

        public SyncTestUserEnvironment GetCreator()
        {
            return _syncTestUserEnvironments[CreatorIndex];
        }

        public IUserPresence GetCreatorPresence()
        {
            return _syncTestUserEnvironments[CreatorIndex].Self;
        }

        public IUserPresence GetRandomGuestPresence()
        {
            List<IUserPresence> guests = GetGuestPresences();
            int randGuestIndex = _randomGuestGenerator.Next(guests.Count);
            return guests[randGuestIndex];
        }

        public SyncTestUserEnvironment GetGuestEnv(IUserPresence presence)
        {
            for (int i = 0; i < _syncTestUserEnvironments.Count; i++)
            {
                if (i == CreatorIndex)
                {
                    continue;
                }

                var env = _syncTestUserEnvironments[i];

                if (env.Self.UserId == presence.UserId)
                {
                    return env;
                }
            }

            throw new InvalidOperationException($"Could not obtain guest environment with presence {presence.UserId}.");
        }

        private List<IUserPresence> GetGuestPresences()
        {
            var guests = new List<IUserPresence>();

            for (int i = 0; i < _syncTestUserEnvironments.Count; i++)
            {
                if (i == CreatorIndex)
                {
                    continue;
                }

                guests.Add(_syncTestUserEnvironments[i].Self);
            }

            return guests;
        }

        public SyncTestUserEnvironment GetUserEnv(IUserPresence clientPresence)
        {
            return _syncTestUserEnvironments.First(env => env.Self.UserId == clientPresence.UserId);
        }

        private SyncErrorHandler DefaultErrorHandler()
        {
            return e => new StdoutLogger().ErrorFormat(e.Message);
        }
    }
}
