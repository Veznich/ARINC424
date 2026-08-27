# Этап 2 — Input + Paddle + Ball

**Статус:** реализован (код)  
**Зависит от:** [Этап 1](stage-1-core-systems.md)

## Scope

| Компонент | Путь |
|-----------|------|
| Input (New Input System) | `Scripts/Input/GameplayInputReader.cs` |
| Paddle | `Scripts/Gameplay/Paddle/PaddleController.cs` |
| Ball + bounce math | `Scripts/Gameplay/Ball/` |
| Playfield bounds | `Scripts/Gameplay/Arena/PlayfieldBounds.cs` |
| Gameplay DI scope | `Scripts/Gameplay/Arena/GameplayLifetimeScope.cs` |
| События | `Scripts/Core/EventBus/GameplayEvents.cs` |
| Editor: Create Arena | `Arkanoid → Gameplay → Create Stage2 Arena In Active Scene` |

## Управление

- **Клавиатура:** A/D или ←/→ — движение; Space / W / ↑ — запуск мяча
- **Touch / Mouse drag** в нижней ~1/3 экрана (`PaddleConfig.controlZoneScreenFraction`)
- **One-hand:** удержание пальца — платформа едет к X касания
- **Launch:** tap или swipe up (пока мяч docked)

## Физика

- Кинематический `Rigidbody`, интеграция в `FixedUpdate`
- Отскок от платформы: угол по hit-factor + импульс скорости платформы (`BallConfig`)
- Стены: reflect + `wallBounceAngle`
- Низ поля: `BallLostEvent` → повторный dock на платформу
- Ускорение: каждые `speedIncrementInterval` × `(1 + speedIncrement)`, потолок `maxSpeed`

## Сборка сцены

1. Открыть `G:\ARINC424` в Unity **6000.5.5f1**
2. **Arkanoid → Project → Setup URP Pipeline (fix pink screen)**  
   (или сразу шаг 3 — он вызывает Setup URP сам)
3. **Arkanoid → Gameplay → Create Full Bootstrap Scene (Playable)**  
   Создаст `Assets/_Project/Scenes/Bootstrap.unity` с камерой, ProjectContext, платформой и мячом
4. **Arkanoid → Project → Setup Mobile Debug View (Portrait 1080x1920)**
5. Play → тёмный фон, cyan-платформа, pink-мяч; в Console: `AutoStart → Gameplay`; Space / tap — запуск

**Фиолетовый/розовый экран** = URP-материалы без назначенного Pipeline в Graphics Settings. Шаг 2/3 чинит это. После смены пайплайна **пересоздай** Bootstrap-сцену (материалы создаются заново).

Если экран пустой — сцена без арены. Снова вызови пункт 3.

## Приёмка Этапа 2

- [x] New Input System (keyboard + pointer)
- [x] Paddle движение + clamp `maxX`
- [x] Ball dock / launch / walls / paddle bounce / lost→redock
- [x] Пауза FSM: управление только в `Gameplay`
- [x] EventBus: Docked / Launched / Lost / HitPaddle / HitWall
- [x] Пользователь проверил Play Mode

## Следующий

**Этап 3:** [stage-3-seed-levels-blocks.md](stage-3-seed-levels-blocks.md) → далее Этап 4 (PowerUps).
