# Дизайн: Чат-инфраструктура

> Статус: **Готов к реализации**

## Скоуп

Сквозной компонент для стадий Design, Plan и Implementation. Включает:
- Доменная модель ChatSession/ChatMessage с персистентностью в PostgreSQL
- REST API для отправки сообщений и получения истории
- SignalR хаб для real-time доставки сообщений и стриминга ответов агента
- Агент-демон (Node.js) — отдельное приложение, подключается к бэкенду по WebSocket, управляет Claude Code CLI через SDK
- Синхронизация между вкладками через SignalR-группы
- Восстановление пропущенных сообщений при обрыве соединения
- Обработка недоступности агента (сообщение + retry)

**Требования:** FR-8

## Принятые решения

| Решение | Выбор | Обоснование |
|---|---|---|
| Текущая сессия | Автоматически по стадии задачи | Фронтенд не управляет сессиями, бэкенд определяет по Task.Stage |
| Стриминг в новой вкладке | Не подхватываем, ждём полное сообщение | Для MVP достаточно, существенно проще |
| Персистентность стрима | Полное сообщение после завершения | Атомарно, нет мусора в БД, крэш посреди стрима — пользователь повторяет |
| Агент-демон | Node.js/TypeScript + Claude Code SDK | Нативная интеграция с CLI, TypeScript уже в проекте |
| Протокол бэкенд ↔ демон | WebSocket + JSON | Двусторонняя связь, простая отладка |
| Демон в MVP | Один на все задачи | Запускается через Aspire, пул Claude CLI процессов внутри |
| SignalR через BFF | Custom server проксирует WebSocket | Бэкенд приватный, единая точка входа |

## Доменная модель

### Сущность ChatSession

| Поле | Тип | Описание |
|---|---|---|
| Id | long | IdGen, Base32 для API |
| TaskId | long | FK → Task |
| Stage | enum | Design / Plan / Implementation |
| ClaudeSessionId | string? | ID для `--resume`, устанавливается при первом ответе агента |
| CreatedAt | DateTime | UTC |

**Связи:** `Task` → `ChatSessions` (one-to-many). Одна активная сессия на стадию. Уникальный индекс `IX_chat_sessions_task_id_stage` на `(TaskId, Stage)`.

**EF-конфигурация:** таблица `chat.chat_sessions`, схема `chat`.

### Сущность ChatMessage

| Поле | Тип | Описание |
|---|---|---|
| Id | long | IdGen, Base32 для API |
| ChatSessionId | long | FK → ChatSession |
| Role | enum | User / Agent |
| Content | text | Текст сообщения (Markdown) |
| CreatedAt | DateTime | UTC |

**Связи:** `ChatSession` → `Messages` (one-to-many, cascade delete).

**Индексы:**
- `IX_chat_messages_chat_session_id_created_at` на `(ChatSessionId, CreatedAt)` — для выборки истории в хронологическом порядке
- `IX_chat_messages_chat_session_id_id` на `(ChatSessionId, Id)` — для запроса «сообщения после X» при восстановлении

**EF-конфигурация:** таблица `chat.chat_messages`, схема `chat`.

### Enum ChatMessageRole

```csharp
public enum ChatMessageRole
{
    User,
    Agent
}
```

### Enum TaskStage (уже определён в домене Tasks)

```csharp
public enum TaskStage
{
    Design,
    Plan,
    Implementation,
    Review,
    Done
}
```

ChatSession создаётся только для стадий Design, Plan, Implementation — на стадиях Review и Done чата нет.

## Backend API

### Эндпоинты

| Метод | Путь | Описание |
|---|---|---|
| POST | `/api/tasks/{taskId}/chat/messages` | Отправить сообщение агенту |
| GET | `/api/tasks/{taskId}/chat/messages` | История сообщений текущей стадии |
| GET | `/api/tasks/{taskId}/chat/messages?stage=Design` | История сообщений конкретной стадии |

Все эндпоинты требуют аутентификации. Изоляция: задача должна принадлежать проекту текущего пользователя.

### POST `/api/tasks/{taskId}/chat/messages`

**Request:**
```json
{
  "content": "Текст сообщения пользователя"
}
```

**Логика:**
1. Валидация входных данных
2. Проверка, что задача существует и принадлежит пользователю
3. Проверка, что стадия задачи допускает чат (Design / Plan / Implementation)
4. Получить или создать ChatSession для текущей стадии
5. Сохранить ChatMessage (Role=User)
6. Отправить сообщение через SignalR всем подписчикам задачи (синхронизация вкладок)
7. Переслать сообщение агент-демону через WebSocket
8. Если демон недоступен — вернуть `503` с кодом `chat:agent:unavailable`

**Response:** `201 Created`
```json
{
  "id": "base32-encoded",
  "role": "user",
  "content": "Текст сообщения пользователя",
  "createdAt": "2026-02-16T..."
}
```

### GET `/api/tasks/{taskId}/chat/messages`

**Query-параметры:**
- `stage` (опционально) — если не указан, используется текущая стадия задачи
- `afterId` (опционально) — вернуть сообщения после указанного ID (для восстановления после обрыва)

**Response:** `200 OK`
```json
{
  "chatSessionId": "base32-encoded",
  "stage": "design",
  "messages": [
    {
      "id": "base32-encoded",
      "role": "user",
      "content": "...",
      "createdAt": "2026-02-16T..."
    },
    {
      "id": "base32-encoded",
      "role": "agent",
      "content": "...",
      "createdAt": "2026-02-16T..."
    }
  ]
}
```

Сообщения отсортированы по `CreatedAt` asc. Если сессия для стадии ещё не создана — возвращается пустой массив с `chatSessionId: null`.

### Коды ошибок

| Код | HTTP | Описание |
|---|---|---|
| `chat:task:not_found` | 404 | Задача не найдена или чужая |
| `chat:stage:no_chat` | 400 | Стадия не поддерживает чат (Review, Done) |
| `chat:message:content:required` | 400 | Пустое сообщение |
| `chat:message:content:too_long` | 400 | Сообщение > 10 000 символов |
| `chat:agent:unavailable` | 503 | Агент-демон недоступен |

## SignalR Hub

### ChatHub (`/hubs/chat`)

**Подключение:** клиент подключается через BFF (custom server проксирует WebSocket на бэкенд). Аутентификация через BFF-заголовки, как и REST API.

### Методы клиент → сервер

| Метод | Параметры | Описание |
|---|---|---|
| `JoinTask` | `taskId: string` | Подписаться на события задачи. Сервер проверяет владельца и добавляет в группу `task:{taskId}` |
| `LeaveTask` | `taskId: string` | Отписаться от событий задачи |

### События сервер → клиент

| Событие | Payload | Описание |
|---|---|---|
| `MessageReceived` | `{ taskId, message: { id, role, content, createdAt } }` | Новое полное сообщение (user или agent). Для user — сразу после POST, для agent — после завершения стрима |
| `StreamTokenReceived` | `{ taskId, token: string }` | Один токен стриминга ответа агента |
| `StreamStarted` | `{ taskId }` | Агент начал генерировать ответ |
| `StreamCompleted` | `{ taskId, messageId: string }` | Стриминг завершён, полное сообщение сохранено. `messageId` для сопоставления |
| `StreamError` | `{ taskId, error: string }` | Ошибка при стриминге (крэш агента, таймаут) |
| `AgentStatusChanged` | `{ taskId, status: "available" \| "unavailable" }` | Статус агент-демона изменился |

### Группы

Каждая задача — отдельная группа `task:{taskId}`. Все вкладки одного пользователя с открытой задачей — в одной группе. Это обеспечивает:
- Синхронизацию сообщений между вкладками (FR-8.2)
- Стриминг ответа видят все вкладки одновременно
- При отправке сообщения из одной вкладки — остальные получают `MessageReceived`

### Поток стриминга

1. Пользователь POST-ом отправляет сообщение
2. Бэкенд шлёт `MessageReceived` (role=user) всем в группе
3. Бэкенд пересылает сообщение демону по WebSocket
4. Демон вызывает Claude Code SDK, получает стрим
5. Демон шлёт токены бэкенду по WebSocket
6. Бэкенд шлёт `StreamStarted`, затем `StreamTokenReceived` для каждого токена
7. Демон сигнализирует завершение с полным текстом
8. Бэкенд сохраняет ChatMessage (Role=Agent) в БД
9. Бэкенд шлёт `StreamCompleted` + `MessageReceived` (role=agent) с полным сообщением

## WebSocket-протокол: бэкенд ↔ агент-демон

### Подключение

Демон подключается к бэкенду по адресу `ws://{backend}/ws/agent`. При подключении передаёт API-ключ в query-параметре: `ws://{backend}/ws/agent?apiKey={key}`. Бэкенд валидирует ключ и регистрирует соединение.

При обрыве демон переподключается с экспоненциальной задержкой (1s, 2s, 4s, max 30s).

### Сообщения бэкенд → демон

**SendMessage** — отправить сообщение пользователя агенту:
```json
{
  "type": "send_message",
  "taskId": "base32-id",
  "chatSessionId": "base32-id",
  "claudeSessionId": null,
  "content": "Текст сообщения пользователя"
}
```
`claudeSessionId` — если не null, демон использует `--resume` для продолжения контекста.

### Сообщения демон → бэкенд

**StreamStarted:**
```json
{
  "type": "stream_started",
  "taskId": "base32-id"
}
```

**StreamToken:**
```json
{
  "type": "stream_token",
  "taskId": "base32-id",
  "token": "фрагмент текста"
}
```

**StreamCompleted:**
```json
{
  "type": "stream_completed",
  "taskId": "base32-id",
  "claudeSessionId": "session-uuid-from-cli",
  "content": "Полный текст ответа агента"
}
```
Бэкенд сохраняет `claudeSessionId` в ChatSession для последующего `--resume`.

**StreamError:**
```json
{
  "type": "stream_error",
  "taskId": "base32-id",
  "error": "Описание ошибки"
}
```

### Конфигурация

```json
{
  "AgentGateway": {
    "ApiKey": "shared-secret-for-agent-daemon"
  }
}
```

API-ключ — общий секрет между бэкендом и демоном. Для MVP достаточно, в production — заменяется на mTLS или service account.

## Агент-демон (Node.js)

### Обзор

Отдельное Node.js/TypeScript приложение. Подключается к бэкенду по WebSocket, получает сообщения пользователей, вызывает Claude Code SDK, стримит ответы обратно. Для MVP — один процесс обслуживает все задачи, пул Claude CLI сессий внутри.

### Claude Code SDK

Используется npm-пакет `@anthropic-ai/claude-code` для программного вызова:

```typescript
import { query, type ClaudeCodeResult } from "@anthropic-ai/claude-code";

const result = await query({
  prompt: userMessage,
  options: {
    sessionId: claudeSessionId ?? undefined, // --resume
  },
  abortController,
});
```

SDK возвращает стрим событий. Демон парсит их и пересылает токены бэкенду.

### Архитектура демона

**Компоненты:**
- **WebSocketClient** — подключение к бэкенду, отправка/получение JSON-сообщений, автопереподключение
- **SessionManager** — пул активных Claude CLI сессий по `taskId`. Создаёт сессию при первом сообщении, переиспользует при последующих. Хранит `AbortController` для возможности отмены
- **MessageHandler** — получает `send_message` от бэкенда, находит или создаёт сессию, запускает Claude Code SDK, стримит токены обратно

**Жизненный цикл сессии:**
1. Приходит `send_message` с `taskId`
2. SessionManager проверяет, нет ли активной сессии для этого `taskId`
3. Если сессия уже обрабатывает сообщение — очередь (один запрос к Claude за раз на задачу)
4. Вызов Claude Code SDK с `claudeSessionId` для продолжения контекста
5. Стриминг токенов → WebSocket → бэкенд
6. По завершении — отправка `stream_completed` с полным текстом и `claudeSessionId`

### Конфигурация

Через переменные окружения:
- `BACKEND_WS_URL` — WebSocket-адрес бэкенда (`ws://localhost:5000/ws/agent`)
- `AGENT_API_KEY` — ключ для аутентификации
- `MAX_CONCURRENT_TASKS` — максимум одновременных задач (по умолчанию 10)

### Интеграция с Aspire

Демон запускается как отдельный ресурс в Aspire AppHost:

```csharp
var agent = builder.AddNpmApp("agent-daemon", "../agent-daemon", "start")
    .WithReference(backend)
    .WithEnvironment("BACKEND_WS_URL", backend.GetEndpoint("ws"))
    .WithEnvironment("AGENT_API_KEY", agentApiKey);
```

## Восстановление соединения

### SignalR-реконнект (фронтенд ↔ бэкенд)

SignalR-клиент на фронтенде настроен с автоматическим переподключением (`withAutomaticReconnect`). При восстановлении:

1. Клиент хранит `lastMessageId` — ID последнего полученного сообщения
2. После реконнекта клиент заново вызывает `JoinTask`
3. Клиент запрашивает `GET /api/tasks/{id}/chat/messages?afterId={lastMessageId}`
4. Полученные сообщения добавляются в UI
5. Если во время обрыва шёл стрим — он потерян. Клиент увидит только финальное сообщение через `MessageReceived` или, если стрим ещё идёт — индикатор «агент печатает...» при следующем `StreamTokenReceived`

### WebSocket-реконнект (агент-демон ↔ бэкенд)

Демон реализует автоматическое переподключение с экспоненциальной задержкой. При обрыве:

- Если демон стримил ответ — текущий вызов Claude CLI продолжает работу. После переподключения демон отправляет оставшиеся токены и `stream_completed`
- Если бэкенд отправил `send_message` и демон не получил (обрыв) — бэкенд обнаруживает отсутствие соединения и возвращает `503` клиенту. Пользователь видит «агент недоступен» и может повторить

## Обработка недоступности агента

### Обнаружение

Бэкенд отслеживает состояние WebSocket-соединения с демоном:
- Соединение установлено → агент доступен
- Соединение потеряно → агент недоступен
- При изменении статуса — `AgentStatusChanged` через SignalR всем подключённым клиентам

### Поведение при недоступности

**Отправка сообщения:**
- POST возвращает `503` с кодом `chat:agent:unavailable`
- Фронтенд показывает: «Агент временно недоступен» с кнопкой «Повторить»
- Кнопка повторяет тот же POST

**Чтение истории:**
- GET работает всегда — данные в PostgreSQL
- Дашборд, дизайн-документы, планы доступны в read-only (NFR-10)

**Таймаут ответа агента:**
- Если демон не начал стрим в течение 30 секунд после `send_message` — бэкенд отправляет `StreamError` клиентам
- Если стрим начался, но не завершился за 5 минут — бэкенд отправляет `StreamError` и отменяет запрос

## Валидация

### Backend (FluentValidation)

**SendChatMessage.RequestValidator:**
- `Content`: обязательное, непустая строка, максимум 10 000 символов

### Frontend (Zod)

```typescript
const chatMessageSchema = z.object({
  content: z.string().min(1).max(10000),
});
```

## Frontend

### Компонент чата

**`ChatPanel` (Client Component):** Основной компонент, встраивается в страницу задачи `/projects/[projectId]/tasks/[taskId]`. Содержит:

- **Список сообщений** — скроллируемая область, auto-scroll при новых сообщениях
- **Стриминг** — последнее сообщение агента рендерится посимвольно по мере поступления `StreamTokenReceived`
- **Индикатор «агент печатает...»** — отображается между `StreamStarted` и `StreamCompleted`
- **Поле ввода** — textarea с кнопкой отправки, Enter для отправки, Shift+Enter для новой строки
- **Disabled-состояние** — поле ввода заблокировано пока агент генерирует ответ
- **Ошибка агента** — плашка «Агент временно недоступен» с кнопкой «Повторить»
- **Ошибка стрима** — плашка с текстом ошибки и кнопкой «Повторить»

### Хук `useChatSignalR`

Custom React hook, инкапсулирует логику подключения:

```typescript
function useChatSignalR(taskId: string) {
  // Возвращает:
  return {
    messages,          // ChatMessage[]
    streamingContent,  // string | null — текущий стрим
    agentStatus,       // "available" | "unavailable"
    isStreaming,       // boolean
    sendMessage,       // (content: string) => Promise<void>
    error,             // string | null
  };
}
```

**Логика хука:**
1. При монтировании — подключение к SignalR, `JoinTask(taskId)`
2. Загрузка истории через GET
3. Подписка на события: `MessageReceived`, `StreamTokenReceived`, `StreamStarted`, `StreamCompleted`, `StreamError`, `AgentStatusChanged`
4. `sendMessage` — POST через BFF, при `503` устанавливает ошибку
5. При размонтировании — `LeaveTask`, отключение
6. Реконнект — подгрузка пропущенных сообщений через `afterId`

### BFF-маршруты

| Метод | BFF путь | Проксирует на |
|---|---|---|
| POST | `/api/tasks/[taskId]/chat/messages` | `POST /api/tasks/{taskId}/chat/messages` |
| GET | `/api/tasks/[taskId]/chat/messages` | `GET /api/tasks/{taskId}/chat/messages` |

### SignalR через BFF

Custom server (`server.ts`) проксирует WebSocket-соединения с пути `/hubs/chat` на бэкенд. Аналогично тому, как описано в архитектуре — фронтенд подключается к BFF, BFF проксирует на приватный бэкенд.

## Тестирование

### Backend: тесты валидаторов

**SendChatMessageTests.ValidatorTests:**
- Content пустой → ошибка `chat:message:content:required`
- Content > 10 000 символов → ошибка `chat:message:content:too_long`
- Content валидный → без ошибок

### Backend: компонентные тесты

**SendChatMessageTests:**
- Отправка сообщения → 201, сообщение в БД, ChatSession создана автоматически
- Повторная отправка в той же стадии → переиспользуется существующая ChatSession
- Задача на стадии Review → 400 `chat:stage:no_chat`
- Чужая задача → 404
- Агент недоступен → 503 `chat:agent:unavailable`

**GetChatMessagesTests:**
- Получение истории текущей стадии → 200, сообщения в хронологическом порядке
- С параметром `stage=Design` → 200, сообщения конкретной стадии
- С параметром `afterId` → 200, только сообщения после указанного ID
- Пустая история → 200, пустой массив, `chatSessionId: null`
- Чужая задача → 404

### Backend: тесты SignalR

**ChatHubTests:**
- `JoinTask` → клиент получает `MessageReceived` при новом сообщении
- `JoinTask` для чужой задачи → соединение не добавляется в группу
- Два клиента в одной группе → оба получают `MessageReceived`
- `LeaveTask` → клиент больше не получает события

### Backend: тесты WebSocket-сервера для агент-демона

**AgentWebSocketTests:**
- Демон подключается с валидным API-ключом → соединение установлено
- Демон подключается с невалидным ключом → соединение отклонено
- Демон отправляет `stream_completed` → ChatMessage сохранён в БД, `ClaudeSessionId` обновлён в ChatSession
- Демон отправляет `stream_error` → клиенты получают `StreamError` через SignalR

### Agent-демон: unit-тесты

**SessionManagerTests:**
- Первое сообщение для задачи → создаёт новую сессию
- Повторное сообщение → переиспользует сессию с `claudeSessionId`
- Два одновременных сообщения для одной задачи → второе в очередь

**WebSocketClientTests:**
- Подключение к серверу → handshake успешен
- Обрыв соединения → автопереподключение с экспоненциальной задержкой
- Получение `send_message` → вызов колбэка

### Harness для тестирования

Существующие harness-ы расширяются:

**AgentGatewayHarness** — мок WebSocket-соединения с демоном для компонентных тестов бэкенда. Позволяет:
- Имитировать доступность/недоступность демона
- Получать отправленные сообщения и проверять их содержимое
- Эмулировать стриминг ответов (`stream_started` → `stream_token` × N → `stream_completed`)
- Эмулировать ошибки (`stream_error`)

## Структура файлов

### Backend

```
DrimAgents.Api/
├── Common/
│   └── AgentGateway/
│       ├── IAgentGateway.cs              # Интерфейс связи с демоном
│       ├── AgentGatewayOptions.cs         # Конфигурация (ApiKey)
│       └── WebSocketAgentGateway.cs       # WebSocket-реализация
├── Domain/Chat/
│   ├── ChatSession.cs
│   ├── ChatMessage.cs
│   └── ChatMessageRole.cs
├── Database/
│   ├── Configurations/
│   │   ├── ChatSessionConfiguration.cs
│   │   └── ChatMessageConfiguration.cs
│   └── Migrations/
│       └── {timestamp}_AddChat.cs
├── Features/Chat/
│   ├── SendChatMessage.cs                # POST /api/tasks/{taskId}/chat/messages
│   └── GetChatMessages.cs               # GET /api/tasks/{taskId}/chat/messages
└── Hubs/
    └── ChatHub.cs                        # SignalR хаб /hubs/chat
```

### Backend Tests

```
DrimAgents.Api.Tests/
├── Harnesses/
│   └── AgentGatewayHarness.cs            # Мок WebSocket-соединения с демоном
└── Features/Chat/
    ├── SendChatMessageTests.cs           # + вложенный ValidatorTests
    ├── GetChatMessagesTests.cs
    ├── ChatHubTests.cs
    └── AgentWebSocketTests.cs
```

### Agent Daemon

```
agent-daemon/
├── package.json
├── tsconfig.json
├── src/
│   ├── index.ts                          # Точка входа
│   ├── websocket-client.ts               # WebSocket-подключение к бэкенду
│   ├── session-manager.ts                # Пул Claude CLI сессий по taskId
│   ├── message-handler.ts                # Обработка send_message, вызов SDK
│   └── types.ts                          # Типы JSON-протокола
└── tests/
    ├── session-manager.test.ts
    └── websocket-client.test.ts
```

### Frontend

```
frontend/
├── app/api/tasks/[taskId]/chat/
│   └── messages/
│       └── route.ts                      # BFF: POST, GET
├── components/chat/
│   ├── ChatPanel.tsx                     # Основной компонент чата
│   ├── ChatMessageList.tsx               # Список сообщений
│   ├── ChatMessageBubble.tsx             # Одно сообщение
│   ├── ChatInput.tsx                     # Поле ввода + кнопка
│   └── ChatStreamingIndicator.tsx        # «Агент печатает...»
├── hooks/
│   └── use-chat-signalr.ts              # SignalR-хук
├── lib/
│   └── signalr.ts                       # SignalR-клиент, конфигурация
└── types/
    └── chat.ts                           # ChatMessageDto, ChatSessionDto
```

### Aspire AppHost

Обновляется `Aspire.AppHost/Program.cs` — добавляется ресурс `agent-daemon`.
