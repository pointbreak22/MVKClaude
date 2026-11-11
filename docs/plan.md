# План переноса

Полная версия с обоснованиями: «MvkClaude — архитектура и план переноса» (Claude Doc).
Каждый этап считается готовым, только когда выполнены его критерии.

| Этап | Что делаем | Критерий готовности |
|---|---|---|
| 0. Каркас | Решение, Directory.Build.props, центральные пакеты, CI, CLAUDE.md, docs, тесты архитектуры. Прототип Silk.NET на Android (окно + GLES + касание + звук). Golden-хэши чанков со старого генератора. | `dotnet build` и `dotnet test` зелёные; прототип запускается на телефоне; `tests/Mvk.Game.Tests/Golden/` заполнен |
| 1. Core | `WinGL/Util` (Glm, Vector*, Mat4, Mth), `Vge/Util`, `Vge/NBT`, `Vge/Json` → `Mvk.Core` | Тесты математики и NBT/JSON (чтение старых сохранений) |
| 2. Engine | `Vge/World`, `Entity` (без Render), `Item`, `Network`, `Management`, `Command`, `Event`, серверная часть `Games`, `Realms` → `Mvk.Engine`; разрезать ссылки на `Renderer` (BlockBase, Entity) | `Mvk.Server` поднимает мир без графики |
| 3. Game | `Mvk2/World`, `Entity`, `Item`, `Command`, `Packets`, `Util` → `Mvk.Game` | Golden-тесты генерации проходят 1:1 |
| 4. Platform + RHI | Silk.NET окно/ввод (InputAction)/OpenAL; RHI (буферы, текстуры, пайплайны, списки команд); GL-бэкенд; шейдеры из `res/Assets/Mvk2Shaders` | Тестовая сцена с текстурированными кубами |
| 5. Client | `Vge/Renderer`, `Vge/Gui`, `Vge/Audio`, клиент `Games` → `Mvk.Engine.Client`; `Mvk2/Renderer`, `Mvk2/Gui` → `Mvk.Game.Client`; ресурсы в `assets/` | Игра запускается на ПК, картинка совпадает с оригиналом |
| 6. Паритет и баги | Сравнение кадров, прогон механики, исправления из `known-issues.md` | Чек-лист паритета пройден |
| 7. Оптимизация | Планировщик задач (генерация, свет, мешинг), пулы буферов, профилирование | Замеры до/после в `progress.md` |
| 8. Две руки | `UseLeftHand`/`UseRightHand`, анимации, раскладки; за флагом | Режим включается в настройках |
| 9. Мобильная версия | Хост Android, GLES-шейдеры, сенсорный HUD, масштаб UI | APK запускается и играбелен |
| 10. Vulkan | Второй бэкенд RHI, многопоточная запись команд, MoltenVK | Та же картинка на Vulkan |
