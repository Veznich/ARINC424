# Визуальный стиль — псевдо-3D + звёздный фон

**Статус:** принят (MVP, Этап 3+)  
**Код:** `Scripts/Gameplay/Visual/`, `Utils/RuntimeMaterialUtil.cs`

## Цель

Геймплей остаётся на плоскости XY (ортокамера), но **блоки / мяч / платформа** читаются как объёмные объекты. Фон — спокойное **звёздное небо**, без ярких вспышек.

## Почему мяч был «3D», а кубы — нет

Сфера даёт разные нормали по поверхности → Lit сразу объёмный.  
У куба при взгляде строго вдоль −Z видна только фронт-грань с одной нормалью → выглядит плоским.

## Контракт

| Элемент | Решение |
|---------|---------|
| Камера | Orthographic + **pitch ~6°** (не 18° — иначе визуал ≠ коллизии), кадр через `PlayfieldLayout` |
| Потолок | `MaxY` = верх блоков; выше — запас под HUD (`HudWorldMargin`), мяч туда не заходит |
| Платформа | Y ≈ `-7.85` (почти у нижнего края кадра) |
| Коллизии | 6 субстепов/FixedUpdate + AABB min-penetration; мяч всегда `z=0` |
| Свет | Directional ~(48°, −42°), soft shadows |
| Материалы | URP Lit + **bevel albedo** (кубы) / sphere-shade (мяч) + soft emission |
| Блоки | Z-depth ≈ `0.85 × XY` + bevel map |
| Платформа | Z-depth ≥ `0.9`, bevel map, cyan |
| Мяч | Сфера + sphere-shade map |
| Фон | `StarfieldBackground`, `starBrightness ≈ 0.16` |

## HUD (статус-бар)

| Зона | Содержимое |
|------|------------|
| Слева | Жизни (`Lives n/max`) |
| Центр | Текущий уровень (`Level N`) — `LevelStartedEvent` |
| Справа | Кнопка паузы (`II` / `▶`) → `RequestPause` / `RequestResume` |
| Оверлей | При Pause: затемнение + «ПАУЗА»; при Game Over: 3D-кнопка `RESTART` (`GameOverRestartButton`) |

Код: `Scripts/UI/GameplayHudView.cs`, `Scripts/Gameplay/Lives/LivesService.cs`

## Принципы

1. Звёзды приглушённые — не отвлекают.
2. Псевдо-3D = pitch камеры + глубина Z + bevel map (не perspective gameplay-камера).
3. `GameplayVisualBootstrap.Apply` в Play — сцена без recreate подхватывает стиль.
4. Toon+Rim+Bloom — Этап 11.

## Не делать

- Яркие мигающие звёзды
- Плоский Unlit на paddle/blocks
- Perspective-камера без отдельного решения по вводу/clamp
