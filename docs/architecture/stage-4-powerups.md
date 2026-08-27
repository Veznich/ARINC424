# Этап 4 — 8 PowerUps + Drop + UI таймеров

**Статус:** реализован (код)  
**Зависит от:** [Этап 3](stage-3-seed-levels-blocks.md)

## Scope

| Компонент | Путь |
|-----------|------|
| Типы / конфиг | `PowerUps/PowerUpType.cs`, `Configs/PowerUpConfig` |
| Drop | `PowerUps/PowerUpDrop.cs` + pool |
| Сервис | `PowerUps/PowerUpService.cs` |
| UI таймеров | Ряд **3D-значков** над платформой; падающие дропы — те же 3D-иконки (не кубы) |
| События | `BlockDestroyed` → drop; `PowerUp*` events |

## Список бонусов

| Бонус | Эффект | Длительность |
|-------|--------|--------------|
| Fireball | Мяч пробивает блоки (без отскока), лимит pierce | 5 с |
| Wide Paddle | Платформа ×1.5 | 6 с |
| Slow Time | Замедление мяча/дропов | 4 с |
| Multi Ball | +1 мяч (макс 3) | до потери |
| Laser | Луч с платформы вверх каждые 0.5 с | 5 с |
| Shield | 1 раз спасает от потери мяча | до срабатывания |
| Magnet | Мяч прилипает к платформе; tap — запуск | 10 с |
| +1 Life | +1 жизнь (макс из PlayerConfig) | мгновенно |

## Правила

- Шанс дропа: `PowerUpConfig.dropChance` (20%)
- +1 Life: доля `lifeBonusShareOfDrops` среди дропов
- Повтор того же timed-бонуса → refresh таймера
- Multi Ball: не выше `maxBalls`
- Падение: `fallSpeed`, жизнь на поле `lifetimeSeconds`
- UI: ряд иконок снизу слева + fill-таймер

## Приёмка

- [ ] Разрушение блока иногда роняет бонус
- [ ] Подбор платформой активирует эффект
- [ ] Таймеры видны и убывают
- [ ] Shield / Life / Wide / Fireball ощущаются в игре

## Следующий

**Этап 5:** Difficulty Director.
