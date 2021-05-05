# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- A session can be refreshed on demand with "SessionRefreshAsync" method.
- Session and/or refresh tokens can now be disabled with a client logout.
- The client now supports session auto-refresh using refresh tokens. This is enabled by default.
- New socket RPC and MatchSend methods using ArraySegment to allow developers to manage memory re-use.
- Add IAP validation APIs for purchase receipts with Apple App Store, Google Play Store, and Huawei AppGallery.

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
