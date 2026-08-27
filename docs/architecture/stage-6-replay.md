# Этап 6 — Replay record / play / export

**Статус:** принят  
**Зависит от:** [Этап 5](stage-5-difficulty-director.md)

## Scope

| Компонент | Путь |
|-----------|------|
| DTO | `Replay/ReplayData.cs` |
| Store | `Replay/ReplayStore.cs` (JSON, макс 10) |
| Input router | `Replay/GameplayInputRouter.cs` |
| Сервис | `Replay/ReplayService.cs` |
| UI | INFO → Replay / Export |

## Контракт

- Запись: каждый кадр `GameplayInputFrame` + `time` от старта уровня, вместе с `level` + `seed`
- Автосейв при `LevelCompleted` / Game Over (если есть кадры)
- Playback: `SeedGenerator` override → тот же уровень → ввод с ленты
- Export: копия последнего replay в `persistentDataPath/replays/export_*.json`
- Лимит: `GameDefaults.MAX_STORED_REPLAYS` (10)

## Приёмка

- [ ] После уровня файл появляется в `…/replays/`
- [ ] INFO → Replay повторяет прохождение (приблизительно)
- [ ] INFO → Export пишет `export_*.json` и путь в лог
- [ ] Во время playback живой ввод не мешает

## Следующий

**Этап 7:** Analytics local JSON.
