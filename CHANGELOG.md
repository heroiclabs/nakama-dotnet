# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]


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
