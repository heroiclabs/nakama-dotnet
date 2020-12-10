#!/bin/bash

# Copyright 2020 The Nakama Authors.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# This command is a utility for generating protocol buffers.

protoc -Iprotobuf/protos --csharp_out=./protobuf/generated \
protobuf/protos/github.com/heroiclabs/nakama-common/api/api.proto \
protobuf/protos/github.com/heroiclabs/nakama-common/rtapi/realtime.proto \
protobuf/protos/apigrpc.proto \
protobuf/protos/google/protobuf/any.proto \
protobuf/protos/google/protobuf/descriptor.proto \
protobuf/protos/google/protobuf/empty.proto \
protobuf/protos/google/protobuf/struct.proto \
protobuf/protos/google/protobuf/timestamp.proto \
protobuf/protos/google/protobuf/wrappers.proto \
protobuf/protos/google/api/annotations.proto \
protobuf/protos/google/api/http.proto \
protobuf/protos/protoc-gen-swagger/options/openapiv2.proto