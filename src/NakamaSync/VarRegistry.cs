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
using Nakama;

namespace NakamaSync
{
    public class VarRegistry
    {
        private readonly Dictionary<Type, IVarSubRegistry> _subregistriesByType = new Dictionary<Type, IVarSubRegistry>();
        private readonly Dictionary<long, IVarSubRegistry> _subregistriesByOpcode = new Dictionary<long, IVarSubRegistry>();

        private readonly int _opcodeStart;
        private SyncMatch _syncMatch;
        private bool _attachedReset = false;

        public VarRegistry(int opcodeStart = 0)
        {
            _opcodeStart = opcodeStart;
        }

        public void Register<T>(SharedVar<T> var)
        {
            VarSubRegistry<T> subRegistry = GetOrAddSubregistry<T>(_opcodeStart + var.Opcode);

            subRegistry.Register(var);

            if (!subRegistry.ReceivedSyncMatch && _syncMatch != null)
            {
                // registration was deferred to after the sync match being received.
                // note that some registries have already received the sync match
                // if a var of their type was already registered prior to receiving the match.
                subRegistry.ReceiveMatch(_syncMatch);
            }
        }

        public void Register<T>(GroupVar<T> var)
        {
            VarSubRegistry<T> subRegistry = GetOrAddSubregistry<T>(_opcodeStart + var.Opcode);
            System.Console.WriteLine("Calling add factory opcode");
            subRegistry.AddFactoryOpcode(_opcodeStart + var.Opcode, var.OthersList);
            subRegistry.Register(var.Self);

            if (!subRegistry.ReceivedSyncMatch && _syncMatch != null)
            {
                // registration was deferred to after the sync match being received.
                // note that some registries have already received the sync match
                // if a var of their type was already registered prior to receiving the match.
                var.Self.ReceiveSyncMatch(_syncMatch);
                subRegistry.ReceiveMatch(_syncMatch);
            }
        }

        public bool HasOpCode(long opcode)
        {
            return _subregistriesByOpcode.ContainsKey(opcode);
        }

        internal void ReceiveMatch(SyncMatch match)
        {
            _syncMatch = match;

            foreach (IVarSubRegistry subRegistry in _subregistriesByType.Values)
            {
                subRegistry.ReceiveMatch(match);
            }

            if (!_attachedReset)
            {
                // todo think about race between this event and inside the presence var factory
                match.Socket.ReceivedMatchPresence += (evt) =>
                {
                    if (evt.Leaves.Any(leave => leave.UserId == match.Session.UserId))
                    {
                        foreach (IVarSubRegistry subRegistry in _subregistriesByType.Values)
                        {
                            subRegistry.Reset();
                        }
                    }
                };
            }

            _attachedReset = true;
        }

        private VarSubRegistry<T> GetOrAddSubregistry<T>(long combinedOpcode)
        {
            VarSubRegistry<T> subRegistry;

            if (!_subregistriesByType.ContainsKey(typeof(T)))
            {
                subRegistry = new VarSubRegistry<T>(_opcodeStart);
                _subregistriesByType[typeof(T)] = subRegistry;
            }
            else
            {
                subRegistry = (VarSubRegistry<T>) _subregistriesByType[typeof(T)];
            }

            if (_subregistriesByOpcode.ContainsKey(combinedOpcode))
            {
                throw new InvalidOperationException("Duplicate opcode for subregistry: " + combinedOpcode);
            }

            _subregistriesByOpcode[combinedOpcode] = subRegistry;

            return subRegistry;
        }

        internal void HandleReceivedMatchState(IMatchState matchState)
        {
            // could just be a regular piece of match data, i.e., not related to sync vars
            if (_subregistriesByOpcode.ContainsKey(_opcodeStart + matchState.OpCode))
            {
                IVarSubRegistry subRegistry = _subregistriesByOpcode[_opcodeStart + matchState.OpCode];
                subRegistry.ReceiveMatchState(matchState);
            }
        }

        internal void HandleMatchClosed()
        {
            foreach (IVarSubRegistry subRegistry in _subregistriesByType.Values)
            {
                subRegistry.Reset();
            }
        }
    }
}
