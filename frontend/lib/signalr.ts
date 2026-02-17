import {
  HubConnectionBuilder,
  HubConnection,
  LogLevel,
} from '@microsoft/signalr'

export function createChatConnection(url: string = '/hubs/chat'): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(url)
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build()
}
