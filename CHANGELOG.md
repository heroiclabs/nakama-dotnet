# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- Nakama: New `IClient` event called `ReceivedSessionUpdated` when session expires and is refreshed.

### Changed
- Nakama: `IsConnected` and `IsConnecting` will now read directly from the underlying .NET socket status. This will allow application code to more quickly and easily detect connectivity loss arising from a lack of internet access.
- Nakama: Default socket adapter changed from `WebSocketAdapter` to `WebSocketStdlibAdapter`. This was done to utilize the native .NET Websocket library for improved stability and maintenance.

## [3.10.0] - 2023-11-21
### Changed
- Nakama: Mark socket as connected before event handler is called.
- Nakama: Limited scope of retry logic to very specific 500-level codes from the server.

### Added
- Nakama: Rank count is now returned with tournament record listings.
- Nakama: Added ability to delete tournament records with `DeleteTournamentRecordAsync`.
- Nakama: Hostnames passed to the client now preserve their hardcoded paths.
- Nakama: Create and update times are now returned with notifications.
- Nakama: Added Facebook Instant Games purchase validation.

## [3.9.0]
### Added
- Satori: Added `recompute` option to `UpdatePropertiesAsync` which allows audiences to be recalculated on property update.

### Changed
- Satori: Decreased size of JSON payloads.

### Fixed
- Satori: `DeleteIdentityAsync` no longer accepts an explicit ID.

## [3.8.0]
### Added
- Nakama: Added `Authoritative` flag to tournaments returned from the server.
- Nakama: Added `RefundTime` and `UserId` to purchases and subscriptions returned from the server.
- Nakama: Added raw subscription provider information.
- Nakama: Added `DeleteAccountAsync` for deleting user accounts.
- Satori: Added `DeleteIdentityAsync` for deleting user identities.

### Changed
- Nakama: Used `session.Username` wherever outdated state might be returned.

### Fixed
- Nakama: Fixed issue where outgoing payloads could include unnecessary JSON.


## [3.7.0]
### Added
- Nakama: Added a `UpdatePresences` utility to `IMatch` and `IParty`. Use this method to maintain the presences in your matches and parties
when an `IMatchPresenceEvent` or `IPartyPresenceEvent` is dispatched.
- Satori: Added optional default and custom properties that can be attached to authentication requests.

### Changed
- Satori: `GetFlagDefault` and `GetFlagsDefault` now use the `apiKey` passed to the client constructor rather than accepting it as a unique parameter.

## [3.6.0]
### Added
- Satori: Adds the Satori .NET SDK. Satori is our liveops server for game studios. Please read more about it on the Heroic Labs website.
- Nakama: Adds support for calling RPCs with a HTTP key via POST when a payload is provided.
- Nakama: Expose the `Logger` object on `IClient`.
- Nakama: Adds support for POST RPC requests when using HTTP key with a payload

### Fixed
- Nakama: Prevent race condition when `Close` is called while receive loop has an incomplete read buffer.
- Nakama: Fixed an issue where 500 errors could cause parsing issues on the client.
- Nakama: Added ability to specify `path` parameter to client urls.

### Changed
- Nakama: Fixed an issue where our websocket would throw an exception on `CloseAsync()` in certain situations.

## [3.5.0] - 2022-09-06
### Added
- Ability to `persist` Apple, Huawei, and Google purchase receipts in the Nakama database. This is set to `true` by default in order to allow the server to detect replay attacks.
- Added a `SeenBefore` property to `IApiValidatedPurchase`.
- Added `ListSubscriptionsAsync` which returns a list of the user's subscriptions.
- Added `ValidateSubscriptionAppleAsync` which returns details about a user's Apple subscription.
- Added `ValidateSubscriptionGoogleAsync` which returns details about a user's Google subscription.
- Added `GetSubscriptionAsync` which returns a subscription for the provided product id.
- Added support for `countMultiple` in `AddMatchmakerAsync` and `AddMatchmakerPartyAsync`.

### Changed
- `ValidatedPurchaseEnvironment` has been renamed to `ApiStoreEnvironment`.
- `ValidatedPurchaseStore` has been renamed to `ApiStoreProvider`.
- Removed obsolete client methods that accept a `CancellationTokenSource`. These have been replaced in favor of methods that accept a `CancellationToken` that were added in v3.3.

### Fixed
- Fixed an issue with Socket Closed event taking a significant length of time or not firing at all when internet connection is lost.
- Fixed an issue with `SocketClosed` event taking a significant length of time or not firing at all when internet connection is lost.
- Fixed an issue that would occur when sending messages over the socket from multiple threads.
- Fixed automatic retry seeding to be random across devices.
- Fixed an issue when parsing unquoted numbers as strings in TinyJson.

## [3.4.0] - 2022-04-28
### Added
- Allow max message size limit with socket messages to be overridden in the adapter.
- Relayed multiplayer matches can now be created with a custom name (i.e. room name).

### Fixed
- Fix background read loop to update 'IsConnecting' and 'IsConnected' when close is detected.

## [3.3.0] - 2022-01-24
### Added
- Add overload methods in Client which take a CancellationToken. Thanks @gamecentric.
- Add WebSocketStdlibAdapter allows the codebase to be used in WASM and Blazor projects. Thanks @mattkanwisher.

### Changed
- Use DualMode in TcpClient to handle NAT64 overlay networks (some mobile carriers).
- Refactor the socket adapter design to use Tasks (previously avoided for Unity WebGL compat.).
- Socket messages which exceed the internal buffer size now generate an "InternalBufferOverflowException" type.
- A socket connect made on an already connected socket will no longer raise an exception.
- Propagate up the "WebSocketException" type thrown on socket messages sent over a disconnected socket.
- Update bundled "Ninja.WebSockets" library to commit 0b698a733f0e8711da7a5854154fe7d8a01fbd06.

### Fixed
- Expose base exception if retry handler fails.

## [3.2.0] - 2021-10-11
### Added
- Added additional group listing filters.
- Added ability to overwrite leaderboard/tournament ranking operators from the client.

### Fixed
- Fixed url-safe encoding of query params that were passed to the client as arrays of strings.

## [3.1.1] - 2021-08-19
### Changed
- Removed `autoRefreshSession` from overloaded `Client` constructors. This can still be customized with the base `Client` constructor. This is a workaround for an internal compiler error in Unity's WebGL toolchain.

## [3.1.0] - 2021-08-11
### Added
- Added ability for user to retry requests if they fail due to a transient network error.
- Added ability for user to cancel requests that are in-flight.

## [3.0.0] - 2021-07-14
### Added
- The language tag for the user can be configured with the socket on connect.

### Changed
- An `IPartyMatchmakerTicket` is now received by the party leader when they add their party to the matchmaker via `AddMatchmakerPartyAsync`.
- Renamed `PromotePartyMember` to `PromotePartyMemberAsync`.

## [2.9.3] - 2021-06-17
### Fixed
- Fixed issue where refreshing a session with metadata threw an exception due to the key already existing.

## [2.9.2] - 2021-05-21
### Fixed
- Fixed issue where `IUserPresence` objects were not being deserialized properly by the client as part of the `IParty` object.

### Changed
- AddMatchmakerPartyAsync now returns an IPartyMatchmakerTicket.
- Renamed PromotePartyMember to PromotePartyMemberAsync.

## [2.9.1] - 2021-05-19
### Added
- The `Socket.ReceivedParty` event can now be subscribed to in order to listen for acceptance events from the leader of a closed party.

## [2.9.0] - 2021-05-15
### Added
- A session can be refreshed on demand with "SessionRefreshAsync" method.
- Session and/or refresh tokens can now be disabled with a client logout.
- The client now supports session auto-refresh using refresh tokens. This is enabled by default.
- New socket RPC and MatchSend methods using ArraySegment to allow developers to manage memory re-use.
- Add IAP validation APIs for purchase receipts with Apple App Store, Google Play Store, and Huawei AppGallery.
- Add Realtime Parties feature.

### Changed
- Use lock object with socket operations instead of ConcurrentDictionary as a workaround for a Unity engine WebGL regression.
- Avoid use of extension methods as a workaround for a Unity engine WebGL regression.

### Fixed
- Parse HTTP responses defensively in case of bad load balancer configurations.

## [2.8.0] - 2020-02-19
### Changed
- Listing tournaments can now be done without providing start or end time filters.
- Can now import Steam friends after authenticating or linking to a Steam account.

## [2.7.1] - 2020-02-1
### Fixed
- HTTP Client now properly reads off timeout value.

## [2.7.0] - 2020-10-19
### Changed
- Upgrade code generator to new Swagger format.
### Fixed
- Properly pass server key to Apple auth calls.

## [2.6.0] - 2020-09-21
### Added
- Added Apple single sign-on support.
- Added Steam single sign-on support.

### Fixed
- Fixed serialization of HTTP API error messages.

### Changed
- Silenced a noisy but benign exception related to web socket connections.

## [2.5.0] - 2020-08-12
### Added
- Add parsing support for the Nakama Console API to the code generator.
- Add support for emitting custom events to the Nakama server.
- Add ban and demote API to the client.

### Changed
- Update TinyJson packaged dependency to the '01c586d' commit.
- Remove usage of "System.Diagnostic.Tracing" from the codebase. This improves compatibility with Unity engine.
- Use a Preserve annotation to mark fields which should not be code stripped at build time. This improves compatibility with Unity engine.

## [2.4.0] - 2020-05-04 :star:
### Added
- New ListStorageObjectsAsync method and marked ListStorageObjects as obsolete.

### Changed
- ListUsersStorageObjectsAsync now uses default arguments for optional inputs.

### Fixed
- Prevent InvalidOperationException caused when socket connect task is already completed.

## [2.3.1] - 2019-09-21
### Changed
- Use workaround for IPv6 bug in TcpClient with Mono runtime used with Unity engine.

### Fixed
- Add missing metadata to match join message.
- Add discrete channel identifier in all channel related messages.

## [2.3.0] - 2019-09-02
### Added
- Follow users by username for status updates.
- Decode session variables from the auth token.
- Paginate friends, groups, and user's group listings.
- Filter friends, groups, and user's group listings.
- Send session variables with authenticate requests.
- Socket messages now use a send timeout of 15 seconds to write to the buffer.

### Changed
- Increase the default socket timeout to 30 seconds.

### Fixed
- Use the connect timeout value in native socket connect attempts.
- Link the token source across socket connect and close tasks.

## [2.2.2] - 2019-07-02
### Changed
- Don't synchronize the socket receive with the current thread context.
- Remove workaround for Mono runtime usage with newer TLS negotation.

### Fixed
- Resolve deadlock in socket dispose with synchronization context.

## [2.2.1] - 2019-06-19
### Added
- New comparison methods on some domain types.

### Changed
- When an auth token is decoded into a session but is null or empty now return null.

### Fixed
- Awaited socket callback tasks are now canceled when the socket adapter is closed and cleared.
- Awaited socket callback tasks are now canceled when the socket adapter sends while disconnected.
- Restored missing helper object with storage writes.

## [2.2.0] - 2019-06-06
### Added
- Add tournaments API.
- Add leaderboards around owner API.
- Provide more overload methods to the socket object for simpler usage.

### Changed
- Update TinyJson packaged dependency to latest version.
- Replace WebSocketListener with a new socket library.
- Flatten use of Tasks in method responses.

### Fixed
- Logger is now initialized correctly with socket debugging.
- Stream data state is correctly deserialized from socket messages.
- Fix callback ID on chat and match leave messages.

## [2.1.0] - 2018-08-17
### Added
- Detect socket message encodings.
- All authenticate methods can now pass in username and create options.
- Support gzip compress/decompress on ApiClient methods.

### Changed
- Update the code generator to handle POST/DELETE query params.
- Match listings can now pass through `null` to indicate no filters.
- ApiClient exceptions now contain HTTP status codes.
- Update lowlevel websocket driver due to performance issues on AOT targets like iOS with Unity.
- Disable request decompression by default due to Unity+Android issue.

### Fixed
- Reuse the HTTP client across all methods.

## [2.0.0] - 2018-06-18
### Added
- Initial public release.

This version starts at 2.0 to match the initial server version it supports.
