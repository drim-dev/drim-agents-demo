# План реализации: Управление проектами

> Источник: `docs/designs/01-projects.md`

## Обзор

4 эндпоинта (CreateProject, GetProjects, GetProject, UpdateProject), сервис шифрования AES-256, сервис GitHub API, фронтенд (список + страница проекта + модалки), BFF-маршруты, компонентные тесты.

## Слои зависимостей

```
Слой 0 (нет зависимостей — параллельно):
  - AesEncryptionService + интерфейсы
  - Сущность Project + EF-конфигурация + миграция
  - HttpServerHarness (тестовый harness для HTTP-моков)
  - TypeScript типы + Zod-схемы

Слой 1 (зависит от Слоя 0):
  - GitHubService (зависит от шифрования для тестов)
  - Рефакторинг LimitOffsetPaging → IPaginationEncryption
  - Регистрация DI в Program.cs

Слой 2 (зависит от Слоя 1):
  - CreateProject (зависит от GitHubService, шифрования, сущности)
  - GetProjects (зависит от сущности)
  - GetProject (зависит от сущности, шифрования для маскировки)
  - UpdateProject (зависит от GitHubService, шифрования, сущности)

Слой 3 (зависит от Слоя 2):
  - Компонентные тесты всех 4 эндпоинтов

Слой 4 (зависит от Слоя 2):
  - BFF-маршруты
  - Фронтенд страницы и компоненты
```

---

## Задачи

### Слой 0: Инфраструктура (параллельно)

#### 0.1. Сервис шифрования AES-256
- [x] Создать `Common/Services/IDataProtectionEncryption.cs` — интерфейс `Encrypt(string) → string`, `Decrypt(string) → string`
- [x] Создать `Common/Services/IPaginationEncryption.cs` — тот же контракт
- [x] Создать `Common/Services/AesEncryptionService.cs` — реализация AES-256-CBC, PKCS7, случайный IV на каждое шифрование, результат `Base64(IV + CipherText)`
- [x] Создать `Common/Options/EncryptionOptions.cs` — `PaginationKey` (string, base64) и `DataProtectionKey` (string, base64)

**Детали реализации:**
- Один класс `AesEncryptionService` реализует оба интерфейса
- В DI регистрируется дважды с разными ключами
- **Отличие от LimitOffsetPaging**: здесь случайный IV на каждое шифрование (более безопасно для данных), в пагинации — фиксированный IV (детерминированные токены)

#### 0.2. Доменная модель Project
- [x] Создать `Domain/Projects/Project.cs` — сущность с полями: `Id` (long), `UserId` (long), `Name` (string), `Description` (string?), `GitHubRepoUrl` (string), `EncryptedGitHubPat` (string), `CreatedAt` (DateTime), `UpdatedAt` (DateTime), navigation property `User`
- [x] Создать `Database/Configurations/ProjectConfiguration.cs` — таблица `projects.projects`, схема `projects`, FK к `users.users`, индекс `IX_projects_user_id` по `UserId`
- [x] Добавить `DbSet<Project> Projects` в `AppDbContext.cs`
- [x] Добавить navigation property `ICollection<Project> Projects` в сущность `User`
- [x] Создать EF-миграцию `AddProjects`

#### 0.3. HttpServerHarness (тестовая инфраструктура)
- [x] Создать `DrimAgents.Api.Tests/Harnesses/HttpServerHarness.cs`
  - Подменяет `HttpMessageHandler` для named HttpClient через DI
  - API: `ForClient("GitHub").RespondTo(HttpMethod.Get, "/repos/owner/repo").WithJson(...)` / `.WithStatusCode(404)` / `.WithError()`
  - Верификация: проверка что запрос был отправлен, с какими заголовками
  - Метод `Reset()` для очистки между тестами
- [x] Подключить `HttpServerHarness` в `TestFixture` — добавить свойство `HttpServer`, вызвать `Reset()` в `Reset()`

#### 0.4. TypeScript типы и Zod-схемы
- [x] Добавить в `types/api.ts` (или создать `types/project.ts`): `ProjectDto`, `CreateProjectRequest`, `UpdateProjectRequest`
- [x] Создать `lib/validations/project.ts` — Zod-схемы: `createProjectSchema`, `updateProjectSchema`
  - `name`: `z.string().min(3).max(200)`
  - `gitHubRepoUrl`: `z.string().url()` + regex `^https://github\.com/[a-zA-Z0-9\-\.]+/[a-zA-Z0-9\-\.\_]+$`
  - `gitHubPat`: `z.string().min(1)` (при создании обязателен, при обновлении опционален)
  - `description`: `z.string().optional()`

---

### Слой 1: Сервисы и DI

#### 1.1. GitHubService
- [x] Создать `Common/Services/IGitHubService.cs` — интерфейс с методом `ValidateRepositoryAccess(string repoUrl, string pat, CancellationToken ct) → GitHubRepoInfo`
- [x] Создать `Common/Services/GitHubRepoInfo.cs` — record `GitHubRepoInfo(string FullName, bool HasPushAccess)`
- [x] Создать `Common/Services/GitHubService.cs` — реализация:
  - Инжектирует `IHttpClientFactory`, получает named client `"GitHub"`
  - Парсит `owner/repo` из URL через regex
  - Вызывает `GET /repos/{owner}/{repo}` с `Authorization: Bearer {pat}`, `User-Agent: DrimAgents`
  - Проверяет `permissions.push` в ответе
  - При 401/403 → бросает `ValidationException` с кодом `projects:project:github_pat:invalid` или `projects:project:github_repo:access_denied`
  - При 404 → бросает `ValidationException` с кодом `projects:project:github_repo:not_found`

#### ~~1.2. Рефакторинг LimitOffsetPaging~~ — ОТЛОЖЕН
> `LimitOffsetPaging` не трогаем. У него фиксированный IV для детерминированных токенов, `AesEncryptionService` использует случайный IV — несовместимо. `IPaginationEncryption` не регистрируем.

#### 1.3. Регистрация DI в Program.cs
- [x] Зарегистрировать `EncryptionOptions`: `builder.Services.Configure<EncryptionOptions>(builder.Configuration.GetSection("Encryption"))`
- [x] Зарегистрировать `IDataProtectionEncryption`: singleton с ключом из `EncryptionOptions.DataProtectionKey`
- [x] Зарегистрировать `IPaginationEncryption`: singleton с ключом из `EncryptionOptions.PaginationKey`
- [x] Зарегистрировать `IGitHubService` + named HttpClient `"GitHub"` (base address `https://api.github.com`, User-Agent `DrimAgents`, Accept `application/vnd.github+json`)
- [x] Добавить ключи шифрования в `appsettings.Development.json`

---

### Слой 2: Вертикальные слайсы (параллельно)

#### 2.1. CreateProject
- [x] Создать `Features/Projects/CreateProject.cs`
  - **Endpoint**: `POST /api/projects`, `.RequireAuthorization()`, извлекает `UserId` из `HttpContext`
  - **Request**: `Name`, `Description`, `GitHubRepoUrl`, `GitHubPat` → `IRequest<Response>`
  - **Response**: `Id` (Base32), `Name`, `Description`, `GitHubRepoUrl`, `MaskedGitHubPat`, `CreatedAt`, `UpdatedAt`
  - **RequestValidator**: Name (required, 3-200), GitHubRepoUrl (required, regex), GitHubPat (required, not empty)
  - **RequestHandler**: валидация GitHub через `IGitHubService`, шифрование PAT через `IDataProtectionEncryption`, создание сущности с `IIdFactory`, сохранение
  - Возвращает `201 Created` с Location header

#### 2.2. GetProjects
- [x] Создать `Features/Projects/GetProjects.cs`
  - **Endpoint**: `GET /api/projects`, `.RequireAuthorization()`, извлекает `UserId`
  - **Request**: `UserId` → `IRequest<Response>`
  - **Response**: массив `ProjectItem` (без пагинации — до 20-30 проектов в MVP)
  - **RequestHandler**: `AsNoTracking()`, фильтр по `UserId`, `OrderByDescending(p => p.CreatedAt)`, `Select()` проекция
  - Маскировка PAT в проекции: расшифровать → взять последние 4 символа → `····xxxx`

#### 2.3. GetProject
- [x] Создать `Features/Projects/GetProject.cs`
  - **Endpoint**: `GET /api/projects/{id}`, `.RequireAuthorization()`, декодирует Base32 ID, извлекает `UserId`
  - **Request**: `ProjectId` (long), `UserId` (long) → `IRequest<Response?>`
  - **Response**: полный DTO проекта с `MaskedGitHubPat`
  - **RequestHandler**: `AsNoTracking()`, фильтр по `Id` и `UserId`, `Select()` проекция. Если не найден → `NotFoundException`

#### 2.4. UpdateProject
- [x] Создать `Features/Projects/UpdateProject.cs`
  - **Endpoint**: `PUT /api/projects/{id}`, `.RequireAuthorization()`, декодирует Base32 ID, извлекает `UserId`
  - **Request**: `ProjectId`, `UserId`, `Name`, `Description`, `GitHubRepoUrl`, `GitHubPat?` → `IRequest<Response>`
  - **RequestValidator**: Name (required, 3-200), GitHubRepoUrl (required, regex), GitHubPat (если передан — not empty)
  - **RequestHandler**:
    1. Найти проект по `Id` + `UserId` (tracking). Если не найден → `NotFoundException`
    2. Если `GitHubPat` передан — валидация нового PAT через `IGitHubService`, шифрование
    3. Если URL изменился и PAT не передан — расшифровать старый PAT, проверить доступ к новому repo
    4. Обновить поля, `UpdatedAt = DateTime.UtcNow`
    5. Вернуть обновлённый DTO

---

### Слой 3: Компонентные тесты (параллельно)

#### 3.1. Тестовая коллекция и тесты CreateProject
- [x] Создать `Tests/Features/Projects/ProjectsTestsCollection.cs`
- [x] Создать `Tests/Features/Projects/CreateProjectTests.cs`
  - **ValidatorTests** (вложенный класс):
    - Name пустой → `projects:project:name:required`
    - Name < 3 → `projects:project:name:too_short`
    - Name > 200 → `projects:project:name:too_long`
    - Name валидный → без ошибок
    - GitHubRepoUrl пустой → ошибка
    - GitHubRepoUrl не github.com → `projects:project:github_repo_url:invalid_format`
    - GitHubRepoUrl без owner/repo → ошибка
    - GitHubRepoUrl валидный → без ошибок
    - GitHubPat пустой → `projects:project:github_pat:required`
    - Description null → без ошибок
  - **Компонентные тесты**:
    - Создание с валидными данными → 201, проект в БД, PAT зашифрован
    - Дублирующееся название → 201 (дубликаты допустимы)
    - Недоступный репозиторий → 400 (GitHub мок возвращает 404)
    - Невалидный PAT → 400 (GitHub мок возвращает 401)
    - Без аутентификации → 401

#### 3.2. Тесты GetProjects
- [x] Создать `Tests/Features/Projects/GetProjectsTests.cs`
  - Возвращает только проекты текущего пользователя (изоляция)
  - Пустой список → 200 с пустым массивом
  - Порядок — по CreatedAt desc
  - PAT замаскирован в ответе
  - Без аутентификации → 401

#### 3.3. Тесты GetProject
- [x] Создать `Tests/Features/Projects/GetProjectTests.cs`
  - Получение своего проекта → 200, PAT замаскирован
  - Чужой проект → 404
  - Несуществующий ID → 404
  - Без аутентификации → 401

#### 3.4. Тесты UpdateProject
- [x] Создать `Tests/Features/Projects/UpdateProjectTests.cs`
  - **ValidatorTests** (вложенный класс):
    - Те же правила для Name и GitHubRepoUrl, что и в Create
    - GitHubPat = null → без ошибок
    - GitHubPat = "" → ошибка `projects:project:github_pat:empty`
  - **Компонентные тесты**:
    - Обновление названия/описания → 200
    - Обновление PAT → 200, новый PAT зашифрован
    - PAT = null → PAT не меняется
    - Смена URL без PAT → проверка старым PAT (мок GitHub)
    - Чужой проект → 404
    - Без аутентификации → 401

---

### Слой 4: Фронтенд (параллельно)

#### 4.1. BFF-маршруты
- [x] Создать `app/api/projects/route.ts` — `GET` (proxyGet), `POST` (proxyPost)
- [x] Создать `app/api/projects/[id]/route.ts` — `GET` (proxyGet), `PUT` (proxyPut)

#### 4.2. Страница списка проектов
- [x] Создать `app/projects/page.tsx` (Server Component)
  - Защита: `requireAuth()` с редиректом на логин
  - Загрузка проектов через `forwardToBackend("/api/projects")`
  - Пустое состояние: сообщение + кнопка «Создать проект»
  - Список карточек `ProjectCard`
  - Кнопка «Создать проект» → открывает `CreateProjectModal`
  - Обёрнуть интерактивную часть в Client Component (`ProjectsPageContent`)

#### 4.3. Страница проекта
- [x] Создать `app/projects/[id]/page.tsx` (Server Component)
  - Защита: `requireAuth()`
  - Загрузка проекта через `forwardToBackend("/api/projects/{id}")`
  - Если 404 → `notFound()`
  - Заголовок с названием, описанием, ссылкой на GitHub
  - Кнопка редактирования → `EditProjectModal`
  - Заглушка для канбан-доски

#### 4.4. Компоненты проектов
- [x] Создать `components/projects/ProjectCard.tsx` — карточка проекта (название, описание, GitHub URL, дата)
- [x] Создать `components/projects/ProjectList.tsx` — обёртка списка карточек
- [x] Создать `components/projects/CreateProjectModal.tsx` (Client Component)
  - Форма: name, description, gitHubRepoUrl, gitHubPat (type="password")
  - Валидация через Zod (`createProjectSchema`)
  - Submit через `apiPost("/api/projects", data)`
  - Обработка ошибок от backend (ProblemDetails → поля формы)
  - Loading-состояние кнопки
  - При успехе: закрыть модалку, `router.refresh()`
- [x] Создать `components/projects/EditProjectModal.tsx` (Client Component)
  - Аналогично Create, но:
  - PAT placeholder `····xxxx` (из `maskedGitHubPat`)
  - PAT опционален (null = не менять)
  - Submit через `apiPut("/api/projects/{id}", data)`

---

## Распределение по агентам

### Агент 1: Backend Infrastructure
**Задачи:** 0.1, 0.2, 1.1, 1.2, 1.3
- AesEncryptionService + интерфейсы + options
- Project entity + EF config + миграция + DbContext
- GitHubService
- Рефакторинг/регистрация DI в Program.cs
- appsettings

### Агент 2: Backend Features
**Задачи:** 2.1, 2.2, 2.3, 2.4
- Все 4 вертикальных слайса
- **Блокер:** ждёт завершения Агента 1 (инфраструктура)

### Агент 3: Test Infrastructure + Tests
**Задачи:** 0.3, 3.1, 3.2, 3.3, 3.4
- HttpServerHarness
- Тестовая коллекция
- Все компонентные тесты + validator tests
- **Блокер:** HttpServerHarness можно делать сразу, тесты — после Агентов 1 и 2

### Агент 4: Frontend
**Задачи:** 0.4, 4.1, 4.2, 4.3, 4.4
- TypeScript типы + Zod-схемы
- BFF-маршруты
- Страницы и компоненты
- **Блокер:** нет (BFF и фронт можно делать параллельно с backend)

---

## Порядок выполнения (оптимальный)

```
Время →

Агент 1: [0.1 Шифрование] [0.2 Entity+Migration] [1.1 GitHubService] [1.3 DI] ──── done
Агент 2:                          ожидание...                              [2.1-2.4 Features] ── done
Агент 3: [0.3 HttpServerHarness]        ожидание...                              [3.1-3.4 Tests] ── done
Агент 4: [0.4 Types+Zod] [4.1 BFF] [4.2 Projects page] [4.3 Project page] [4.4 Components] ── done
```

**Агенты 1 и 4 стартуют сразу параллельно.** Агент 3 делает harness сразу, потом ждёт. Агент 2 ждёт Агента 1.

## Проверка после реализации

- [x] `dotnet build DrimAgents.sln` — успешная сборка
- [x] `dotnet test` — все тесты проходят
- [x] `npm run build` (frontend) — успешная сборка
- [ ] Ручная проверка: создание проекта, просмотр списка, просмотр деталей, обновление
