# Targets
.PHONY: setup nakama nakama-console satori satori-console

OUT := "../"

setup:
	@echo "Installing dependencies..."
	@go install tool

nakama: setup
	@echo "Building Nakama..."
	@mkdir -p "${OUT}/Nakama"
	@NAKAMA_DIR=${NAKAMA_DIR} NAKAMA_PROTO_FILE=apigrpc/apigrpc.proto go generate ./build/swagger_nakama.go
	@go run main.go "./build/apigrpc/apigrpc.swagger.json" "Nakama" > "${OUT}/Nakama/ApiClient.gen.cs"
	@rm -r ./build/apigrpc

nakama-console: setup
	@echo "Building Nakama Console..."
	@go run main.go "${SWAGGER_FILE}" "NakamaConsole" > "${OUT}/Nakama/Console/ConsoleClient.gen.cs"

satori: setup
	@echo "Building Satori..."
	@mkdir -p "${OUT}/Satori"
	@SATORI_DIR=${SATORI_DIR} SATORI_PROTO_FILE=api/satori.proto go generate ./build/swagger_satori.go
	@go run main.go "./build/api/satori.swagger.json" "Satori" > "${OUT}/Satori/ApiClient.gen.cs"
	@rm -r ./build/api

satori-console: setup
	@echo "Building Satori Console..."
	@mkdir -p "${OUT}/Satori/Console"
	@SATORI_DIR=${SATORI_DIR} SATORI_PROTO_FILE=console/console.proto go generate ./...
	@go run main.go "./build/console/console.swagger.json" "Satori" > "${OUT}/Satori/Console/ConsoleClient.gen.cs"
	@rm -r ./build/console
