# Scrutor: логирование отменённых HTTP-запросов — пример кода

[English](README.md) | [Русский](README.ru.md)

Рабочий пример к статье про подавление шумных `OperationCanceledException` в логах через декоратор `ILoggerProvider`, зарегистрированный с помощью [Scrutor](https://github.com/khellang/Scrutor), плюс обе неудачные попытки решения и два дополнительных сценария декорирования того же вида.

## Структура

- `src/Demo.Logging` — рабочее решение: `CancelledHttpLoggerProvider` + `CancelledHttpLogger`.
- `src/Demo.FailedAttempts` — неудачные попытки: фильтр в `nlog.config` и middleware, которое перехватывает исключение слишком поздно (см. `README.md` внутри).
- `src/Demo.Scenarios` — два дополнительных сценария декорирования через Scrutor: retry-обёртка вокруг HTTP-клиента и аудит изменений через `IHttpContextAccessor`.
- `src/Demo.Api` — минимальный ASP.NET Core API, в котором всё это зарегистрировано и подключено вместе.
- `src/Demo.Benchmarks` — бенчмарки на BenchmarkDotNet: сравнение вызова `ILogger.Log(...)` без декоратора, с декоратором (лог проходит насквозь) и с декоратором (лог подавляется).
- `tests/Demo.Tests` — тесты, подтверждающие поведение из статьи: подавление отмен, отсутствие `NullReferenceException` вне HTTP-запроса, декорирование сразу всех `ILoggerProvider`, ретраи и аудит.

## Запуск

```bash
dotnet test
dotnet run --project src/Demo.Api
dotnet run -c Release --project src/Demo.Benchmarks
```

Эндпоинт `GET /slow` эмулирует долгую операцию на 10 секунд — если оборвать запрос раньше (закрыть вкладку браузера или `curl --max-time 1 http://localhost:<port>/slow`), в консоли не появится запись об ошибке: `CancelledHttpLoggerProvider` её подавит.

Бенчмарки нужно запускать в конфигурации `Release` (BenchmarkDotNet сам откажется работать в `Debug`). Абсолютные числа зависят от железа, на котором запускаете, — воспроизводить стоит соотношение между сценариями и отсутствие аллокаций, а не конкретные наносекунды из статьи.
