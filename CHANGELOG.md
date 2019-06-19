# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]


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
