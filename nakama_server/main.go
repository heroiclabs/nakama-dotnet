package main

import (
	"context"
	"database/sql"
	"encoding/json"
	"fmt"

	"github.com/heroiclabs/nakama-common/runtime"
)

func InitModule(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, initializer runtime.Initializer) error {
	logger.Info("Anitra plugin loaded.")

	rpcs := map[string]func(context.Context, runtime.Logger, *sql.DB, runtime.NakamaModule, string) (string, error){
		"clientrpc.create_leaderboard": createLeaderboardRpc,
		"clientrpc.rpc":               rpcEcho,
		"clientrpc.rpc_get":           rpcGet,
		"clientrpc.rpc_error":         rpcError,
		"clientrpc.send_notification":  sendNotificationRpc,
	}

	for id, fn := range rpcs {
		if err := initializer.RegisterRpc(id, fn); err != nil {
			return fmt.Errorf("unable to register %s: %w", id, err)
		}
	}

	return nil
}

// createLeaderboardRpc creates a leaderboard and returns its ID.
func createLeaderboardRpc(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, payload string) (string, error) {
	var input map[string]string
	if err := json.Unmarshal([]byte(payload), &input); err != nil {
		return "", fmt.Errorf("failed to parse payload: %w", err)
	}

	operator := input["operator"]
	if operator == "" {
		operator = "best"
	}

	id := fmt.Sprintf("leaderboard-%s", ctx.Value(runtime.RUNTIME_CTX_USER_ID))

	authoritative := false
	sortOrder := "desc"
	resetSchedule := ""
	metadata := map[string]interface{}{}
	enableRanks := true

	if err := nk.LeaderboardCreate(ctx, id, authoritative, sortOrder, operator, resetSchedule, metadata, enableRanks); err != nil {
		return "", fmt.Errorf("failed to create leaderboard: %w", err)
	}

	result, err := json.Marshal(map[string]string{"leaderboard_id": id})
	if err != nil {
		return "", fmt.Errorf("failed to marshal response: %w", err)
	}

	return string(result), nil
}

// rpcEcho echoes back the payload it receives.
func rpcEcho(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, payload string) (string, error) {
	return payload, nil
}

// rpcGet returns a static PONG response.
func rpcGet(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, payload string) (string, error) {
	return `{"message":"PONG"}`, nil
}

// rpcError returns a structured error matching what the .NET client tests expect.
func rpcError(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, payload string) (string, error) {
	return "", runtime.NewError("Some error occured.", 13)
}

// sendNotificationRpc sends a notification to the user specified in the payload.
func sendNotificationRpc(ctx context.Context, logger runtime.Logger, db *sql.DB, nk runtime.NakamaModule, payload string) (string, error) {
	var input map[string]string
	if err := json.Unmarshal([]byte(payload), &input); err != nil {
		return "", fmt.Errorf("failed to parse payload: %w", err)
	}

	userID := input["user_id"]
	senderID := ctx.Value(runtime.RUNTIME_CTX_USER_ID).(string)

	content := map[string]interface{}{"message": "test notification"}
	if err := nk.NotificationSend(ctx, userID, "test", content, 1, senderID, true); err != nil {
		return "", fmt.Errorf("failed to send notification: %w", err)
	}

	return "{}", nil
}
