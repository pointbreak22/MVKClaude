using System.IO;
using Mvk.Engine.Audio;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.Particle;
using Mvk.Engine.Item;
using Mvk.Engine.Util;
using Mvk.Engine.World.Block;
using Mvk.Engine.World.BlockEntity;
using Mvk.Game.Entity;
using Mvk.Game.Entity.Particle;
using Mvk.Game.Item;
using Mvk.Game.Util;
using Mvk.Game.World.Block;
using Mvk.Game.World.BlockEntity;

namespace Mvk.Game.Client
{
    /// <summary>
    /// Запуск клиента Малювек 2: пути, настройки и реестры, как WindowMvk оригинала до создания игры
    /// </summary>
    public static class MvkClient
    {
        /// <summary>Шейдер блоков без теней</summary>
        public const string ShaderBlocksLow = "BlocksLow";

        /// <summary>
        /// Задать папку Assets, прочитать настройки и заполнить реестры блоков, предметов и сущностей.
        /// После вызова атлас блоков готов к загрузке на GPU.
        /// </summary>
        /// <param name="assets">Путь к папке Assets</param>
        /// <param name="loadSounds">Загружать ли звуки (без звуковой карты false)</param>
        public static void Initialize(string assets, bool loadSounds)
        {
            Options.PathAssets = Path.GetFullPath(assets).TrimEnd(Path.DirectorySeparatorChar, '/')
                + Path.DirectorySeparatorChar;
            new OptionsFileMvk().Load();
            AudioReg.Initialization(loadSounds);
            Ce.SetSpriteAtlasSize(64, 16);
            BlocksReg.Initialization();
            BlocksRegMvk.Initialization();
            ItemsReg.SizeShape = 32;
            ItemsReg.Initialization();
            ItemsRegMvk.Initialization();
            EntitiesReg.Initialization();
            EntitiesRegMvk.Initialization();
            BlocksEntityReg.Initialization();
            BlocksEntityRegMvk.Initialization();
            EntitiesFXReg.Initialization();
            EntitiesFXRegMvk.Initialization();
            EntitiesReg.TextureManagerRun();
        }
    }
}
