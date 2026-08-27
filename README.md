# 3D Roguelike Arkanoid

Мобильный MVP (Unity **6000.5.5f1** + URP).

## Документация

- [Этап 0 — Архитектура](docs/architecture/stage-0-3d-roguelike-arkanoid.md)
- [Этап 1 — Core Systems](docs/architecture/stage-1-core-systems.md)
- [Этап 2 — Input + Paddle + Ball](docs/architecture/stage-2-input-paddle-ball.md)

## Требования

- Unity **6000.5.5f1** (см. `ProjectSettings/ProjectVersion.txt`)
- Открывать корень: `G:\ARINC424` (не папку `ProjectSettings`)
- Пакеты под 6.5: Input System **1.20+**, VContainer с фиксом `GetEntityId` (Addressables подключаем на Этапе 12)

## Открыть проект

1. Закрыть текущий Editor, если открыт ошибочный проект
2. Hub → **6000.5.5f1** → Open → `G:\ARINC424`
3. **Arkanoid → Configs → Create All Default Configs**
4. При варнингах Input/Batching: **Arkanoid → Project → Apply Player Settings**
5. Сцена Bootstrap + `ProjectLifetimeScope` (см. Этап 1)
6. **Arkanoid → Gameplay → Create Stage2 Arena** → Parent LifetimeScope = ProjectContext

Студийный kit агентов: см. `AGENTS.md`.
