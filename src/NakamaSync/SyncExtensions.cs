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

// todo entire concurrency pass on all this
// should var registry have a dictionary or should it have methods?
// TODO what if someone changes the type collection that the key is in, will try to send to the incorrect type
// between clients and may pass handshake.
// if user leaves and then rejoins do their values come back? they are still in collection but
// are they received by that user on initial sync? I think so.
// catch all exceptions and route them through the OnError event? have to think about ones that cannot be handled.
// todo protobuf support when that is merged.
// todo think about fact that user can still attach to presence handler without being able to sequence presence events as they see fit.
// todo synced composite object?
// todo synced list
// todo potential race when creating and joining a match between the construction of this object
// and the dispatching of presence objects off the socket.
// TODO restore the default getvalue call with self
// think about end match flow, resetting sync vars.
// to string calls
// expose interfaces, not concrete classes.
// todo create hostvar.
// todo handle host changed
// todo handle guest left
// todo migrate pending values when host changes
// override tostring
// todo remove any event subscriptions in constructors.
// todo parameter ordering
// todo internalize the registry so that users can't change it from the outside?
// otherwise find some other way to prevent users from messing with it.
// todo error handling checks particularly on dictionary accessing etc.
// should user vars have acks on a user by user basis?
// what if clients don't agree on opcodes? how can you establish that they are on different binary versions?
// todo shouldn't have public sets on the DTOs but need it due to tinyjson
// todo add the reflection approach?
// todo too many params in sync extensions methods
// todo expose metadata to match id method.
// think about what should happen to local changes that occur before the initial store is synced from handshake.
// maybe it's game specific?
// todo what happens if you set a var prior to passing it through the match? throw an exception from the var itself?
// add a "HasValue" helper to the user var for a particular value
// put self and other var into the same variable that has each as a member? or some other elegant way of "grouping" them.
// debate pros and cons of user id vs user presence for user vars/values. i think we do want to use
// userpresence, but it's just a matter of serializing interface rather than concrete type.
// expose an OnLocalVariable changed event for users.
// renamed SharedVar to GlobalVar?
// think about if leavematch should clear all variables as opposed to being dropped from a match
// which would keep the variables there?
// todo create a syncmatch struct and expose presences.
// add null checks for inputs and maybe more input validation
// whole PresenceVarcollection?
// todo make sure you can
// does usemainthread need to be true? test with both. think about different threading models.
// TODO think about a decision to give all vars an interface component and most events or other user-facing objects an interface component. right now it's a mixed approach.
// todo fix disparity w/r/t whether var events have the actual concrete var on them or if we don't really need that...there is inconsistency between the var evnets at the moment.
// todo lock the processing of each envelope to avoid multithreading issues e.g., host changing while processing a value.
// todo support more list-like and dictionary-like methods on the shared and self vars (or maybe use an implicit operator on the var) rather than just setting a fresh new object each time.

using System;
using System.Threading.Tasks;
using Nakama;

namespace NakamaSync
{
    public static class SyncExtensions
    {
        // todo maybe don't require session as a parameter here since we pass it to socket.
        public static async Task<SyncMatch> CreateSyncMatch(this ISocket socket, ISession session, VarRegistry registry, RpcTargetRegistry rpcTargetRegistry, SyncOpcodes opcodes, SyncErrorHandler errorHandler, ILogger logger = null, string name = null)
        {
            logger?.DebugFormat($"User {session.UserId} is creating sync match.");
            AssertValidInputs(registry, errorHandler);
            var services = new SyncServices(socket, session, registry, rpcTargetRegistry, opcodes);
            services.Initialize(isMatchCreator: true, errorHandler: errorHandler, logger: logger);

            IMatch match = await socket.CreateMatchAsync(name);

            logger?.DebugFormat($"User {session.UserId} services are receiving sync match.");

            SyncMatch syncMatch = services.ReceiveMatch(match);

            if (name != null && match.Size > 1)
            {
                try
                {
                    await services.GetHandshakeTask();
                }
                catch (HandshakeFailedException e)
                {
                    errorHandler?.Invoke(e);
                }
            }

            logger?.DebugFormat($"User {session.UserId} is returning sync match.");

            return syncMatch;
        }

        public static async Task<SyncMatch> JoinSyncMatch(this ISocket socket, ISession session, SyncOpcodes opcodes, IMatchmakerMatched matched, VarRegistry registry, RpcTargetRegistry rpcTargetRegistry, SyncErrorHandler errorHandler, ILogger logger = null)
        {
            AssertValidInputs(registry, errorHandler);
            logger?.DebugFormat($"User {session.UserId} is joining sync match via matchmaker.");

            var services = new SyncServices(socket, session, registry, rpcTargetRegistry, opcodes);
            services.Initialize(isMatchCreator: false, errorHandler: errorHandler, logger: logger);

            IMatch match = await socket.JoinMatchAsync(matched);
            SyncMatch syncMatch = services.ReceiveMatch(match);

            try
            {
                await services.GetHandshakeTask();
            }
            catch (HandshakeFailedException e)
            {
                errorHandler?.Invoke(e);
            }

            return syncMatch;
        }

        public static async Task<SyncMatch> JoinSyncMatch(this ISocket socket, ISession session, SyncOpcodes opcodes, string matchId, VarRegistry registry, RpcTargetRegistry rpcTargetRegistry, SyncErrorHandler errorHandler, ILogger logger = null)
        {
            AssertValidInputs(registry, errorHandler);
            logger?.DebugFormat($"User {session.UserId} is joining sync match: {matchId}.");

            var services = new SyncServices(socket, session, registry, rpcTargetRegistry, opcodes);
            services.Initialize(isMatchCreator: false, errorHandler: errorHandler, logger: logger);

            IMatch match = await socket.JoinMatchAsync(matchId);

            var syncMatch = services.ReceiveMatch(match);

            try
            {
                await services.GetHandshakeTask();
            }
            catch (HandshakeFailedException e)
            {
                errorHandler?.Invoke(e);
            }

            return syncMatch;
        }

        private static void AssertValidInputs(VarRegistry registry, SyncErrorHandler errorHandler)
        {
            if (registry == null)
            {
                throw new NullReferenceException("Var registry cannot be null.");
            }

            if (errorHandler == null)
            {
                throw new NullReferenceException("Sync error handler cannot be null.");
            }
        }

        private static void DefaultErrorHandler(Exception e)
        {
            throw e;
        }
    }
}
