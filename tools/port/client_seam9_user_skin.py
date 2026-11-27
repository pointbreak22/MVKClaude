"""[ПОВЕДЕНИЕ] Шов клиента: свой игрок со скином пользователя (Options.PlayerSkin, текстура UserSkin) и галочка
«Свой скин персонажа» в настройках."""
import os
from portlib import load, save, rep, ROOT
C = os.path.join(ROOT, r"src\Mvk.Engine.Client")

rel = r"Entity\Render\EntityRenderAnimation.cs"
t = load(rel, C)
assert t.count("_resourcesEntity.GetDepthTextureAndSmall(),") == 2
t = t.replace("_resourcesEntity.GetDepthTextureAndSmall(),", "_DepthTexture(),")
t = rep(t, """        private readonly TriggerAnimation _trigger = new TriggerAnimation();""", """        private readonly TriggerAnimation _trigger = new TriggerAnimation();

        /// <summary>
        /// [ПОВЕДЕНИЕ] Текстура сущности: игрок со скином пользователя — свой по Options.PlayerSkin, другие по флагу
        /// метаданных IsUserSkin (передаётся сервером), иначе из json
        /// </summary>
        private int _DepthTexture()
        {
            bool own = Entity.Id == _entities.Player.Id;
            if (own ? Options.PlayerSkin : Entity is EntityLiving living && living.IsUserSkin())
            {
                int depth = _resourcesEntity.GetDepthTextureByName("UserSkin");
                if (depth >= 0) return depth;
            }
            return _resourcesEntity.GetDepthTextureAndSmall();
        }""", rel, 1)
if "using Mvk.Engine.Util;" not in t:
    t = t.replace("namespace Mvk.Engine.Entity.Render", "using Mvk.Engine.Util;\n\nnamespace Mvk.Engine.Entity.Render", 1)
save(rel, t, C)

rel = r"Gui\Screens\Window\ScreenOptions.cs"
t = load(rel, C)
t = rep(t, """        protected readonly CheckBox _checkBoxBlockPlacementPreview;""", """        protected readonly CheckBox _checkBoxBlockPlacementPreview;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Свой скин персонажа
        /// </summary>
        protected readonly CheckBox _checkBoxPlayerSkin;""", rel, 1)
t = rep(t, """            _checkBoxBlockPlacementPreview.SetChecked(Options.BlockPlacementPreview);
""", """            _checkBoxBlockPlacementPreview.SetChecked(Options.BlockPlacementPreview);
            _checkBoxPlayerSkin = new CheckBox(window, font, 200, L.T("PlayerSkin"));
            _checkBoxPlayerSkin.SetChecked(Options.PlayerSkin);
""", rel, 1)
t = rep(t, """            Options.BlockPlacementPreview = _checkBoxBlockPlacementPreview.Checked;""", """            Options.BlockPlacementPreview = _checkBoxBlockPlacementPreview.Checked;
            Options.PlayerSkin = _checkBoxPlayerSkin.Checked;
            if (window.Game != null && window.Game.Player != null) window.Game.Player.SendPlayerSetting();""", rel, 1)
t = rep(t, """            _AddControls(_checkBoxBlockPlacementPreview);""", """            _AddControls(_checkBoxBlockPlacementPreview);
            _AddControls(_checkBoxPlayerSkin);""", rel, 1)
t = rep(t, """            _checkBoxBlockPlacementPreview.SetPosition(PosX + 274, PosY + 62);""", """            _checkBoxBlockPlacementPreview.SetPosition(PosX + 274, PosY + 62);
            _checkBoxPlayerSkin.SetPosition(PosX + 274, PosY + 92);""", rel, 1)
save(rel, t, C)
print("client user skin ok")
