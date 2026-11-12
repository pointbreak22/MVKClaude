using Mvk.Game.World.Gen;
using System;
using System.IO;
using Mvk.Core.Nbt;
using Mvk.Engine.Nbt;
using Mvk.Engine.World;
using Mvk.Engine.World.Environment;

namespace Mvk.Game.World
{
    /// <summary>
    /// Опции кошмарного мира
    /// </summary>
    public class WorldSettingsNightmare : WorldSettings
    {
        /// <summary>
        /// Для клиента
        /// </summary>
        public WorldSettingsNightmare() : base()
        {
#if DEBUG
            Console.WriteLine("_construct WorldSettingsNightmare CLIENT");
#endif
        }

        /// <summary>
        /// Для сервера
        /// </summary>
        public WorldSettingsNightmare(string pathWorld) : base(pathWorld)
        {
            ChunkGenerate = new ChunkProviderGenerateDebug(NumberChunkSections);

            if (File.Exists(_pathFileSetting))
            {
                TagCompound nbt = NBTTools.ReadFromFile(_pathFileSetting, true);
                Environment.ReadFromNBT(nbt);
            }
#if DEBUG
            Console.WriteLine("_construct WorldSettingsNightmare SERVER");
#endif
        }

        protected override void _Init()
        {
            IdSetting = 2;
            // HasNoSky = true;
            ActiveRadius = 3;
            NumberChunkSections = 16;
            Environment = new EnvironmentNone();
        }
    }
}
