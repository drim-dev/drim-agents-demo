import WebSocket from "ws";
import type { IncomingMessage, OutgoingMessage } from "./types.js";

const MAX_RECONNECT_DELAY_MS = 30_000;
const INITIAL_RECONNECT_DELAY_MS = 1_000;

export class WebSocketClient {
  private ws: WebSocket | null = null;
  private reconnectDelay = INITIAL_RECONNECT_DELAY_MS;
  private messageCallback: ((message: IncomingMessage) => void) | null = null;
  private closed = false;
  private readonly url: string;

  constructor(backendWsUrl: string, apiKey: string) {
    const separator = backendWsUrl.includes("?") ? "&" : "?";
    this.url = `${backendWsUrl}${separator}apiKey=${encodeURIComponent(apiKey)}`;
  }

  onMessage(callback: (message: IncomingMessage) => void): void {
    this.messageCallback = callback;
  }

  connect(): void {
    this.closed = false;
    this.createConnection();
  }

  send(message: OutgoingMessage): void {
    if (this.ws?.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify(message));
    }
  }

  close(): void {
    this.closed = true;
    this.ws?.close();
    this.ws = null;
  }

  private createConnection(): void {
    if (this.closed) return;

    this.ws = new WebSocket(this.url);

    this.ws.on("open", () => {
      console.log("[ws] Connected to backend");
      this.reconnectDelay = INITIAL_RECONNECT_DELAY_MS;
    });

    this.ws.on("message", (data: WebSocket.RawData) => {
      try {
        const parsed = JSON.parse(data.toString()) as IncomingMessage;
        this.messageCallback?.(parsed);
      } catch (err) {
        console.error("[ws] Failed to parse message:", err);
      }
    });

    this.ws.on("close", () => {
      console.log("[ws] Disconnected from backend");
      this.scheduleReconnect();
    });

    this.ws.on("error", (err: Error) => {
      console.error("[ws] Connection error:", err.message);
    });
  }

  private scheduleReconnect(): void {
    if (this.closed) return;

    console.log(`[ws] Reconnecting in ${this.reconnectDelay}ms...`);
    setTimeout(() => this.createConnection(), this.reconnectDelay);
    this.reconnectDelay = Math.min(this.reconnectDelay * 2, MAX_RECONNECT_DELAY_MS);
  }
}
