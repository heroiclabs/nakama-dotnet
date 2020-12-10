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

# This command is a utility for downloading all .proto files required
# for making full source builds.

NAKAMA_COMMIT=8e8e74280915e77b02871955931f60bcea5b91a1
NAKAMA_COMMON_COMMIT=5254da5c83ced136a4c8014a2cf2eaa85875811d

DOMAIN=https://raw.githubusercontent.com

API_URL=${DOMAIN}/heroiclabs/nakama-common/${NAKAMA_COMMON_COMMIT}/api/api.proto
APIGRPC_URL=${DOMAIN}/heroiclabs/nakama/${NAKAMA_COMMIT}/apigrpc/apigrpc.proto
REALTIME_URL=${DOMAIN}/heroiclabs/nakama-common/${NAKAMA_COMMON_COMMIT}/rtapi/realtime.proto

NAKAMA_COMMON_DIR=github.com/heroiclabs/nakama-common
ROOT_DIR=protobuf/protos

rm -rf proto

curl --create-dirs $API_URL -o ${ROOT_DIR}/${NAKAMA_COMMON_DIR}/api/api.proto
curl --create-dirs $APIGRPC_URL -o ${ROOT_DIR}/apigrpc.proto
curl --create-dirs $REALTIME_URL -o ${ROOT_DIR}/${NAKAMA_COMMON_DIR}/rtapi/realtime.proto
