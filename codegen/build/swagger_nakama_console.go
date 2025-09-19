// Copyright 2025 Heroic Labs.
// All rights reserved.
//
// NOTICE: All information contained herein is, and remains the property of Heroic
// Labs. and its suppliers, if any. The intellectual and technical concepts
// contained herein are proprietary to Heroic Labs. and its suppliers and may be
// covered by U.S. and Foreign Patents, patents in process, and are protected by
// trade secret or copyright law. Dissemination of this information or reproduction
// of this material is strictly forbidden unless prior written permission is
// obtained from Heroic Labs.

package api

//build:ignore
//go:generate protoc -I $NAKAMA_DIR -I $NAKAMA_DIR/vendor -I $NAKAMA_DIR/vendor/github.com/heroiclabs/nakama-common -I $NAKAMA_DIR/build/grpc-gateway-v2.3.0/third_party/googleapis -I $NAKAMA_DIR/vendor/github.com/grpc-ecosystem/grpc-gateway/v2 --openapiv2_out=. --openapiv2_opt=json_names_for_fields=false,logtostderr=true $NAKAMA_PROTO_FILE
