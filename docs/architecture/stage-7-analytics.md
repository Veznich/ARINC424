# Этап 7 — Analytics local JSON

**Статус:** принят  
**Зависит от:** [Этап 6](stage-6-replay.md)

## Scope

| Компонент | Путь |
|-----------|------|
| DTO | `Analytics/AnalyticsData.cs` |
| Store | `Analytics/AnalyticsStore.cs` → `analytics.json` |
| Сервис | `Analytics/AnalyticsService.cs` |
| Flush | Quit / Pause / Focus lost (+ периодически) |

## События (локально)

| Event | Когда |
|-------|--------|
| `session_start` | старт приложения |
| `level_start` | LevelStarted |
| `level_complete` | LevelCompleted |
| `ball_lost` | BallLost |
| `powerup_collected` | PowerUpCollected |
| `game_over` | RequestGameOver |
| `replay_saved` / `replay_playback` | Replay* |
| `session_end` | Quit |

Агрегаты в файле: уровни, смерти, блоки (через complete), бонусы, сессии.

## Контракт

- Файл: `persistentDataPath/analytics.json` (`GameDefaults.ANALYTICS_FILE_NAME`)
- Буфер событий + counters; flush без Firebase
- Лимит ленты: последние N событий (ротация)

## Приёмка

- [x] После Play в логе путь к `analytics.json`
- [x] После уровня / смерти / бонуса события появляются в JSON
- [x] Сворачивание приложения пишет файл на диск

## Следующий

**Этап 8:** [Полная настройка всех SO defaults](stage-8-so-defaults.md).
