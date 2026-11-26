"""[ПОВЕДЕНИЕ] Выбор персонажа (просьба пользователя 26.11): настройка PlayerSkin — свой игрок рисуется текстурой
UserSkin модели Humanoid (скин пользователя, tools/Assets/skin_convert.py) вместо текстуры из json сущности."""
from portlib import load, save, rep

rel = r"Util\Options.cs"
t = load(rel)
t = rep(t, """        public static bool BlockPlacementPreview = true;""", """        public static bool BlockPlacementPreview = true;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Свой игрок со скином пользователя (текстура UserSkin)
        /// </summary>
        public static bool PlayerSkin;""", rel, 1)
save(rel, t)

rel = r"Util\OptionsFile.cs"
t = load(rel)
t = rep(t, """                case "BlockPlacementPreview": Options.BlockPlacementPreview = value == "1"; return true;""",
        """                case "BlockPlacementPreview": Options.BlockPlacementPreview = value == "1"; return true;
                case "PlayerSkin": Options.PlayerSkin = value == "1"; return true;""", rel, 1)
save(rel, t)

rel = r"Entity\ResourcesEntity.cs"
t = load(rel)
t = rep(t, """        /// <summary>
        /// Получить параметр есть ли анимация для шейдора""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Параметр глубины другой текстуры той же формы по названию (например UserSkin), -1 если нет
        /// </summary>
        public int GetDepthTextureByName(string name)
        {
            int index = EntitiesReg.Shapes[_indexShape].FindIndexTexture(name);
            return index < 0 ? -1 : EntitiesReg.Shapes[_indexShape].DepthTextures[index];
        }

        /// <summary>
        /// Получить параметр есть ли анимация для шейдора""", rel, 1)
save(rel, t)
print("user_skin ok")
