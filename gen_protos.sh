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

protoc -Isrc/Nakama.Protobuf/protos \
--csharp_out=internal_access:./src/Nakama.Protobuf/generated \
./src/Nakama.Protobuf/protos/github.com/heroiclabs/nakama-common/api/api.proto \
./src/Nakama.Protobuf/protos/github.com/heroiclabs/nakama-common/rtapi/realtime.proto \
./src/Nakama.Protobuf/protos/apigrpc.proto
