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
using Nakama;

namespace NakamaSync
{
    public class VarRegistry
    {
        public const long STICKY_HOST_OPCODE = -1;

        internal SharedVar<string> StickyHostId => _stickyHostId;

        private readonly Dictionary<Type, IVarSubRegistry> _subregistriesByType = new Dictionary<Type, IVarSubRegistry>();
        private readonly Dictionary<long, IVarSubRegistry> _subregistriesByOpcode = new Dictionary<long, IVarSubRegistry>();

        private readonly int _opcodeStart;
        private SyncMatch _syncMatch;
        private bool _attachedReset = false;
        private readonly int _handshakeTimeoutSec;
        private readonly object _lock = new object();
        private readonly HashSet<long> _reservedOpcodes = new HashSet<long>();
        private readonly SharedVar<string> _stickyHostId = new SharedVar<string>(VarRegistry.STICKY_HOST_OPCODE);

        public VarRegistry(int opcodeStart = 0, int handshakeTimeoutSec = 5)
        {
            _opcodeStart = opcodeStart;
            _handshakeTimeoutSec = handshakeTimeoutSec;

            _reservedOpcodes.Add(STICKY_HOST_OPCODE);
            RegisterInternal(_stickyHostId);
        }

        public void Register<T>(SharedVar<T> var)
        {
            ThrowIfReserved(var.Opcode);
            RegisterInternal(var);
        }

        public void Register<T>(GroupVar<T> var)
        {
            ThrowIfReserved(var.Opcode);
            RegisterInternal(var);
        }

        internal void RegisterInternal<T>(GroupVar<T> var)
        {
            lock (_lock)
            {
                VarSubRegistry<T> subRegistry = GetOrAddSubregistry<T>(_opcodeStart + var.Opcode);
                subRegistry.AddGroupVar(_opcodeStart + var.Opcode, var);
                subRegistry.Register(var.Self);

                if (!subRegistry.ReceivedSyncMatch && _syncMatch != null)
                {
                    // registration was deferred to after the sync match being received.
                    // note that some registries have already received the sync match
                    // if a var of their type was already registered prior to receiving the match.
                    subRegistry.ReceiveMatch(_syncMatch);
                }
            }
        }

        internal void RegisterInternal<T>(SharedVar<T> var)
        {
            lock (_lock)
            {
                VarSubRegistry<T> subRegistry = GetOrAddSubregistry<T>(_opcodeStart + var.Opcode);

                if (!subRegistry.ReceivedSyncMatch && _syncMatch != null)
                {
                    // registration was deferred to after the sync match being received.
                    // note that some registries have already received the sync match
                    // if a var of their type was already registered prior to receiving the match.
                    subRegistry.ReceiveMatch(_syncMatch);
                }

                subRegistry.Register(var);
            }
        }

        public bool HasOpCode(long opcode)
        {
            return _subregistriesByOpcode.ContainsKey(opcode);
        }

        internal async Task ReceiveMatch(SyncMatch match)
        {
            _syncMatch = match;

            match.Socket.ReceivedMatchPresence += HandlePresenceEvent;

            var subregistryTasks = new List<Task>();

            foreach (IVarSubRegistry subRegistry in _subregistriesByType.Values)
            {
                subregistryTasks.Add(subRegistry.ReceiveMatch(match));
            }

            if (!_attachedReset)
            {
                // todo think about race between this event and inside the presence var factory
                match.Socket.ReceivedMatchPresence += (evt) =>
                {
                    if (evt.Leaves.Any(leave => leave.UserId == match.Self.UserId))
                    {
                            System.Console.WriteLine("resetting sub registry");

                        foreach (IVarSubRegistry subRegistry in _subregistriesByType.Values)
                        {
                            System.Console.WriteLine("resetting sub registry");
                            subRegistry.Reset();
                        }
                    }
                };
            }

            _attachedReset = true;

            await Task.WhenAll(subregistryTasks);
        }

        private void HandlePresenceEvent(IMatchPresenceEvent obj)
        {
            lock (_lock)
            {
                // update internal presence tracker
                _syncMatch.PresenceTracker.HandlePresenceEvent(obj);

                // update vars (e.g., presence var factory)
                foreach (IVarSubRegistry subRegistry in _subregistriesByType.Values)
                {
                    subRegistry.ReceivePresenceEvent(obj);
                }
            }
        }

        private VarSubRegistry<T> GetOrAddSubregistry<T>(long combinedOpcode)
        {
            VarSubRegistry<T> subRegistry;

            if (!_subregistriesByType.ContainsKey(typeof(T)))
            {
                subRegistry = new VarSubRegistry<T>(_opcodeStart, _handshakeTimeoutSec);
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
            lock (_lock)
            {
                // could just be a regular piece of match data, i.e., not related to sync vars
                if (_subregistriesByOpcode.ContainsKey(_opcodeStart + matchState.OpCode))
                {
                    IVarSubRegistry subRegistry = _subregistriesByOpcode[_opcodeStart + matchState.OpCode];
                    subRegistry.ReceiveMatchState(matchState);
                }
            }
        }

        internal void HandleMatchClosed()
        {
            foreach (IVarSubRegistry subRegistry in _subregistriesByType.Values)
            {
                subRegistry.Reset();
            }

            if (_syncMatch == null)
            {
                throw new NullReferenceException("Null sync match during match close.");
            }

            _syncMatch.Socket.ReceivedMatchPresence -= _syncMatch.PresenceTracker.HandlePresenceEvent;
            _syncMatch = null;
        }

        private void ThrowIfReserved(long varOpcode)
        {
            long combinedOpcode = _opcodeStart + varOpcode;
            if (_reservedOpcodes.Contains(combinedOpcode))
            {
                throw new ArgumentException($"Opcode {combinedOpcode} is reserved by the registry.");
            }
        }
    }
}
