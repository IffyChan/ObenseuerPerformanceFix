# Obenseuer Performance Fix

BepInEx 6 mod for **Obenseuer** fixing the 30 FPS borderless window lock and reducing draw calls on outdoor scenes without sacrificing visual quality.

[English](#english) | [Русский](#русский)

---

## English

### What it does
- **Fixes 30 FPS Borderless Lock**: Disables conflicting engine software sleep (`targetFrameRate = -1`) when VSync is ON, stopping Windows iFlip / DWM from halving refresh rates down to 30 FPS.
- **Micro-Prop Shadow Cull**: Disables shadow passes on tiny clutter (<0.08 m³: cigarette butts, cans, shards), cutting up to ~7,500 shadow draw calls on outdoor streets. Player, NPCs, buildings, and lights keep full shadows.
- **Reflection Probe Slicing**: Forces `timeSlicingMode = IndividualFaces` and zero shadow distance on reflection probes, eliminating periodic 36ms hitches outdoors.
- **Automatic GPU Instancing**: Enables GPU instancing on compatible scene materials (fences, crates, windows) to batch draw calls.
- **Shadow Cascade & LOD Tuning**: Clamps shadow cascades to 2 and distance to 50m, avoiding GPU bottlenecks on Ultra settings.

### Installation
1. Install [BepInEx 6 Bleeding Edge (Unity Mono)](https://builds.bepinex.dev/projects/bepinex_be).
2. Place `ObenseuerPerformanceFix.dll` into `Obenseuer/BepInEx/plugins/ObenseuerPerformanceFix/`.
3. Launch the game.

### Configuration
Config is generated at `BepInEx/config/obenseuer.performancefix.cfg`:
- `[Mod] Enabled` (default `true`): Master switch.
- `[Display] TargetFpsWithVsyncOff` (default `-1`): Frame limit when VSync is OFF (`-1` = Unlocked).
- `[Shadows] MinShadowMeshVolume` (default `0.08`): Minimum object volume in m³ to cast shadows.
- `[Shadows] OptimizeCascades` (default `true`): Caps cascades at 2.

---

## Русский

### Что делает мод
- **Устраняет лок 30 FPS в Borderless**: Отключает конфликтующий софтверный таймер движка (`targetFrameRate = -1`) при включенном VSync. Оконный композитор Windows DWM больше не сбрасывает частоту вдвое.
- **Умный куллинг теней мусора**: Отключает расчет теней для микро-объектов (<0.08 м³: окурки, банки, осколки), срезая до ~7 500 вызовов отрисовки на улице. Тени от зданий, персонажей, фонарей и мебели остаются полноценными.
- **Time-Slicing отражений**: Переводит зонды отражений в покадровый рендеринг (`IndividualFaces`) и убирает тени внутри зондов, устраняя микрофризы на 36 мс каждые 20 секунд.
- **GPU Instancing**: Включает инстансинг на типовых материалах (заборы, ящики, окна), объединяя одинаковые меши в пакетные вызовы.
- **Оптимизация каскадов и LOD**: Ограничивает каскады теней до 2 и дальность до 50 м на высоких пресетах.

### Установка
1. Установите [BepInEx 6 Bleeding Edge (Unity Mono)](https://builds.bepinex.dev/projects/bepinex_be).
2. Скопируйте `ObenseuerPerformanceFix.dll` в `Obenseuer/BepInEx/plugins/ObenseuerPerformanceFix/`.
3. Запустите игру.

### Настройки
Файл настроек: `BepInEx/config/obenseuer.performancefix.cfg`:
- `[Mod] Enabled` (по умолчанию `true`): Главный выключатель мода.
- `[Display] TargetFpsWithVsyncOff` (по умолчанию `-1`): Лимит кадров при выключенном VSync (`-1` = без ограничений).
- `[Shadows] MinShadowMeshVolume` (по умолчанию `0.08`): Минимальный объем объекта в м³ для отбрасывания теней.
- `[Shadows] OptimizeCascades` (по умолчанию `true`): Ограничение до 2 каскадов.

---

### Build from source
```bash
dotnet build -c Release
```
