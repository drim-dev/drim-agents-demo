# План реализации: Чат-инфраструктура

> Дизайн: `docs/designs/2026-02-16-chat-infrastructure.md`

## Обзор

4 параллельных трека. Треки B, C, D стартуют одновременно. Трек A начинается сразу и блокирует только трек B. Финальная интеграция — после завершения всех треков.

```
Время →

Трек A: [Backend: домен + DB + фичи]──────────────┐
                                                    ├──[Интеграция + тесты]
Трек B:              (ждёт A) [Backend: SignalR + Gateway + wiring]──┤
                                                    │
Трек C: [Agent Daemon]────────────────────────────────┤
                                                    │
Трек D: [Frontend]────────────────────────────────────┘
```

## Трек A: Backend — домен, база данных, API-фичи

**Агент:** `backend-core`
**Контекст:** домен, DB-конфигурации, features. Не трогает Program.cs, Hubs/, Common/AgentGateway/.
**Блокирует:** Трек B

### Слой 0: Доменная модель и база данных

- [ ] Создать `Domain/Chat/ChatMessageRole.cs` — enum (User, Agent)
- [ ] Создать `Domain/Chat/ChatSession.cs` — сущность (Id, TaskId, Stage, ClaudeSessionId?, CreatedAt)
- [ ] Создать `Domain/Chat/ChatMessage.cs` — сущность (Id, ChatSessionId, Role, Content, CreatedAt)
- [ ] Создать `Database/Configurations/ChatSessionConfiguration.cs` — схема `chat`, таблица `chat_sessions`, уникальный индекс (TaskId, Stage)
- [ ] Создать `Database/Configurations/ChatMessageConfiguration.cs` — схема `chat`, таблица `chat_messages`, индексы (ChatSessionId, CreatedAt) и (ChatSessionId, Id)
- [ ] Обновить `Database/AppDbContext.cs` — добавить DbSet-ы ChatSessions и ChatMessages
- [ ] Создать миграцию `AddChat`

### Слой 1: API-фичи

- [ ] Создать `Common/AgentGateway/IAgentGateway.cs` — только интерфейс (SendMessageAsync, IsAvailable, события OnStreamStarted/OnStreamToken/OnStreamCompleted/OnStreamError)
- [ ] Создать `Features/Chat/GetChatMessages.cs` — GET /api/tasks/{taskId}/chat/messages, query-параметры stage и afterId, автоматическое определение текущей стадии
- [ ] Создать `Features/Chat/SendChatMessage.cs` — POST /api/tasks/{taskId}/chat/messages, валидация, автосоздание ChatSession, сохранение сообщения, вызов IAgentGateway

**Выход:** домен, миграция, 2 рабочих эндпоинта (с mock IAgentGateway в DI для компиляции).

---

## Трек B: Backend — SignalR, Agent Gateway, интеграция

**Агент:** `backend-infra`
**Контекст:** Hubs/, Common/AgentGateway/, Program.cs. Читает домен из трека A, не меняет Features/.
**Зависит от:** Трек A (слой 0 — доменные сущности)

### Слой 2: SignalR хаб

- [ ] Создать `Hubs/ChatHub.cs` — методы JoinTask/LeaveTask, проверка владельца задачи, группы `task:{taskId}`

### Слой 3: Agent Gateway

- [ ] Создать `Common/AgentGateway/AgentGatewayOptions.cs` — ApiKey, таймауты (30s на старт стрима, 5min на завершение)
- [ ] Создать `Common/AgentGateway/WebSocketAgentGateway.cs` — WebSocket-сервер (/ws/agent), аутентификация по API-ключу, маршрутизация сообщений по taskId, отслеживание статуса соединения, пробрасывание событий стрима в ChatHub

### Слой 4: Wiring в Program.cs

- [ ] Зарегистрировать SignalR (`AddSignalR`, `MapHub<ChatHub>`)
- [ ] Зарегистрировать IAgentGateway → WebSocketAgentGateway как singleton
- [ ] Зарегистрировать AgentGatewayOptions из конфигурации
- [ ] Замапить WebSocket-эндпоинт `/ws/agent`
- [ ] Обновить `SendChatMessage` — подключить SignalR (IHubContext) для отправки MessageReceived после сохранения сообщения пользователя

**Выход:** полностью рабочий бэкенд — API + SignalR + WebSocket для демона.

---

## Трек C: Agent Daemon (Node.js)

**Агент:** `agent-daemon`
**Контекст:** только директория `agent-daemon/`. Полностью независим от остальных треков.
**Зависит от:** ничего (работает по спецификации протокола из дизайна)

### Слой 0: Скаффолдинг

- [ ] Создать `agent-daemon/package.json` — зависимости: `@anthropic-ai/claude-code`, `ws`, TypeScript
- [ ] Создать `agent-daemon/tsconfig.json`
- [ ] Создать `agent-daemon/src/types.ts` — типы JSON-протокола (SendMessage, StreamStarted, StreamToken, StreamCompleted, StreamError)

### Слой 1: Коммуникация

- [ ] Создать `agent-daemon/src/websocket-client.ts` — подключение к бэкенду, аутентификация через apiKey query-параметр, автопереподключение с экспоненциальной задержкой (1s→2s→4s→max 30s), отправка/получение JSON

### Слой 2: Бизнес-логика

- [ ] Создать `agent-daemon/src/session-manager.ts` — пул сессий по taskId, очередь сообщений (один запрос к Claude за раз на задачу), хранение AbortController, передача claudeSessionId для --resume
- [ ] Создать `agent-daemon/src/message-handler.ts` — получение send_message, вызов Claude Code SDK через SessionManager, стриминг токенов обратно через WebSocketClient

### Слой 3: Точка входа

- [ ] Создать `agent-daemon/src/index.ts` — чтение env-переменных, создание WebSocketClient/SessionManager/MessageHandler, запуск

**Выход:** рабочее Node.js-приложение, готовое к подключению к бэкенду.

---

## Трек D: Frontend

**Агент:** `frontend`
**Контекст:** только директория `frontend/`. Работает по API-контракту из дизайна.
**Зависит от:** ничего (работает по спецификации API из дизайна)

### Слой 0: Основы

- [ ] Установить `@microsoft/signalr` npm-пакет
- [ ] Создать `types/chat.ts` — ChatMessageDto, ChatSessionDto, ChatMessagesResponse
- [ ] Создать `lib/signalr.ts` — создание HubConnection с автореконнектом, конфигурация URL `/hubs/chat`

### Слой 1: Хук

- [ ] Создать `hooks/use-chat-signalr.ts` — подключение к SignalR, загрузка истории, подписка на события, отправка сообщений через BFF, реконнект с подгрузкой пропущенных сообщений через afterId

### Слой 2: Компоненты

- [ ] Создать `components/chat/ChatMessageBubble.tsx` — рендер одного сообщения (user/agent), Markdown для агента
- [ ] Создать `components/chat/ChatStreamingIndicator.tsx` — индикатор «агент печатает...»
- [ ] Создать `components/chat/ChatMessageList.tsx` — скроллируемый список сообщений, auto-scroll
- [ ] Создать `components/chat/ChatInput.tsx` — textarea, Enter/Shift+Enter, disabled во время стрима
- [ ] Создать `components/chat/ChatPanel.tsx` — композиция всех компонентов, ошибки, retry

### Слой 3: BFF

- [ ] Создать `app/api/tasks/[taskId]/chat/messages/route.ts` — GET и POST, проксирование на бэкенд
- [ ] Обновить `server.ts` (создать если нет) — проксирование WebSocket /hubs/chat на бэкенд

**Выход:** полностью рабочий фронтенд чата, готовый к интеграции.

---

## Финал: Интеграция и тестирование

**Зависит от:** все треки завершены

### Aspire

- [ ] Обновить `Aspire.AppHost/Program.cs` — добавить agent-daemon как NpmApp с переменными окружения (BACKEND_WS_URL, AGENT_API_KEY)

### Backend тесты

- [ ] Создать `Harnesses/AgentGatewayHarness.cs` — мок WebSocket-соединения с демоном
- [ ] Создать `Features/Chat/SendChatMessageTests.cs` — компонентные тесты + ValidatorTests
- [ ] Создать `Features/Chat/GetChatMessagesTests.cs` — компонентные тесты
- [ ] Создать `Features/Chat/ChatHubTests.cs` — тесты SignalR
- [ ] Создать `Features/Chat/AgentWebSocketTests.cs` — тесты WebSocket-сервера

### Agent Daemon тесты

- [ ] Создать `agent-daemon/tests/session-manager.test.ts`
- [ ] Создать `agent-daemon/tests/websocket-client.test.ts`

### Сквозная проверка

- [ ] Запуск через Aspire — проверить полный flow: отправка сообщения → стриминг ответа → сохранение в БД → отображение на фронтенде
- [ ] Проверить синхронизацию между вкладками
- [ ] Проверить обрыв и восстановление соединения

---

## Сводка параллельности

| Этап | backend-core | backend-infra | agent-daemon | frontend |
|---|---|---|---|---|
| Старт | Сразу | Ждёт backend-core | Сразу | Сразу |
| Файлы | Domain/, Database/, Features/Chat/ | Hubs/, Common/AgentGateway/, Program.cs | agent-daemon/ | frontend/ |
| Конфликты | Нет | Program.cs (единственный) | Нет | Нет |
| Контекст | Домен + существующие фичи | Домен + Program.cs | Только дизайн (протокол) | Только дизайн (API) |
