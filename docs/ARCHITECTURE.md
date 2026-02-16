# Architecture: AI Tasks Orchestrator

## Обзор

Распределённая система для оркестрации AI-агентов (Claude Code CLI) в полном цикле разработки. Архитектура — клиент-серверная с BFF-слоем: Next.js (UI + BFF) → ASP.NET Core API → PostgreSQL. Бэкенд полностью приватный — весь внешний трафик (REST, SignalR, GitHub-вебхуки) проходит через BFF. Тяжёлая работа (дизайн, планирование, генерация кода, тесты) выполняется на отдельных worker pods с Claude Code CLI, которые общаются с бэкендом по WebSocket. Фоновая оркестрация — Hangfire с PostgreSQL storage.

## Технический стек

| Компонент | Технология | Обоснование |
|---|---|---|
| Бэкенд | C#, ASP.NET Core 9.0 | Vertical Slice Architecture, MediatR, FluentValidation |
| Фронтенд | TypeScript, Next.js (App Router) | SSR, Server Components, BFF через API Routes |
| База данных | PostgreSQL + EF Core | Code-first, единое хранилище для данных и Hangfire |
| Реалтайм | SignalR (через BFF) | Двусторонняя связь для чата и обновлений дашборда |
| Фоновые задачи | Hangfire + PostgreSQL storage | Распределённая очередь, готовность к масштабированию |
| Оркестрация (dev) | .NET Aspire | Service discovery, наблюдаемость, локальная разработка |
| Аутентификация | NextAuth.js (OAuth 2.0) | Google, GitHub, GitLab — без логина/пароля |
| AI-агент | Claude Code CLI (headless) | Подписка Max, стриминг через stream-json |
| Деплой | Kubernetes (k3s), Hetzner | Self-hosted, cloud-agnostic |

## Компоненты системы

### Frontend (Next.js)

**Ответственность:** UI (Server + Client Components), BFF-слой (API Routes), проксирование SignalR и GitHub-вебхуков, OAuth-сессии (NextAuth.js).

**Взаимодействует с:** Backend API (REST, SignalR), GitHub (входящие вебхуки).

**Ключевые решения:** Custom server (`server.ts`) для проксирования WebSocket. Бэкенд полностью скрыт — единственная точка входа для внешнего мира.

### Backend API (ASP.NET Core)

**Ответственность:** Бизнес-логика, CQRS-обработчики (MediatR), валидация (FluentValidation), SignalR-хабы, координация worker pods.

**Взаимодействует с:** PostgreSQL, Worker Pods (WebSocket), GitHub API (исходящие вызовы через PAT).

**Ключевые решения:** Vertical Slice Architecture — каждая фича в одном файле. Приватный — не доступен извне кластера.

### Orchestrator (Hangfire)

**Ответственность:** Управление жизненным циклом задач — запуск worker pods через Kubernetes API, распределение шагов по графу зависимостей, реакция на события (шаг завершён → запустить зависимые, все шаги готовы → создать PR).

**Взаимодействует с:** PostgreSQL (storage + состояние), Kubernetes API, Backend API.

**Ключевые решения:** Hangfire с PostgreSQL storage — распределённая очередь без внешней инфраструктуры, готовность к масштабированию на несколько реплик бэкенда.

### Worker Pod

**Ответственность:** Выполнение всех стадий задачи — дизайн (диалог с пользователем, формирование документа), планирование (генерация плана), реализация (код, тесты, ревью), коммиты артефактов в репозиторий.

**Взаимодействует с:** Backend API (WebSocket — получает команды, стримит результаты), GitHub (коммиты через CLI), файловая система (клонированный репозиторий).

**Ключевые решения:** Один worker pod на задачу. Создаётся при начале работы над дизайном, живёт до завершения задачи (или до остановки пользователем), уничтожается после. Дизайн-документ и план реализации хранятся как текстовые файлы в репозитории — агент коммитит их по итогам каждой стадии. Содержит агент-демон + Claude Code CLI + клон репозитория. На стадии реализации независимые шаги могут выполняться параллельно несколькими процессами `claude -p` внутри одного worker-а.

### PostgreSQL

**Ответственность:** Хранение всех данных (проекты, задачи, чаты, дизайны, планы, артефакты) + Hangfire storage.

**Ключевые решения:** Единая БД для данных и очереди задач. GitHub PAT шифруется AES-256 на уровне приложения.

## Модель данных

### User

| Поле | Тип | Описание |
|---|---|---|
| Id | string | IdGen + Crockford Base32 |
| Email | string | |
| Name | string | |
| AvatarUrl | string? | |
| OAuthProvider | string | Google / GitHub / GitLab |
| OAuthId | string | ID у провайдера |
| CreatedAt | DateTime | |

### Project

| Поле | Тип | Описание |
|---|---|---|
| Id | string | |
| UserId | string | FK → User |
| Name | string | 3-200 символов |
| Description | string? | |
| GitHubRepoUrl | string | |
| EncryptedGitHubPat | string | AES-256 |
| CreatedAt | DateTime | |

### Task

| Поле | Тип | Описание |
|---|---|---|
| Id | string | |
| ProjectId | string | FK → Project |
| Name | string | 3-200 символов |
| Stage | enum | Design / Plan / Implementation / Review / Done |
| CreatedAt | DateTime | |

### DesignDocument

| Поле | Тип | Описание |
|---|---|---|
| Id | string | |
| TaskId | string | FK → Task (1:1) |
| Content | text | Markdown — кэш файла из репозитория |
| FilePath | string | Путь к файлу в репозитории |
| IsApproved | bool | |
| UpdatedAt | DateTime | |

### Plan

| Поле | Тип | Описание |
|---|---|---|
| Id | string | |
| TaskId | string | FK → Task (1:1) |
| Content | text | Markdown — кэш файла из репозитория |
| FilePath | string | Путь к файлу в репозитории |
| IsApproved | bool | |
| UpdatedAt | DateTime | |

### Step

| Поле | Тип | Описание |
|---|---|---|
| Id | string | |
| PlanId | string | FK → Plan |
| Title | string | |
| Status | enum | Pending / InProgress / Completed / Failed |
| Order | int | Порядок отображения |
| CreatedAt | DateTime | |

**Зависимости между шагами:** таблица связи StepDependency (StepId, DependsOnStepId).

### ChatSession

| Поле | Тип | Описание |
|---|---|---|
| Id | string | |
| TaskId | string | FK → Task |
| Stage | enum | Design / Plan / Implementation |
| ClaudeSessionId | string? | ID для `--resume` |
| CreatedAt | DateTime | |

**Связи:** одна задача → отдельная ChatSession на каждую стадию (дизайн, план, реализация). Контекст Claude Code CLI привязан к конкретному `--resume` ID.

### ChatMessage

| Поле | Тип | Описание |
|---|---|---|
| Id | string | |
| ChatSessionId | string | FK → ChatSession |
| Role | enum | User / Agent |
| Content | text | |
| CreatedAt | DateTime | |

### Artifact

| Поле | Тип | Описание |
|---|---|---|
| Id | string | |
| TaskId | string | FK → Task |
| StepId | string? | FK → Step (опционально) |
| Type | enum | TestLog / ReviewResult / Other |
| Content | text | |
| CreatedAt | DateTime | |

## API

### Проекты

| Метод | Путь | Описание |
|---|---|---|
| POST | `/api/projects` | Создать проект |
| GET | `/api/projects` | Список проектов пользователя |
| GET | `/api/projects/{id}` | Получить проект |

### Задачи

| Метод | Путь | Описание |
|---|---|---|
| POST | `/api/projects/{id}/tasks` | Создать задачу (запускает worker pod) |
| GET | `/api/projects/{id}/tasks` | Список задач проекта (для канбана) |
| GET | `/api/tasks/{id}` | Детальный вид задачи |

### Дизайн

| Метод | Путь | Описание |
|---|---|---|
| GET | `/api/tasks/{id}/design` | Получить дизайн-документ |
| POST | `/api/tasks/{id}/design/approve` | Утвердить дизайн |
| POST | `/api/tasks/{id}/design/revise` | Вернуть дизайн на доработку |

### План

| Метод | Путь | Описание |
|---|---|---|
| GET | `/api/tasks/{id}/plan` | Получить план с шагами |
| POST | `/api/tasks/{id}/plan/generate` | Сгенерировать план |
| POST | `/api/tasks/{id}/plan/approve` | Утвердить план (запускает реализацию) |
| POST | `/api/tasks/{id}/plan/revise` | Вернуть план на доработку |

### Реализация

| Метод | Путь | Описание |
|---|---|---|
| POST | `/api/tasks/{id}/implementation/stop` | Остановить реализацию |
| GET | `/api/tasks/{id}/steps` | Статусы всех шагов |

### Чат

| Метод | Путь | Описание |
|---|---|---|
| POST | `/api/tasks/{id}/chat/messages` | Отправить сообщение агенту |
| GET | `/api/tasks/{id}/chat/messages` | История сообщений текущей стадии |

### GitHub-вебхуки (через BFF)

| Метод | Путь (BFF) | Описание |
|---|---|---|
| POST | `/api/webhooks/github` | Приём вебхуков, валидация подписи, проксирование на бэкенд |

### SignalR-хабы (через BFF)

| Хаб | События |
|---|---|
| `/hubs/tasks` | Обновление стадии задачи, прогресс шагов |
| `/hubs/chat` | Новые сообщения, стриминг ответа агента |

## Взаимодействие компонентов

### Поток: Создание задачи и дизайн

1. Пользователь нажимает "Создать задачу" → BFF → Backend создаёт Task в БД
2. Backend через Hangfire запускает job → Kubernetes API создаёт worker pod
3. Worker pod клонирует репозиторий, запускает агент-демон, подключается к бэкенду по WebSocket
4. Backend уведомляет фронтенд через SignalR — задача готова к диалогу
5. Пользователь пишет в чат → BFF → Backend → WebSocket → Worker pod → Claude Code CLI (`claude -p`)
6. Claude Code CLI стримит ответ → Worker pod → WebSocket → Backend → SignalR → BFF → фронтенд
7. По итогам диалога агент формирует Markdown-файл дизайна, коммитит в репозиторий
8. Worker pod уведомляет бэкенд → бэкенд сохраняет контент в БД (кэш) и обновляет DesignDocument

### Поток: Генерация и утверждение плана

1. Пользователь нажимает "Сгенерировать план" → BFF → Backend → команда worker pod-у
2. Claude Code CLI анализирует дизайн + исходники, генерирует план с шагами и зависимостями
3. Агент коммитит план как файл в репозиторий
4. Worker pod отправляет структуру плана бэкенду → бэкенд создаёт Plan, Steps, StepDependency в БД
5. Пользователь утверждает план → бэкенд переводит Task в стадию Implementation

### Поток: Параллельная реализация

1. Утверждение плана → Hangfire job анализирует граф зависимостей
2. Находит шаги без зависимостей → отправляет команды worker pod-у
3. Worker pod запускает параллельные процессы `claude -p` для независимых шагов
4. Каждый процесс коммитит код, прогоняет тесты
5. Шаг завершён → worker pod уведомляет бэкенд → бэкенд обновляет Step.Status → SignalR → дашборд
6. Hangfire проверяет, разблокированы ли зависимые шаги → запускает следующие
7. Агенты проводят взаимное ревью — результаты сохраняются как Artifact
8. Все шаги завершены → Hangfire job создаёт PR через GitHub API → Task переходит в Review

### Поток: PR и завершение

1. GitHub вебхук (комментарий с `@drim-agent`) → BFF → Backend
2. Backend отправляет команду worker pod-у → агент вносит правку, коммитит
3. GitHub вебхук (PR merged) → BFF → Backend → Task переходит в Done
4. Hangfire job уничтожает worker pod

## Инфраструктура и деплой

**Хостинг:** Self-hosted Kubernetes (k3s) на Hetzner.

**Pods:**

- Frontend (Next.js) — custom server, единственная точка входа извне
- Backend (ASP.NET Core) — приватный, доступен только внутри кластера
- PostgreSQL — приватный
- Worker pods — эфемерные, создаются/уничтожаются по жизненному циклу задачи

**CI/CD:** Нет в MVP, ручной деплой.

**Мониторинг:** .NET Aspire (OpenTelemetry) — логи, метрики, трассировка. Для локальной разработки из коробки, для production — подключение к стеку по выбору (Grafana/Prometheus/Jaeger).

## Решения и компромиссы

| Решение | Альтернативы | Почему выбрано |
|---|---|---|
| BFF для всего трафика | Прямой доступ к бэкенду | Бэкенд полностью приватный, единая точка входа |
| SignalR через BFF (custom server) | SignalR напрямую к бэкенду | Соответствует требованию приватности бэкенда |
| Hangfire + PostgreSQL | Hosted services, RabbitMQ | Распределённая очередь без внешней инфраструктуры, масштабирование на несколько реплик |
| Один worker pod на задачу | Worker pod на шаг | Репозиторий клонируется один раз, контекст сохраняется между стадиями |
| Дизайн и план как файлы в репо | Только в БД | Артефакты живут вместе с кодом, версионируются через Git |
| Дублирование контента в БД | Только файлы в репо | Быстрый доступ с дашборда без обращения к worker pod |
| Self-hosted K8s на Hetzner | Managed K8s (GKE/EKS) | Экономия, достаточно для MVP с одним пользователем |
| Без CI/CD | GitHub Actions | Упрощение MVP, ручной деплой |

## Структура проекта

```
drim-agents-demo/
├── Aspire.AppHost/                # .NET Aspire оркестрация
├── Aspire.ServiceDefaults/        # Общие конфигурации Aspire
├── backend/
│   └── src/
│       ├── DrimAgents.Api/
│       │   ├── Features/          # Vertical slices
│       │   │   ├── Projects/      # CRUD проектов
│       │   │   ├── Tasks/         # Управление задачами, стадии
│       │   │   ├── Design/        # Утверждение/возврат дизайна
│       │   │   ├── Plans/         # Генерация/утверждение плана
│       │   │   ├── Steps/         # Статусы шагов
│       │   │   ├── Chat/          # Сообщения, история
│       │   │   ├── Implementation/ # Остановка реализации
│       │   │   └── Webhooks/      # GitHub-вебхуки
│       │   ├── Domain/
│       │   │   ├── Projects/      # Project
│       │   │   ├── Tasks/         # Task, DesignDocument, Plan, Step
│       │   │   ├── Chat/          # ChatSession, ChatMessage
│       │   │   └── Artifacts/     # Artifact
│       │   ├── Database/          # DbContext, миграции
│       │   ├── Hubs/              # SignalR-хабы (Tasks, Chat)
│       │   ├── Workers/           # Hangfire jobs, оркестрация
│       │   └── Common/            # Инфраструктура
│       └── DrimAgents.Api.Tests/
├── frontend/
│   ├── app/
│   │   ├── projects/              # Список проектов, дашборд
│   │   ├── tasks/                 # Детальный вид задачи
│   │   └── api/                   # BFF routes + вебхуки
│   ├── components/                # UI-компоненты
│   ├── lib/                       # Утилиты, SignalR-клиент
│   └── server.ts                  # Custom server (WebSocket proxy)
├── worker/                        # Агент-демон для worker pod
│   ├── Dockerfile
│   └── src/                       # HTTP/WebSocket сервер, управление Claude CLI
├── k8s/                           # Kubernetes-манифесты
└── docs/                          # Документация
```
