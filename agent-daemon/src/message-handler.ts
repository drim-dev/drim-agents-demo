import type { IncomingMessage } from "./types.js";
import type { SessionManager } from "./session-manager.js";
import type { WebSocketClient } from "./websocket-client.js";

export class MessageHandler {
  constructor(
    private readonly wsClient: WebSocketClient,
    private readonly sessionManager: SessionManager,
  ) {}

  async handleMessage(message: IncomingMessage): Promise<void> {
    switch (message.type) {
      case "send_message":
        await this.handleSendMessage(message);
        break;
      default:
        console.warn("[handler] Unknown message type:", (message as unknown as Record<string, unknown>).type);
    }
  }

  private async handleSendMessage(message: IncomingMessage & { type: "send_message" }): Promise<void> {
    const { taskId, chatSessionId, claudeSessionId, content } = message;

    this.wsClient.send({ type: "stream_started", taskId });

    try {
      const result = await this.sessionManager.processMessage(
        taskId,
        chatSessionId,
        claudeSessionId,
        content,
      );

      if (!result) {
        return;
      }

      this.wsClient.send({
        type: "stream_completed",
        taskId,
        claudeSessionId: result.claudeSessionId,
        content: result.content,
      });

      let queued = await this.sessionManager.processNextInQueue(taskId);
      while (queued) {
        this.wsClient.send({
          type: "stream_completed",
          taskId,
          claudeSessionId: queued.result.claudeSessionId,
          content: queued.result.content,
        });
        queued = await this.sessionManager.processNextInQueue(taskId);
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : String(err);
      console.error(`[handler] Error processing task ${taskId}:`, errorMessage);
      this.wsClient.send({ type: "stream_error", taskId, error: errorMessage });
    }
  }
}
