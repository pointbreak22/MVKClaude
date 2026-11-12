"""Шов Mvk2 (ADR 0003): клиентский мод Малювек без окна. В Mvk.Game остаётся логика
(цвета биомов, настройки мира, коррекция таблиц, F5/F7), окно, HUD, экраны и небо
переопределит наследник в Mvk.Game.Client (этап 5)."""
import os
from portlib import load, save, rep, sub, remove_member, remove_field, add_using, fail, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")
def L(rel): return load(rel, GAME)
def S(rel, t): save(rel, t, GAME)

rel = r"Games\GameModClientMvk.cs"; t = L(rel)
t = rep(t, "using Mvk.Game.Gui;\n", "", rel)
t = rep(t, "using Mvk.Game.Renderer.World;\n", "", rel)
t = remove_field(t, "private readonly WindowMvk _windowMvk;", rel)
t = rep(t, "public GameModClientMvk(WindowMvk window) : base(window)\n        {\n            _windowMvk = window;\n",
        "public GameModClientMvk()\n        {\n", rel)
# Небо создаёт клиентский наследник после base.CreateWorldSettings
t = sub(t, r"\n[ \t]*Game\.WorldRender\.WorldStarted\(new SkyRender\(Game\.Player, Game\.WorldRender\)\);", "", rel)
t = sub(t, r"\n[ \t]*Game\.WorldRender\.WorldStarted\(new SkyIslandRender\(\n[ \t]*Game\.Player, Game\.WorldRender, _windowMvk\.GetRender\(\)\)\);", "", rel)
t = remove_member(t, "public override void WorldStoped()", rel)
t = remove_member(t, "public override HudBase CreateHud()", rel)
# Экраны (чат, инвентарь, хранилище, креатив) и снимок биомов открывает клиентский наследник
t = sub(t, r"\n[ \t]*// Окно чата[^\n]*\n[ \t]*case Keys\.T: case Keys\.Oemtilde: _windowMvk\.LScreenMvk\.Chat\(\); break;", "", rel)
t = sub(t, r"\n[ \t]*// скрин инвентаря\n[ \t]*case Keys\.E: _windowMvk\.LScreenMvk\.Inventory\(\); break;", "", rel)
t = sub(t, r"\n[ \t]*// скрин хранилища\n[ \t]*case Keys\.R: _windowMvk\.LScreenMvk\.StorageDebug\(\); break;", "", rel)
t = sub(t, r"\n[ \t]*// скрин креативного инвентаря\n[ \t]*case Keys\.C: \n[ \t]*if \(Player\.CreativeMode\) _windowMvk\.LScreenMvk\.CreativeInventory\(\); break;", "", rel)
t = sub(t, r"\n[ \t]*// Скрин\n[ \t]*case Keys\.F6: DebugMvk\.ScreenFileBiomeArea\(Game\); break;", "", rel)
if "_windowMvk" in t or "WorldRender" in t: fail(rel + ": window still used")
S(rel, t)

# Рендер игрока-владельца создаёт фабрика клиента по виду "Owner"
rel = r"Entity\List\PlayerClientOwnerMvk.cs"; t = L(rel)
t = rep(t, "using Mvk.Game.Entity.Render;\n", "using Mvk.Engine.Entity.Render;\n", rel)
t = rep(t, "public override void InitRender(ushort index, EntitiesRenderer entitiesRenderer)",
        "public override void InitRender(ushort index, IEntityRenderFactory renderFactory)", rel)
t = rep(t, "Render = new EntityRenderOwner(this, entitiesRenderer, resourcesEntity);",
        "Render = renderFactory.Create(EntityRenderKindMvk.Owner, this, resourcesEntity);", rel)
S(rel, t)
print("game_seam1 ok")
