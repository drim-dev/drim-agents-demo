import { WebSocketClient } from "./websocket-client.js";
import { SessionManager } from "./session-manager.js";
import { MessageHandler } from "./message-handler.js";

function requireEnv(name: string): string {
  const value = process.env[name];
  if (!value) {
    console.error(`Missing required environment variable: ${name}`);
    process.exit(1);
  }
  return value;
}

const backendWsUrl = requireEnv("BACKEND_WS_URL");
const agentApiKey = requireEnv("AGENT_API_KEY");

const wsClient = new WebSocketClient(backendWsUrl, agentApiKey);
const sessionManager = new SessionManager();
const messageHandler = new MessageHandler(wsClient, sessionManager);

wsClient.onMessage((message) => {
  messageHandler.handleMessage(message).catch((err: unknown) => {
    console.error("[main] Unhandled error in message handler:", err);
  });
});

function shutdown(): void {
  console.log("[main] Shutting down...");
  sessionManager.shutdown();
  wsClient.close();
  process.exit(0);
}

process.on("SIGINT", shutdown);
process.on("SIGTERM", shutdown);

console.log("[main] Starting agent daemon...");
wsClient.connect();
