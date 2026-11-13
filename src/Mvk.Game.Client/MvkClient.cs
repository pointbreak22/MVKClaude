using System;
using System.IO;
using Mvk.Engine.Games;
using Mvk.Engine.Renderer.World;
using Mvk.Engine.Entity.Player;
using Mvk.Game.Client.Entity.Render;
using Mvk.Engine.Renderer;
using Mvk.Game.Client.Gui;
using Mvk.Game.Client.Renderer;
using Mvk.Game.World;
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
        /// <summary>Шейдер сущностей без теней</summary>
        public const string ShaderEntityLow = "EntityLow";

        /// <summary>
        /// Задать папку Assets (в оригинале её задавал запуск окна). Опции читает WindowMvk,
        /// реестры заполняются при каждом запуске игры, как в оригинале.
        /// </summary>
        /// <param name="assets">Путь к папке Assets</param>
        public static void SetAssets(string assets)
            => Options.PathAssets = Path.GetFullPath(assets).TrimEnd(Path.DirectorySeparatorChar, '/')
                + Path.DirectorySeparatorChar;

        /// <summary>
        /// Подключить рендер мира Малювек к хосту клиента: небо острова и рендеры сущностей мода
        /// </summary>
        public static void SetupHost(GameClientHost host, SkyTexturesMvk skyTextures)
        {
            host.SkyFactory = SkyFactory(skyTextures);
            // HUD Малювек (GameModClientMvk.CreateHud оригинала)
            host.HudFactory = game => new HudMvk(game, (RenderMvk)host.Render);
            host.RegisterEntityRender(EntityRenderKindMvk.Owner, (entity, entities, resources)
                => new EntityRenderOwner((PlayerClientOwner)entity, entities, resources));
        }

        /// <summary>
        /// Фабрика неба мира для GameClientHost.SkyFactory, как GameModClientMvk.CreateWorldSettings
        /// оригинала: мир кошмаров (id 2) с базовым небом, остальные с небом острова
        /// </summary>
        public static Func<GameBase, WorldRenderer, SkyRender> SkyFactory(SkyTexturesMvk textures)
            => (game, render) => game.World.Settings is WorldSettingsNightmare
                ? new SkyRender(game.Player, render)
                : new SkyIslandRender(game.Player, render, textures);
    }
}
