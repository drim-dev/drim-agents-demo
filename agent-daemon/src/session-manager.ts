import { query, type SDKResultMessage } from "@anthropic-ai/claude-code";

interface QueuedMessage {
  chatSessionId: string;
  claudeSessionId: string | null;
  content: string;
}

interface SessionState {
  claudeSessionId?: string;
  abortController?: AbortController;
  processing: boolean;
  queue: QueuedMessage[];
}

export interface QueryResult {
  claudeSessionId: string;
  content: string;
}

export class SessionManager {
  private sessions = new Map<string, SessionState>();

  async processMessage(
    taskId: string,
    chatSessionId: string,
    claudeSessionId: string | null,
    content: string,
  ): Promise<QueryResult | null> {
    let session = this.sessions.get(taskId);
    if (!session) {
      session = { processing: false, queue: [] };
      this.sessions.set(taskId, session);
    }

    if (session.processing) {
      session.queue.push({ chatSessionId, claudeSessionId, content });
      return null;
    }

    return this.executeQuery(taskId, session, claudeSessionId, content);
  }

  async processNextInQueue(taskId: string): Promise<{ queued: QueuedMessage; result: QueryResult } | null> {
    const session = this.sessions.get(taskId);
    if (!session || session.queue.length === 0) return null;

    const next = session.queue.shift()!;
    const result = await this.executeQuery(
      taskId,
      session,
      next.claudeSessionId,
      next.content,
    );

    if (!result) return null;
    return { queued: next, result };
  }

  abort(taskId: string): void {
    const session = this.sessions.get(taskId);
    if (session?.abortController) {
      session.abortController.abort();
      session.abortController = undefined;
    }
  }

  shutdown(): void {
    for (const [taskId] of this.sessions) {
      this.abort(taskId);
    }
    this.sessions.clear();
  }

  private async executeQuery(
    taskId: string,
    session: SessionState,
    claudeSessionId: string | null,
    content: string,
  ): Promise<QueryResult> {
    session.processing = true;
    const abortController = new AbortController();
    session.abortController = abortController;

    const resumeSessionId = claudeSessionId ?? session.claudeSessionId;

    try {
      const stream = query({
        prompt: content,
        options: {
          abortController,
          resume: resumeSessionId ?? undefined,
        },
      });

      let resultText = "";
      let resultSessionId = resumeSessionId ?? taskId;

      for await (const message of stream) {
        if (message.type === "result") {
          const resultMsg = message as SDKResultMessage;
          if (resultMsg.subtype === "success" && "result" in resultMsg) {
            resultText = resultMsg.result;
          }
          resultSessionId = message.session_id;
        } else if ("session_id" in message && message.session_id) {
          resultSessionId = message.session_id;
        }
      }

      session.claudeSessionId = resultSessionId;

      return { claudeSessionId: resultSessionId, content: resultText };
    } finally {
      session.processing = false;
      session.abortController = undefined;
    }
  }
}
