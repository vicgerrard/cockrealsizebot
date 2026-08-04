# CockRealSizeBot

Шуточный inline-бот для Telegram: «измеряет» размер мужского достоинства и отдаёт
результат карточкой прямо в тот чат, где пользователь набрал `@имябота`.

Общение с разработчиком — **на русском**. Тексты бота — тоже только на русском.

---

## Стек

| Слой | Выбор | Почему |
| --- | --- | --- |
| Хост | Worker Service (`Microsoft.NET.Sdk.Worker`), .NET 10 | Деплой на VPS без домена и сертификата |
| Telegram | `Telegram.Bot` 22.x, **long polling** | Не нужен публичный HTTPS; нагрузки inline-бота хватит с запасом |
| Хранилище | **нет** | Результат — чистая функция от `userId` + соли + даты |
| Конфигурация | Options pattern + `ValidateOnStart` | Падаем на старте, а не на первом апдейте |
| Тесты | xUnit v3 (Microsoft.Testing.Platform), `FakeTimeProvider` | Ядро детерминированное — тестируется без моков Telegram |
| Логирование | `Microsoft.Extensions.Logging` (консоль) | На VPS логи забирает journald |

Пакеты — только через central package management: версии живут в
`Directory.Packages.props`, в `.csproj` идёт `<PackageReference Include="..." />` **без** `Version`.

---

## Архитектура: Vertical Slice

Один проект, папки по фичам. Никаких Domain/Application/Infrastructure — для бота
с одной чистой функцией это ceremony без выгоды.

```
src/CockRealSizeBot.Bot/
  Program.cs                      — хост, DI, регистрация фич
  Configuration/
    BotOptions.cs                 — Token, Salt, TimeZone
  Features/
    Measurement/                  — ядро, ноль I/O
      DeterministicEntropy.cs     — HMAC-SHA256 → четыре независимых числа
      DailyCycle.cs               — границы суток в часовом поясе бота
      MeasurementTier.cs          — разряды: звание, эмодзи, прозвища
      MeasureUser.cs              — сам расчёт
    Inline/                       — ответ на inline-запрос
      MeasurementCard.cs          — все пользовательские тексты
      AnswerMeasurementQuery.cs   — обработчик
    Start/                        — экран приветствия в личке
      StartMessages.cs            — тексты
      AnswerStartCommand.cs       — обработчик, кнопка switch_inline_query
  Infrastructure/
    BotIdentity.cs                — @username бота, забирается getMe один раз
    Polling/                      — long polling и роутинг апдейтов
tests/CockRealSizeBot.Tests/
  TestSubjects.cs                 — сборка объектов под тест
  FakeTelegramBotClient.cs        — клиент, складывающий запросы в список
  Configuration/, Features/…      — зеркалит структуру src
```

Папка слайса называется `Inline`, а не `InlineQuery`, намеренно: пространство имён
`…Features.InlineQuery` конфликтовало бы с типом `Telegram.Bot.Types.InlineQuery`.

**Правило зависимостей:** `Features/Measurement` не знает ни про Telegram.Bot, ни про
хостинг — это чистый C# без I/O. Всё остальное может зависеть от него. Обратное
направление — ошибка ревью.

### Как добавлять фичи

1. Папка в `Features/<ИмяФичи>/`.
2. Внутри — всё, что нужно слайсу: модель, обработчик, форматирование текста.
3. Регистрация в DI — метод расширения `AddXxxFeature(this IServiceCollection)` в той же папке,
   вызов из `Program.cs`. Не растим `Program.cs` регистрациями по одной.
4. Тесты кладём в зеркальную папку в `tests/`.

---

## Доменные правила (не нарушать без явного решения)

- **Детерминированность.** Размер = `hash(userId + Salt + датаВTimeZone)`. Один и тот же
  пользователь в течение суток всегда получает один и тот же результат — это и есть
  «ежедневный кулдаун», отдельного состояния для него не нужно.
- **Никакого `Random`.** `Random`, `Guid.NewGuid()`, `DateTime.Now` внутри расчёта ломают
  детерминированность. Время берём только через инжектированный `TimeProvider`.
- **Соль неизменна.** Смена `Bot:Salt` в проде переписывает результаты всем сразу.
  Менять — только осознанно.
- **Границу суток** считаем по `BotOptions.TimeZone` (по умолчанию Europe/Moscow), не по UTC.
- **Stateless.** Пока не появилось требования на топ чата или историю — БД не заводим.
  Если понадобится лидерборд, это отдельное решение: сначала обсуждаем, потом ставим SQLite.
- **Никакого `string.GetHashCode`.** Он рандомизирован между запусками процесса — после
  перезапуска бота у всех сменились бы результаты. Только `DeterministicEntropy` (HMAC-SHA256).
- **Текст inline-запроса игнорируется.** Мерим всегда автора запроса. Иначе ломается
  суточный кулдаун и появляется возможность «измерить» кого-то без его участия.
- **Шкала 1–35 см, распределение колоколом** (среднее трёх равномерных величин).
  Пик приходится на 17–20 см, края выпадают редко — в этом и шутка.

### Что бот обрабатывает

`AllowedUpdates` — только `InlineQuery` и `Message`. В личке на любой текст (не только
на `/start`) приходит экран приветствия с кнопкой `switch_inline_query`. В группах бот
молчит: там он вызывается исключительно упоминанием в inline-режиме.

### Как устроен ответ

`AnswerInlineQuery` вызывается с `isPersonal: true` и `cacheTime: 0`. Оба параметра
обязательны и передаются явно:

- **`isPersonal: true`.** Без флага кэш у Telegram общий — «результаты могут быть
  показаны любому пользователю, который отправил тот же запрос». Текст запроса мы
  игнорируем, то есть запрос у всех одинаковый (пустой), и один пользователь получал бы
  карточку другого. Ровно это и случилось 4 августа 2026: два разных человека с разницей
  в четыре минуты увидели один и тот же текст.
- **`cacheTime: 0`.** Не передать параметр — не то же самое, что выключить кэш: Telegram
  подставит свой дефолт в 300 секунд.

Кэш на стороне Telegram намеренно выключен совсем. Раньше `cacheTime` равнялся времени
до локальной полуночи (результат внутри суток всё равно не меняется); при желании это
легко вернуть, но только вместе с `isPersonal`.

### Тон и контент

Грубая лексика и мат разрешены — это решение владельца проекта, смягчать формулировки
по своей инициативе не надо. Прозвища в `MeasurementTier.cs` намеренно колеблются
от безобидных до матерных.

Что остаётся за границей: тексты не должны быть направлены на конкретного человека
(бот меряет только автора запроса — это в том числе про это) и не должны быть
порнографическим описанием. Ориентир — Telegram ToS.

---

## Конвенции кода

- Современный C# 14: file-scoped namespaces, primary constructors, collection expressions,
  `required` члены, records для сообщений/DTO, pattern matching вместо цепочек `if`.
- Классы по умолчанию `sealed`. Не `sealed` — только если наследование действительно нужно.
- `CancellationToken` пробрасывается через все async-методы до самого низа. Параметр — последний.
- Асинхронные методы — суффикс `Async`. `async void` запрещён (кроме обработчиков событий, которых тут нет).
- `TreatWarningsAsErrors=true` — предупреждения чинятся, а не подавляются. `#pragma warning disable`
  требует комментария с причиной.
- Логирование — только через source-generated `[LoggerMessage]` в `partial`-классе
  (этого требует CA1848). `logger.LogInformation("...")` не соберётся.
- `InvariantGlobalization` выключен осознанно: без ICU на Windows не резолвятся
  IANA-идентификаторы часовых поясов, а граница суток считается именно по ним.
- Тексты для пользователя не разбросаны по коду: держим их в одном месте слайса
  (константы/`Messages`-класс), чтобы правки формулировок не превращались в поиск по проекту.

---

## Секреты

Токен и соль **никогда** не попадают в `appsettings.json` и в репозиторий.

Локально:

```bash
dotnet user-secrets set "Bot:Token" "<токен от @BotFather>" --project src/CockRealSizeBot.Bot
dotnet user-secrets set "Bot:Salt"  "<длинная случайная строка>" --project src/CockRealSizeBot.Bot
```

На VPS — переменные окружения: `Bot__Token`, `Bot__Salt` (двойное подчёркивание).

---

## Команды

```bash
dotnet build CockRealSizeBot.slnx              # сборка
dotnet test --solution CockRealSizeBot.slnx    # тесты (MTP требует --solution)
dotnet format CockRealSizeBot.slnx             # форматирование
dotnet run --project src/CockRealSizeBot.Bot
```

Тестовый раннер — Microsoft.Testing.Platform (см. `global.json`), поэтому у `dotnet test`
другой синтаксис, чем у VSTest, а прогон без единого теста считается ошибкой (exit 8).

Имена тестов — `Method_does_something_when_condition`, подчёркивания разрешены:
CA1707 отключён для всего `tests/` в `tests/Directory.Build.props`.

---

## Деплой

Ubuntu VPS + Docker. Порты наружу не публикуются: бот сам ходит в Telegram,
входящих соединений нет.

Схема: push в `main` → GitHub Actions гоняет сборку, формат и тесты → собирает
образ и кладёт в GHCR → на VPS `docker compose pull && docker compose up -d`.

### Обычный деплой

```bash
ssh vps
cd /opt/cockrealsizebot
docker compose pull && docker compose up -d
docker compose logs -f --tail 50
```

### Откат

Каждая сборка тегируется ещё и как `sha-<полный-хэш-коммита>`. Откат — это правка
одной строки:

```bash
sed -i 's|:latest|:sha-<хэш>|' .env
docker compose up -d
```

### Первая настройка VPS

```bash
mkdir -p /opt/cockrealsizebot && cd /opt/cockrealsizebot
# положить сюда compose.yaml и .env.example из репозитория
cp .env.example .env && chmod 600 .env && nano .env   # BOT_IMAGE, Bot__Token, Bot__Salt

# Для приватного пакета в GHCR нужен PAT с правом read:packages.
echo "<PAT>" | docker login ghcr.io -u <аккаунт> --password-stdin

docker compose up -d
```

### Чего в контейнере нет и почему

- **Хелсчека нет.** HTTP-эндпоинта у бота тоже нет, а проверять «жив ли процесс»
  докер и так умеет. Необработанная ошибка опроса роняет хост целиком
  (`BackgroundServiceExceptionBehavior.StopHost`), контейнер выходит,
  `restart: unless-stopped` поднимает заново — это и есть механизм восстановления.
- **Тестов в образе нет:** `tests/` исключён через `.dockerignore`, гоняет их CI.

### Ловушка при смене базового образа

`InvariantGlobalization` выключен, а граница суток считается по IANA-зоне.
Образу нужны ICU **и** tzdata. В `mcr.microsoft.com/dotnet/runtime:10.0` (Ubuntu)
оба есть из коробки — проверено. В chiseled и alpine их нет, там
`FindSystemTimeZoneById` упадёт на старте.

---

## Настройка бота в BotFather

| Команда | Нужно | Зачем |
| --- | --- | --- |
| `/setinline` | **обязательно** | Inline-режим выключен по умолчанию, без него запросы не придут вообще |
| `/setdescription` | да | Текст на пустом экране чата |
| `/setabouttext` | да | Строка в профиле, до 120 символов |
| `/setuserpic` | да | По аватарке бота узнают в списке inline-ботов |
| `/setjoingroups` → Disable | да | Inline работает без добавления в группу; запрет убирает бессмысленный сценарий |
| `/setcommands` | опционально | Работает только `/start`; Telegram и так показывает кнопку Start |
| `/setinlinefeedback` | нет | Нужна только для `ChosenInlineResult`, мы его не запрашиваем |
| `/setinlinegeo` | нет | Геолокация не используется |

Placeholder задаётся внутри диалога `/setinline` — серый текст в поле ввода после
упоминания бота.

После `/setinline` клиент Telegram иногда показывает inline-подсказки не сразу.
Пустая выпадашка после запуска — сначала перезапустить клиент, потом лезть в логи.

---

## Что дальше

- `/verify` — прогон всех проверок перед коммитом.
- `/health-check` — оценка состояния проекта.
- Не сделано и ждёт решения: реакция на `ChosenInlineResult` (статистика по
  отправленным карточкам), автодеплой по ssh из CI вместо ручного `compose pull`.
