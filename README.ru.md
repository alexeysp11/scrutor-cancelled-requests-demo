# Подавление логов отмены HTTP-запросов с помощью Scrutor

Пример подавления логов `OperationCanceledException`, возникающих при обрыве соединения клиентом в ASP.NET Core. Решение реализовано через декоратор `ILoggerProvider` с использованием библиотеки [Scrutor](https://github.com).

## Структура проекта

- `src/Demo.Logging` — Компоненты решения (`CancelledHttpLoggerProvider` и `CancelledHttpLogger`).
- `src/Demo.Api` — Минимальный API на ASP.NET Core с регистрацией декоратора.
- `src/Demo.Benchmarks` — Бенчмарки BenchmarkDotNet для сравнения производительности прямого и декорированного логирования.
- `tests/Demo.Tests` — Юнит- и интеграционные тесты.

---

## Запуск приложения

```bash
dotnet run --project src/Demo.Api
```
Сервер слушает HTTP (порт `5241`) и HTTPS (порт `7040`).

---

## Проверка через curl

Для проверки поведения используется эндпоинт `GET /slow` (эмулирует задержку в 3 секунды):

### Тестирование через HTTPS
* **Успешный запрос:**
  ```bash
  curl -k -L --max-time 5 https://localhost:7040/slow
  ```
  *Логи приложения:* Появятся обе записи `"Started..."` и `"Finished..."`.

* **Прерванный запрос (Проверка подавления):**
  ```bash
  curl -k -L --max-time 1 https://localhost:7040/slow
  ```
  *Логи приложения:* Выведется **только** запись `"Started..."`. Системный стек-трейс ошибки `OperationCanceledException` от Kestrel/ASP.NET Core будет подавлен.

### Тестирование через HTTP
* **Прерывание на старте:** `curl --max-time 1 http://localhost:5241/slow` (Запрос завершается на этапе Middleware редиректа; подавление лога эндпоинта не происходит).
* **Выполнение до конца:** `curl --max-time 5 http://localhost:5241/slow` (Возвращает статус `307 Temporary Redirect`).

---

## Запуск бенчмарков

```bash
dotnet run -c Release --project src/Demo.Benchmarks
```
При анализе результатов оценивайте соотношение скорости сценариев и отсутствие аллокаций, а не конкретные наносекунды.
