# План: Создание инфраструктуры на основе готовой системы

## Обзор

Создание инфраструктурного каркаса проекта AI Agents Orchestrator на основе структуры готовой системы из папки `another/`. Архитектура: Frontend (Next.js) + BFF (Next.js API Routes) + Backend (ASP.NET Core).

---

## Фаза 1: Структура проекта и конфигурация

### 1.1 Корневая инфраструктура

- [ ] Создать `DrimAgents.sln` (solution file)
- [ ] Обновить `.gitignore` (добавить .NET, Node.js, IDE)
- [ ] Создать `Aspire.AppHost/` — проект оркестрации .NET Aspire
- [ ] Создать `Aspire.ServiceDefaults/` — общие конфигурации Aspire

### 1.2 Backend (ASP.NET Core)

- [ ] Создать `backend/src/DrimAgents.Api/` — основной API-проект
  - [ ] `Program.cs` — точка входа с регистрацией сервисов
  - [ ] `appsettings.json` — конфигурация (подключение к БД, IdGen, Paging)
  - [ ] `DrimAgents.Api.csproj` — зависимости (MediatR, FluentValidation, EF Core, IdGen)
- [ ] Создать `backend/src/DrimAgents.Api/Common/` — общая инфраструктура по concern-ам:
  - [ ] `Common/Auth/` — UserContextMiddleware (конвертация BFF-заголовков в Claims)
  - [ ] `Common/Exceptions/` — доменные исключения + глобальный обработчик (ProblemDetails)
  - [ ] `Common/Http/` — IEndpoint, HttpContextExtensions
  - [ ] `Common/Identity/` — IdFactory (IdGen), Base32Encoder
  - [ ] `Common/Validation/` — ValidationBehavior (MediatR pipeline)
  - [ ] `Common/Pagination/` — LimitOffsetPaging, PageResponse<T>, PaginationExceptions
  - [ ] `Common/Options/` — PagingOptions
- [ ] Создать `backend/src/DrimAgents.Api/Database/` — инфраструктура БД:
  - [ ] `AppDbContext.cs`
  - [ ] `Configurations/` — EF Fluent API конфигурации
  - [ ] `Migrations/` — миграции EF Core
- [ ] Создать `backend/src/DrimAgents.Api/Domain/` — доменные сущности (пока пусто)
- [ ] Создать `backend/src/DrimAgents.Api/Features/` — вертикальные слайсы (пока пусто)

### 1.3 Backend Tests

- [ ] Создать `backend/src/DrimAgents.Api.Tests/` — проект тестов
  - [ ] `DrimAgents.Api.Tests.csproj` — зависимости (xUnit, FluentAssertions, TestContainers, Respawn)
  - [ ] `Harnesses/` — DatabaseHarness, HttpClientHarness
  - [ ] `TestFixture.cs` — общая фикстура для тестов

### 1.4 Frontend (Next.js)

- [ ] Создать `frontend/` — Next.js приложение:
  - [ ] `package.json` — зависимости (Next.js, React, TypeScript, Tailwind, next-auth, zod, react-hook-form, zustand)
  - [ ] `tsconfig.json` — strict mode, path aliases
  - [ ] `next.config.ts`
  - [ ] `tailwind.config.ts`
  - [ ] `auth.ts` — конфигурация NextAuth.js (OAuth: Google, GitHub, GitLab)
- [ ] Создать структуру `frontend/app/`:
  - [ ] `layout.tsx` — корневой layout
  - [ ] `page.tsx` — главная страница
  - [ ] `app/api/` — BFF API routes
  - [ ] `app/auth/` — страницы авторизации
- [ ] Создать `frontend/components/ui/` — базовые UI-компоненты
- [ ] Создать `frontend/lib/` — утилиты
- [ ] Создать `frontend/hooks/` — кастомные хуки
- [ ] Создать `frontend/types/` — TypeScript типы
- [ ] Создать `frontend/stores/` — Zustand стейт
- [ ] Создать `frontend/styles/` — глобальные стили
- [ ] Создать `frontend/DESIGN_SYSTEM.md` — спецификация дизайн-системы

---

## Фаза 2: Перенос скиллов с переводом на русский

Перенос инфраструктурных скиллов из `another/.claude/skills/` в `.claude/skills/` с переводом на русский.

### 2.1 Скиллы для бэкенда (обязательные при реализации фичей)

- [ ] `vertical-slice-architecture` — Вертикальные слайсы: структура фичей, MediatR, FluentValidation, IEndpoint, Options pattern
- [ ] `component-testing` — Компонентное тестирование: harness-подход, TestContainers, xUnit коллекции
- [ ] `validation` — Валидация: FluentValidation (бэкенд) + Zod (фронтенд), ProblemDetails, коды ошибок
- [ ] `error-handling` — Обработка ошибок: доменные исключения, ProblemDetails (RFC 7807), HTTP-коды
- [ ] `id-generation` — Генерация ID: IdGen + Crockford Base32, стратегия Slug + Long ID
- [ ] `token-pagination` — Пагинация: AIP-158, зашифрованные токены, валидация параметров запроса

### 2.2 Скилл для рабочего процесса

- [ ] `spec-maintenance` — Поддержка спецификаций: обновление модульных спеков в `docs/specs/`

### 2.3 Скиллы НЕ переносятся

- ~~`design-brainstorming`~~ — не нужен
- ~~`lesson-presentations`~~ — специфичен для обучающей платформы
- ~~`content-visualization`~~ — специфичен для обучающей платформы

---

## Фаза 3: Обновление CLAUDE.md

Перенос только необходимой информации из `another/CLAUDE.md`, которая НЕ дублирует скиллы. Перевод на русский, адаптация под текущий проект.

### 3.1 Заполнить «Обзор проекта» (сейчас TODO)

- [ ] Описание проекта, архитектура (Frontend + BFF + Backend), технический стек

### 3.2 Добавить «.NET Aspire»

- [ ] Service Discovery, Observability, структура проектов (AppHost, ServiceDefaults)

### 3.3 Добавить «Структура проекта»

- [ ] Дерево каталогов с кратким описанием каждой папки

### 3.4 Добавить «Vertical Slice Architecture (Backend)» (краткое описание + отсылка к скиллу)

- [ ] Суть паттерна, неймспейсы `DrimAgents.Api.Features.{Domain}`
- [ ] Список обязательных скиллов перед реализацией бэкенд-фичей

### 3.5 Добавить «Организация фронтенда (Next.js)»

- [ ] Server Components по умолчанию, Server Actions для мутаций
- [ ] Дизайн-система: `frontend/DESIGN_SYSTEM.md`

### 3.6 Добавить «Аутентификация и авторизация»

- [ ] OAuth 2.0 только, роли (User, Admin), политики на уровне эндпоинтов

### 3.7 Добавить «База данных»

- [ ] PostgreSQL + EF Core, code-first, AsNoTracking(), Select()

### 3.8 Добавить «API-коммуникация»

- [ ] Server Components → Backend (напрямую), Client Components → BFF → Backend

### 3.9 Добавить «Стандарты тестирования»

- [ ] Все падения тестов — ответственность разработчика, не удалять тесты, не тестировать моки

### 3.10 Добавить «TypeScript»

- [ ] Strict mode, no `any`, prefer interfaces, discriminated unions

### 3.11 Добавить «Добавление зависимостей»

- [ ] NuGet: последние стабильные версии, правила для npm

---

## Фаза 4: Аутентификация (end-to-end)

Полный флоу: открыть сайт → залогиниться через OAuth → аккаунт сохранён в БД → выйти → зайти снова.

### 4.1 Backend: доменная модель пользователя

- [ ] `Domain/Users/User.cs` — сущность (Id, Email, Name, AvatarUrl, Provider, ProviderAccountId, Role, CreatedAt, LastLoginAt)
- [ ] `Database/Configurations/UserConfiguration.cs` — EF конфигурация (уникальный индекс по Provider+ProviderAccountId)
- [ ] Миграция: создание таблицы Users

### 4.2 Backend: эндпоинт OAuth callback

- [ ] `Features/Users/HandleOAuthCallback.cs` — вертикальный слайс:
  - BFF отправляет данные OAuth-пользователя после успешной авторизации
  - Если пользователь существует (по Provider+ProviderAccountId) — обновить LastLoginAt, вернуть данные
  - Если нет — создать нового пользователя, вернуть данные
- [ ] `Features/Users/GetCurrentUser.cs` — получить текущего пользователя по заголовкам BFF

### 4.3 Frontend: NextAuth.js

- [ ] `auth.ts` — конфигурация NextAuth.js с провайдером GitHub (для начала один)
- [ ] `app/api/auth/[...nextauth]/route.ts` — API route для NextAuth
- [ ] Callback в NextAuth: после OAuth → вызов бэкенда (HandleOAuthCallback) → сохранение данных пользователя в сессии

### 4.4 Frontend: UI аутентификации

- [ ] `app/auth/signin/page.tsx` — страница логина с кнопкой «Войти через GitHub»
- [ ] `app/page.tsx` — главная страница: показать имя пользователя если залогинен, кнопку «Войти» если нет
- [ ] Кнопка «Выйти» — вызов signOut() из NextAuth
- [ ] `middleware.ts` — защита маршрутов (опционально, для будущих защищённых страниц)

### 4.5 Интеграция BFF → Backend

- [ ] BFF передаёт заголовки пользователя (X-User-Id, X-User-Email, X-User-Role) при запросах к бэкенду
- [ ] `Common/Auth/UserContextMiddleware.cs` — парсинг заголовков в Claims
- [ ] Проверка: логин → данные в БД → выход → повторный логин → тот же аккаунт

---

## Порядок выполнения

1. **Фаза 1** — каркас проекта
2. **Фаза 3** — обновление CLAUDE.md
3. **Фаза 2** — перенос скиллов с переводом
4. **Фаза 4** — аутентификация (первая рабочая фича)

## Примечания

- Все тексты на русском языке
- Неймспейсы: `DrimAgents.Api.*` вместо `DrimDev.Api.*`
- Домены: будут определены позже (предварительно: Agents, Tasks, Projects, Users)
- Скиллы переводятся и адаптируются под неймспейсы текущего проекта
