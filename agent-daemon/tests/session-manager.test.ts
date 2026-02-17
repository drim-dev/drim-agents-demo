import { vi, describe, it, expect, beforeEach } from "vitest";

vi.mock("@anthropic-ai/claude-code", () => ({
  query: vi.fn(),
}));

import { query } from "@anthropic-ai/claude-code";
import { SessionManager } from "../src/session-manager.js";

const mockedQuery = vi.mocked(query);

function mockQueryGenerator(sessionId: string, result: string) {
  return async function* () {
    yield {
      type: "result" as const,
      subtype: "success" as const,
      result,
      session_id: sessionId,
    };
  };
}

describe("SessionManager", () => {
  let manager: SessionManager;

  beforeEach(() => {
    vi.clearAllMocks();
    manager = new SessionManager();
  });

  it("should create new session for first message", async () => {
    mockedQuery.mockReturnValue(mockQueryGenerator("ses-1", "Hello!")());

    const result = await manager.processMessage("task-1", "chat-1", null, "Hi");

    expect(result).not.toBeNull();
    expect(result!.content).toBe("Hello!");
    expect(result!.claudeSessionId).toBe("ses-1");
    expect(mockedQuery).toHaveBeenCalledWith({
      prompt: "Hi",
      options: {
        abortController: expect.any(AbortController),
        resume: undefined,
      },
    });
  });

  it("should reuse session with claudeSessionId", async () => {
    mockedQuery.mockReturnValue(mockQueryGenerator("ses-1", "First")());

    await manager.processMessage("task-1", "chat-1", null, "Hi");

    mockedQuery.mockReturnValue(mockQueryGenerator("ses-1", "Second")());

    const result = await manager.processMessage("task-1", "chat-1", "ses-1", "Follow up");

    expect(result).not.toBeNull();
    expect(result!.content).toBe("Second");
    expect(mockedQuery).toHaveBeenLastCalledWith({
      prompt: "Follow up",
      options: {
        abortController: expect.any(AbortController),
        resume: "ses-1",
      },
    });
  });

  it("should queue message when session is processing", async () => {
    let resolveQuery!: () => void;
    const blockingPromise = new Promise<void>((r) => {
      resolveQuery = r;
    });

    mockedQuery.mockReturnValue(
      (async function* () {
        await blockingPromise;
        yield {
          type: "result" as const,
          subtype: "success" as const,
          result: "done",
          session_id: "ses-1",
        };
      })(),
    );

    const firstCall = manager.processMessage("task-1", "chat-1", null, "First");

    await vi.waitFor(() => {
      expect(mockedQuery).toHaveBeenCalledTimes(1);
    });

    const secondResult = await manager.processMessage("task-1", "chat-1", null, "Second");

    expect(secondResult).toBeNull();

    resolveQuery();
    await firstCall;
  });

  it("should abort session", async () => {
    let resolveQuery!: () => void;
    const blockingPromise = new Promise<void>((r) => {
      resolveQuery = r;
    });

    const abortSpy = vi.fn();
    const OriginalAbortController = globalThis.AbortController;
    vi.stubGlobal(
      "AbortController",
      class extends OriginalAbortController {
        abort(...args: Parameters<AbortController["abort"]>) {
          abortSpy();
          return super.abort(...args);
        }
      },
    );

    mockedQuery.mockReturnValue(
      (async function* () {
        await blockingPromise;
        yield {
          type: "result" as const,
          subtype: "success" as const,
          result: "done",
          session_id: "ses-1",
        };
      })(),
    );

    const processPromise = manager.processMessage("task-1", "chat-1", null, "Hi");

    await vi.waitFor(() => {
      expect(mockedQuery).toHaveBeenCalledTimes(1);
    });

    manager.abort("task-1");

    expect(abortSpy).toHaveBeenCalled();

    resolveQuery();
    await processPromise.catch(() => {});

    vi.unstubAllGlobals();
  });

  it("should shutdown all sessions", async () => {
    mockedQuery.mockReturnValue(mockQueryGenerator("ses-1", "R1")());
    await manager.processMessage("task-1", "chat-1", null, "Hi");

    mockedQuery.mockReturnValue(mockQueryGenerator("ses-2", "R2")());
    await manager.processMessage("task-2", "chat-2", null, "Hi");

    manager.shutdown();

    const result = await manager.processMessage("task-1", "chat-1", null, "After shutdown");
    expect(result).not.toBeNull();
    expect(mockedQuery).toHaveBeenLastCalledWith({
      prompt: "After shutdown",
      options: {
        abortController: expect.any(AbortController),
        resume: undefined,
      },
    });
  });
});
