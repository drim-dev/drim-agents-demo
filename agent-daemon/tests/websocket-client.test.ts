import { vi, describe, it, expect, beforeEach, afterEach } from "vitest";
import { EventEmitter } from "events";

const constructorCalls: string[] = [];
const mockInstances: Array<EventEmitter & { readyState: number; send: ReturnType<typeof vi.fn>; close: ReturnType<typeof vi.fn>; url: string }> = [];

vi.mock("ws", () => {
  const { EventEmitter: EE } = require("events") as typeof import("events");

  class MockWS extends EE {
    static OPEN = 1;
    static CLOSED = 3;
    readyState = 1;
    send = vi.fn();
    close = vi.fn();
    url: string;

    constructor(url: string) {
      super();
      this.url = url;
      constructorCalls.push(url);
      mockInstances.push(this);
    }
  }

  return { default: MockWS };
});

import { WebSocketClient } from "../src/websocket-client.js";

function getLastMockInstance() {
  return mockInstances[mockInstances.length - 1];
}

describe("WebSocketClient", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    constructorCalls.length = 0;
    mockInstances.length = 0;
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("should connect to backend with apiKey", () => {
    const client = new WebSocketClient("ws://localhost:5000/ws", "my-key");
    client.connect();

    expect(constructorCalls).toEqual(["ws://localhost:5000/ws?apiKey=my-key"]);
  });

  it("should append apiKey with & when URL has query params", () => {
    const client = new WebSocketClient("ws://localhost:5000/ws?foo=bar", "my-key");
    client.connect();

    expect(constructorCalls).toEqual(["ws://localhost:5000/ws?foo=bar&apiKey=my-key"]);
  });

  it("should call message callback on incoming message", () => {
    const client = new WebSocketClient("ws://localhost:5000/ws", "key");
    const callback = vi.fn();
    client.onMessage(callback);
    client.connect();

    const ws = getLastMockInstance();
    const message = { type: "send_message", taskId: "t-1", chatSessionId: "c-1", claudeSessionId: null, content: "hello" };
    ws.emit("message", Buffer.from(JSON.stringify(message)));

    expect(callback).toHaveBeenCalledWith(message);
  });

  it("should send JSON messages", () => {
    const client = new WebSocketClient("ws://localhost:5000/ws", "key");
    client.connect();

    const ws = getLastMockInstance();
    ws.emit("open");

    const outgoing = { type: "stream_completed" as const, taskId: "t-1", claudeSessionId: "s-1", content: "done" };
    client.send(outgoing);

    expect(ws.send).toHaveBeenCalledWith(JSON.stringify(outgoing));
  });

  it("should close connection and not reconnect", () => {
    const client = new WebSocketClient("ws://localhost:5000/ws", "key");
    client.connect();

    expect(constructorCalls).toHaveLength(1);
    const ws = getLastMockInstance();

    client.close();

    expect(ws.close).toHaveBeenCalled();

    vi.advanceTimersByTime(60_000);

    expect(constructorCalls).toHaveLength(1);
  });

  it("should schedule reconnect on disconnect", () => {
    const client = new WebSocketClient("ws://localhost:5000/ws", "key");
    client.connect();

    expect(constructorCalls).toHaveLength(1);

    const ws = getLastMockInstance();
    ws.emit("close");

    vi.advanceTimersByTime(1_000);

    expect(constructorCalls).toHaveLength(2);
  });
});
