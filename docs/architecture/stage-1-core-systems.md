# Этап 1 — Core Systems

**Статус:** реализован (код)  
**Зависит от:** [Этап 0](stage-0-3d-roguelike-arkanoid.md)

## Что сделано

| Компонент | Путь |
|-----------|------|
| Event Bus | `Scripts/Core/EventBus/` |
| State Machine | `Scripts/Core/StateMachine/` |
| Save JSON | `Scripts/Save/` |
| VContainer DI | `Scripts/Core/DI/ProjectLifetimeScope.cs` |
| Bootstrap | `Scripts/Core/DI/GameBootstrap.cs` |
| Seed util + debug | `Scripts/Utils/` |
| SO-конфиги (скелеты) | `Scripts/Configs/` |
| Editor: Create All Configs | `Scripts/Editor/ConfigAssetsMenu.cs` |
| Packages | `Packages/manifest.json` (Input System 1.20, VContainer EntityId-fix; Addressables — с Этапа 12) |

## Быстрый старт в Unity

1. Открыть папку `G:\ARINC424` как Unity-проект (**6000.5.5f1**). Не открывать `ProjectSettings`.
2. Дождаться резолва пакетов (VContainer по Git URL; URP подтянется под Editor).
3. Меню **Arkanoid → Configs → Create All Default Configs**.
4. Создать сцену `Assets/_Project/Scenes/Bootstrap.unity`.
5. Empty GameObject `ProjectContext`:
   - `ProjectLifetimeScope` → назначить `GameConfigCatalog`
   - `SaveLifecycleBehaviour` добавится автоматически в Awake
6. (Опционально) `CoreSmokeTest` на тот же объект.
7. Play → в Console: «Core готов», файл `save.json` в `persistentDataPath`.

## Контракты

- `Application.targetFrameRate = 60`
- Состояния: Menu / Gameplay / Pause / GameOver
- Автосейв: Quit, Pause, потеря фокуса + авто-пауза в Gameplay
- Seed: `(level * 1337 + 42) % 1000000`, override через `SeedDebugCommands`

## Приёмка Этапа 1

- [x] IEventBus / EventBus
- [x] GameStateMachine + события запросов
- [x] SaveData + SaveService (Newtonsoft JSON)
- [x] ProjectLifetimeScope (VContainer)
- [x] Скелеты Ball/Paddle/Level/PowerUp/Difficulty/Combo/Player + Catalog
- [x] Editor-меню создания ассетов
- [ ] Пользователь проверил Play Mode в Unity **6000.5.5f1**

## Следующий

**Этап 2:** ✅ см. [stage-2-input-paddle-ball.md](stage-2-input-paddle-ball.md)  
**Этап 3:** Seed levels · 3 архетипа · 5 типов блоков.
