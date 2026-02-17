export interface SendMessage {
  type: "send_message";
  taskId: string;
  chatSessionId: string;
  claudeSessionId: string | null;
  content: string;
}

export type IncomingMessage = SendMessage;

export interface StreamStarted {
  type: "stream_started";
  taskId: string;
}

export interface StreamToken {
  type: "stream_token";
  taskId: string;
  token: string;
}

export interface StreamCompleted {
  type: "stream_completed";
  taskId: string;
  claudeSessionId: string;
  content: string;
}

export interface StreamError {
  type: "stream_error";
  taskId: string;
  error: string;
}

export type OutgoingMessage =
  | StreamStarted
  | StreamToken
  | StreamCompleted
  | StreamError;
