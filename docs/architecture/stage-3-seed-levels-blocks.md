# Этап 3 — Seed levels · 3 архетипа · 5 блоков

**Статус:** принят (Play Mode)  
**Зависит от:** [Этап 2](stage-2-input-paddle-ball.md)

## Scope

| Компонент | Путь |
|-----------|------|
| ObjectPool | `Scripts/Pool/ObjectPool.cs` |
| Типы / архетипы | `Scripts/Gameplay/Block/BlockType.cs`, `Level/LevelArchetype.cs` |
| Генератор | `Scripts/Gameplay/Level/LevelGenerator.cs` |
| LevelService | `Scripts/Gameplay/Level/LevelService.cs` |
| Поле блоков | `Scripts/Gameplay/Block/BlockField.cs`, `BlockView.cs` |
| Конфиг | `Configs/LevelConfig` (+ frozen) |

## Архетипы (seed % 3)

| Archetype | Паттерн |
|-----------|---------|
| **Tunnel** | Коридор по центру, плотные стены слева/справа |
| **Fortress** | Плотная стена + «башни» по краям |
| **Diamond** | Ромб / алмаз по манхэттенскому расстоянию |

## Типы блоков

Цвет = текущий лвл HP (при ударе понижается):

| Лвл / цвет | HP | При ударе |
|------------|----|-----------|
| **Red** | 3 | → Yellow |
| **Yellow** | 2 | → Green |
| **Green** | 1 | → уничтожен |

Спец-блоки (Explosive / Frozen / Generator) — отложены.

## Seed

`Seed = (level * 1337 + 42) % 1_000_000` · override: `SeedDebugCommands.SetSeed(n)`

## Приёмка

- [x] Play → видны блоки архетипа
- [x] Мяч ломает блоки, отскакивает
- [x] Red/Yellow/Green лвлы HP
- [x] Один seed → одинаковая раскладка
- [x] Очистка поля → `LevelCompleted` → следующий уровень
- [x] HUD: жизни, уровень, пауза, Game Over → Restart

## Следующий

**Этап 4:** [stage-4-powerups.md](stage-4-powerups.md)
