# Журнал работ

Новые записи сверху. Изменения поведения относительно оригинала помечаются `[ПОВЕДЕНИЕ]`.

## 2026-11-12: этап 0, каркас
Сделано:
- Решение `MvkClaude.slnx` на .NET 10.0.302 (`global.json`), общие настройки в `Directory.Build.props`,
  версии пакетов централизованно в `Directory.Packages.props` (Silk.NET 2.23.0).
- Проекты: Core, Engine, Engine.Client, Rendering, Rendering.OpenGL, Platform, Game, Game.Client,
  хосты Desktop и Server, тесты Core/Engine/Game.
- Заготовка RHI: `IGraphicsDevice`, `GraphicsBackend`; `OpenGLDevice` на Silk.NET.OpenGL.
- `Mvk.Desktop` открывает окно OpenGL 3.3 и очищает экран белым (как загрузчик оригинала).
- `ArchitectureTests`: Core, Engine, Game и Server не ссылаются на графику, платформу и клиент.
- Оригинал склонирован в `original/` (коммит `dfbd814`), исключён из git.
- `dotnet build`: 0 предупреждений, 0 ошибок. `dotnet test`: 5 из 5.

Дальше:
- Прототип Silk.NET на Android.
- Снять golden-хэши чанков со старого генератора.
- Этап 1: перенос `WinGL/Util` и `Vge/Util`, `NBT`, `Json` в `Mvk.Core`.
