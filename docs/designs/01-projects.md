# Дизайн: Управление проектами

> Статус: **Готов к реализации**

## Скоуп

CRUD проектов: создание, просмотр списка, получение деталей, обновление. Проект — контейнер для задач, привязанный к GitHub-репозиторию. Хранение и шифрование GitHub Personal Access Token (AES-256). Проверка доступа к репозиторию через GitHub API при создании и обновлении. Изоляция данных по пользователям.

**Требования:** FR-1

## Принятые решения

| Решение | Выбор | Обоснование |
|---|---|---|
| Обновление/удаление | Update без удаления | PAT-ы протухают, описание захочется поправить. Удаление при 20-30 проектах в MVP не критично |
| Валидация GitHub URL | Формат + проверка доступа через API | Fail fast — лучше сразу сказать, что PAT невалидный, чем когда агент попытается клонировать |
| Валидация формата PAT | Не валидируем формат, только через API | Формат токенов может измениться, проверка через API уже покрывает |
| Навигация | `/projects` → список, `/projects/[id]` → канбан | Зашёл в проект — сразу видишь задачи (FR-1.5). Редактирование через модалку |
| Создание проекта | Модальное окно | Форма небольшая (4 поля), пользователь остаётся в контексте списка |
| Ключи шифрования | Раздельные ключи для пагинации и данных | Независимая ротация, разделение рисков |
| Интерфейсы шифрования | Два типизированных интерфейса | `IDataProtectionEncryption` и `IPaginationEncryption` — явная зависимость через DI |

## Доменная модель

### Сущность Project

| Поле | Тип | Описание |
|---|---|---|
| Id | long | IdGen, Base32 для API |
| UserId | long | FK → User, владелец |
| Name | string | 3–200 символов |
| Description | string? | Опциональное описание |
| GitHubRepoUrl | string | `https://github.com/{owner}/{repo}` |
| EncryptedGitHubPat | string | AES-256, хранится зашифрованным |
| CreatedAt | DateTime | UTC |
| UpdatedAt | DateTime | UTC |

**Связи:** `User` → `Projects` (one-to-many). Navigation property в обе стороны.

**Индексы:**
- `IX_projects_user_id` — для фильтрации по пользователю
- Уникальность на Name не нужна (FR-1.6 — дубликаты допустимы)

**EF-конфигурация:** таблица `projects.projects`, схема `projects`.

## Шифрование PAT

### Сервис шифрования

Единая реализация `AesEncryptionService`, два типизированных интерфейса:

```csharp
public interface IDataProtectionEncryption
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public interface IPaginationEncryption
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
```

**Реализация:** один класс `AesEncryptionService`, регистрируется в DI дважды с разными ключами:

```csharp
services.AddSingleton<IDataProtectionEncryption>(
    new AesEncryptionService(options.DataProtectionKey));
services.AddSingleton<IPaginationEncryption>(
    new AesEncryptionService(options.PaginationKey));
```

**Конфигурация:**
```json
{
  "Encryption": {
    "PaginationKey": "base64-key-256-bit",
    "DataProtectionKey": "base64-key-256-bit"
  }
}
```

**Детали AES:**
- AES-256-CBC, PKCS7 padding
- Случайный IV на каждое шифрование
- Результат: `Base64(IV + CipherText)`

**Маскировка PAT в API:**
- Отдаётся `maskedGitHubPat`: `····abcd` (последние 4 символа)
- PAT в открытом виде через API никогда не возвращается

**Рефакторинг:** существующий `LimitOffsetPaging` переходит на `IPaginationEncryption` вместо собственного шифрования.

## Backend API

### Эндпоинты

| Метод | Путь | Описание |
|---|---|---|
| POST | `/api/projects` | Создать проект |
| GET | `/api/projects` | Список проектов пользователя |
| GET | `/api/projects/{id}` | Получить проект |
| PUT | `/api/projects/{id}` | Обновить проект |

Все эндпоинты требуют аутентификации. Изоляция по `UserId` — пользователь видит только свои проекты.

### POST `/api/projects`

**Request:**
```json
{
  "name": "My Project",
  "description": "Optional description",
  "gitHubRepoUrl": "https://github.com/owner/repo",
  "gitHubPat": "ghp_xxxx"
}
```

**Логика:**
1. Валидация входных данных (FluentValidation)
2. Валидация формата URL: `https://github.com/{owner}/{repo}`
3. Проверка доступа к репозиторию через GitHub API с переданным PAT
4. Шифрование PAT через `IDataProtectionEncryption`
5. Создание сущности, сохранение

**Response:** `201 Created` с телом проекта

### GET `/api/projects`

**Response:** массив проектов пользователя, отсортированных по `CreatedAt` desc. Без пагинации (до 20–30 проектов в MVP).

### GET `/api/projects/{id}`

**Response:** проект с `maskedGitHubPat`. `404` если не найден или чужой.

### PUT `/api/projects/{id}`

**Request:**
```json
{
  "name": "Updated Name",
  "description": "Updated description",
  "gitHubRepoUrl": "https://github.com/owner/other-repo",
  "gitHubPat": "new-token-or-null"
}
```

**Логика:**
- `gitHubPat: null` — PAT не меняется
- `gitHubPat: "new-value"` — валидация через GitHub API, шифрование, перезапись
- Если URL изменился, а PAT не передан — проверяем доступ к новому репо со старым PAT
- `404` если не найден или чужой

**Response:** `200 OK` с обновлённым проектом

### DTO ответа

```json
{
  "id": "base32-encoded",
  "name": "My Project",
  "description": "...",
  "gitHubRepoUrl": "https://github.com/owner/repo",
  "maskedGitHubPat": "····abcd",
  "createdAt": "2026-02-16T...",
  "updatedAt": "2026-02-16T..."
}
```

## Валидация

### Backend (FluentValidation)

**CreateProject.RequestValidator:**
- `Name`: обязательное, 3–200 символов
- `GitHubRepoUrl`: обязательное, формат `https://github.com/{owner}/{repo}`
- `GitHubPat`: обязательное, непустая строка

**UpdateProject.RequestValidator:**
- `Name`: обязательное, 3–200 символов
- `GitHubRepoUrl`: обязательное, формат `https://github.com/{owner}/{repo}`
- `GitHubPat`: если передан — непустая строка (null допустим — означает «не менять»)

**Регулярка для GitHub URL:**
```
^https://github\.com/[a-zA-Z0-9\-\.]+/[a-zA-Z0-9\-\.\_]+$
```

**Проверка доступа к GitHub** — не в валидаторе, а в handler-е. Валидатор проверяет только формат. Handler вызывает GitHub API и бросает `ValidationException` с понятным сообщением, если репозиторий недоступен или PAT невалидный.

### Frontend (Zod)

Зеркальная схема валидации для формы создания/редактирования:
- `name`: `z.string().min(3).max(200)`
- `gitHubRepoUrl`: `z.string().url()` + regex для github.com
- `gitHubPat`: `z.string().min(1)` (при создании)
- `description`: `z.string().optional()`

Ошибки от backend (включая «репозиторий недоступен») отображаются в форме через стандартный маппинг ProblemDetails → поля формы.

### Коды ошибок

| Код | Описание |
|---|---|
| `projects:project:name:required` | Название обязательно |
| `projects:project:name:too_short` | Название < 3 символов |
| `projects:project:name:too_long` | Название > 200 символов |
| `projects:project:github_repo_url:required` | URL репозитория обязателен |
| `projects:project:github_repo_url:invalid_format` | Невалидный формат URL |
| `projects:project:github_pat:required` | PAT обязателен (при создании) |
| `projects:project:github_pat:empty` | PAT не может быть пустой строкой |
| `projects:project:github_repo:not_found` | Репозиторий не найден |
| `projects:project:github_repo:access_denied` | Нет доступа к репозиторию |
| `projects:project:github_pat:invalid` | PAT невалидный |

## Сервис проверки GitHub-репозитория

### GitHubService

Размещается в `Common/Services/`. Отвечает за взаимодействие с GitHub API.

**Интерфейс:**
```csharp
public interface IGitHubService
{
    Task<GitHubRepoInfo> ValidateRepositoryAccess(
        string repoUrl, string pat, CancellationToken ct);
}
```

**GitHubRepoInfo** — результат проверки:
```csharp
public record GitHubRepoInfo(string FullName, bool HasPushAccess);
```

**Логика:**
1. Парсит `owner/repo` из URL
2. Вызывает `GET /repos/{owner}/{repo}` с заголовком `Authorization: Bearer {pat}`
3. Проверяет `permissions.push` в ответе — пользователь должен иметь права на запись
4. Если 401/403 — бросает `ValidationException` («PAT невалидный или нет доступа»)
5. Если 404 — бросает `ValidationException` («Репозиторий не найден»)

**HttpClient:**
- Регистрируется как named/typed HttpClient через `IHttpClientFactory`
- Base address: `https://api.github.com`
- User-Agent: `DrimAgents`

Этот же сервис будет переиспользован в будущих модулях (клонирование, создание PR, вебхуки).

## Frontend

### Страницы и компоненты

**`/projects` — список проектов (Server Component):**
- Загружает список проектов через прямой вызов backend
- Отображает карточки проектов (название, описание, GitHub URL)
- Кнопка «Создать проект» — открывает модалку
- Клик по карточке → переход на `/projects/[id]`
- Пустое состояние: сообщение + кнопка создания

**`/projects/[id]` — страница проекта (Server Component):**
- Загружает детали проекта
- Заголовок с названием, описанием, ссылкой на GitHub
- Кнопка редактирования проекта → модалка с формой
- Область для канбан-доски (заглушка до модуля задач)

### Модалка создания/редактирования

**`CreateProjectModal` / `EditProjectModal` (Client Components):**
- Форма: название, описание, GitHub URL, PAT
- PAT — поле `type="password"`, при редактировании placeholder `····abcd`
- Валидация через Zod на клиенте
- Submit через BFF → backend
- При ошибке от backend (репозиторий недоступен) — показываем ошибку у соответствующего поля
- Loading-состояние кнопки во время проверки GitHub

### BFF-маршруты

| Метод | BFF путь | Проксирует на |
|---|---|---|
| POST | `/api/projects` | `POST /api/projects` |
| GET | `/api/projects` | `GET /api/projects` |
| GET | `/api/projects/[id]` | `GET /api/projects/{id}` |
| PUT | `/api/projects/[id]` | `PUT /api/projects/{id}` |

Используют существующие `proxyGet`, `proxyPost`, `proxyPut` из `lib/api.ts`.

## Тестирование

### Тесты валидаторов (unit-тесты)

Каждый файл компонентных тестов содержит вложенный класс `ValidatorTests`. Используют `FluentValidation.TestHelper`.

**CreateProjectTests.ValidatorTests:**
- Name пустой → ошибка `projects:project:name:required`
- Name < 3 символов → ошибка `projects:project:name:too_short`
- Name > 200 символов → ошибка `projects:project:name:too_long`
- Name валидный → без ошибок
- GitHubRepoUrl пустой → ошибка
- GitHubRepoUrl не github.com → ошибка `projects:project:github_repo_url:invalid_format`
- GitHubRepoUrl без owner/repo → ошибка
- GitHubRepoUrl валидный → без ошибок
- GitHubPat пустой → ошибка `projects:project:github_pat:required`
- Description не обязательное → без ошибок при null

**UpdateProjectTests.ValidatorTests:**
- Те же правила для Name и GitHubRepoUrl
- GitHubPat = null → без ошибок (означает «не менять»)
- GitHubPat = "" (пустая строка) → ошибка

### Компонентные тесты (через TestFixture)

**CreateProjectTests:**
- Создание с валидными данными → 201, проект в БД, PAT зашифрован
- Дублирующееся название → 201 (FR-1.6)
- Недоступный репозиторий → 400 (через `HttpServerHarness`)
- Без аутентификации → 401

**GetProjectsTests:**
- Возвращает только проекты текущего пользователя
- Пустой список → 200 с пустым массивом
- Порядок — по CreatedAt desc

**GetProjectTests:**
- Получение своего проекта → 200, PAT замаскирован
- Чужой проект → 404
- Несуществующий ID → 404

**UpdateProjectTests:**
- Обновление названия/описания → 200
- Обновление PAT → 200, новый PAT зашифрован
- PAT = null → PAT не меняется
- Смена URL без PAT → проверка старым PAT (через `HttpServerHarness`)
- Чужой проект → 404

### HttpServerHarness

Переиспользуемый harness для мокирования внешних HTTP-зависимостей. Подменяет `HttpMessageHandler` для named/typed HttpClient через `IHttpClientFactory`.

```csharp
fixture.HttpServer.ForClient("GitHub")
    .RespondTo(HttpMethod.Get, "/repos/owner/repo")
    .WithJson(new { full_name = "owner/repo", permissions = new { push = true } });
```

**Возможности:**
- Настройка ответов по методу + пути
- Ответы: JSON, статус-код, ошибки сети
- Привязка к named HttpClient
- Верификация: проверка что запрос был отправлен, с какими заголовками

## Структура файлов

### Backend

```
DrimAgents.Api/
├── Common/Services/
│   ├── IDataProtectionEncryption.cs
│   ├── IPaginationEncryption.cs
│   ├── AesEncryptionService.cs
│   ├── EncryptionOptions.cs
│   ├── IGitHubService.cs
│   └── GitHubService.cs
├── Domain/Projects/
│   └── Project.cs
├── Database/
│   ├── Configurations/
│   │   └── ProjectConfiguration.cs
│   └── Migrations/
│       └── {timestamp}_AddProjects.cs
├── Features/Projects/
│   ├── CreateProject.cs
│   ├── GetProjects.cs
│   ├── GetProject.cs
│   └── UpdateProject.cs
```

### Backend Tests

```
DrimAgents.Api.Tests/
├── Harnesses/
│   └── HttpServerHarness.cs
├── Features/Projects/
│   ├── CreateProjectTests.cs      (+ вложенный ValidatorTests)
│   ├── GetProjectsTests.cs
│   ├── GetProjectTests.cs
│   └── UpdateProjectTests.cs      (+ вложенный ValidatorTests)
```

### Frontend

```
frontend/
├── app/
│   ├── projects/
│   │   ├── page.tsx               (список проектов, Server Component)
│   │   └── [id]/
│   │       └── page.tsx           (страница проекта, Server Component)
│   └── api/projects/
│       ├── route.ts               (BFF: POST, GET)
│       └── [id]/
│           └── route.ts           (BFF: GET, PUT)
├── components/projects/
│   ├── ProjectCard.tsx
│   ├── ProjectList.tsx
│   ├── CreateProjectModal.tsx
│   └── EditProjectModal.tsx
├── lib/
│   └── validations/
│       └── project.ts             (Zod-схемы)
└── types/
    └── project.ts                 (ProjectDto, CreateProjectRequest, etc.)
```
