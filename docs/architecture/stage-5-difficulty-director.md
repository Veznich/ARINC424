# Этап 5 — Difficulty Director

**Статус:** принят  
**Зависит от:** [Этап 4](stage-4-powerups.md)

## Scope

| Компонент | Путь |
|-----------|------|
| Конфиг | `Configs/DifficultyConfig` |
| Сервис | `Difficulty/DifficultyDirector` |
| Событие | `DifficultyChangedEvent` |
| Потребители | Ball speed · PowerUp dropChance · BlockField extra HP · HUD toast |

## Логика

Метрики **за уровень**:
- `livesLost` (счётчик `BallLost`)
- серия чистых прохождений без смертей (`clearStreak`)

| Условие (по завершению уровня) | Bias | Эффект |
|--------------------------------|------|--------|
| `livesLost >= strugglingLivesLostPerLevel` | Assist | ↑ drop chance, ↓ ball speed, −1 extra HP |
| `livesLost == 0` и `clearStreak >= easyLevelsWithoutDeath` | Challenge | ↓ drop chance, ↑ ball speed, +extraBlockHp |
| иначе | Neutral drift | лёгкий откат модификаторов к базе |

Модификаторы **плавно** (`lerpSpeed`) и **клампятся** (min/max drop, speed mul, max extra HP).

## Контракт значений (defaults)

| Параметр | Default |
|----------|---------|
| strugglingLivesLostPerLevel | 2 |
| easyLevelsWithoutDeath | 3 |
| dropChanceBonus / maxDropChance | +0.10 / 0.35 |
| dropChancePenalty / minDropChance | −0.05 / 0.10 |
| ballSpeedPenalty / Bonus | −10% / +10% |
| extraBlockHp / maxExtraBlockHp | +1 / 2 |
| showNotifications | true |

## Приёмка

- [ ] После 2+ смертей на уровне следующий легче (больше дропов / медленнее мяч)
- [ ] 3 чистых уровня подряд — чуть жёстче (HP / скорость)
- [ ] Toast «ASSIST» / «CHALLENGE» при смене bias (если включено)
- [ ] Рестарт сессии сбрасывает модификаторы

## Следующий

**Этап 6:** Replay record / play / export.
