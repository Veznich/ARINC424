# Этап 8 — Полная настройка SO defaults

**Статус:** реализован (код)  
**Зависит от:** [Этап 7](stage-7-analytics.md)

## Scope

Зафиксировать **MVP-баланс** во всех ScriptableObject после плейтеста этапов 2–7.
Скелеты SO были на Этапе 1; здесь — канонические значения + инструменты сброса.

| Asset | Роль |
|-------|------|
| BallConfig | скорость, ускорение, отскок |
| PaddleConfig | размер, maxX, ввод, wide |
| LevelConfig | сетка + прогрессия блоков |
| PowerUpConfig | дроп, длительности, multi/laser |
| DifficultyConfig | Assist/Challenge + лёгкий рост |
| ComboConfig | тиры (для Этапа 10 UX) |
| PlayerConfig | жизни, meta-лимиты, монеты |
| GameConfigCatalog | ссылки на все |

## Канон MVP (плейтест)

| Параметр | Значение |
|----------|----------|
| Ball base / max | 10 / 20 |
| Paddle maxX | 5.2 (SidePad 1.15 в layout) |
| L1 blocks / +per level / cap | 5 / +3 / 72 |
| Tier unlock / max tier | каждые 10 ур. / 8 |
| Drop chance / Life share | 0.20 / 0.05 |
| Multi spawn / max balls | 2 / 5 |
| Magnet / Laser / Fireball | 10s / 5s / 5s |
| Lives start / max | 3 / 5 |
| Director levelExtraHp на блоки | **выкл.** (цвета в LevelGenerator) |

## Инструменты

- `Arkanoid → Configs → Create All Default Configs`
- `Arkanoid → Configs → Apply MVP Defaults`
- `Arkanoid → Configs → Validate Catalog`
- Context menu на каждом SO: **Reset to MVP Defaults**
- Runtime: `MvpConfigDefaults.Apply(*)`

## Приёмка

- [ ] Catalog валиден, все 7 ссылок назначены
- [ ] Apply MVP Defaults не ломает Play Mode
- [ ] Баланс совпадает с таблицей выше (Inspector)

## Следующий

**Этап 9:** Coins, shop, meta.
