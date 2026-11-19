#nullable enable
using Mvk.Core.Util;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.Particle;
using Mvk.Engine.Item;
using Mvk.Engine.World.Block;
using Mvk.Engine.World.BlockEntity;

namespace Mvk.Engine
{
    /// <summary>
    /// Реестры мира: блоки, предметы, сущности, блоки сущностей и частицы под таблицу id мира.
    /// Регистрация (чтение json, объекты блоков, атлас) делается один раз на процесс, корректировка под таблицу
    /// мира повторяемая: второй мир в процессе получает свои реестры без повторной регистрации.
    /// Объекты блоков и предметов общие и хранят свой id, поэтому активны одни реестры на процесс
    /// (Ce.Blocks и прочие указывают на них): сервер и клиент локальной игры делят их, а два мира с разными
    /// таблицами id одновременно в одном процессе не поддерживаются.
    /// </summary>
    public sealed class Registries
    {
        /// <summary>
        /// Активные реестры процесса, на них указывают Ce.Blocks, Ce.Items и прочие
        /// </summary>
        public static Registries? Active { get; private set; }

        public readonly BlockArrays Blocks;
        public readonly ItemArrays Items;
        public readonly EntityArrays Entities;
        public readonly BlockEntityArrays BlocksEntity;
        public readonly EntityFXArrays EntitiesFX;

        private Registries()
        {
            Blocks = Ce.Blocks;
            Items = Ce.Items;
            Entities = Ce.Entities;
            BlocksEntity = Ce.BlocksEntity;
            EntitiesFX = Ce.EntitiesFX;
        }

        /// <summary>
        /// Скорректировать зарегистрированные объекты под таблицы id мира и сделать реестры активными
        /// </summary>
        public static Registries Correct(CorrectTable entitiesFX, CorrectTable blocks, CorrectTable items,
            CorrectTable entities, CorrectTable blocksEntity)
        {
            // Порядок как в оригинале (GameModServer.CorrectObjects)
            EntitiesFXReg.Correct(entitiesFX);
            BlocksReg.Correct(blocks);
            ItemsReg.Correct(items);
            EntitiesReg.Correct(entities);
            BlocksEntityReg.Correct(blocksEntity);
            return Active = new Registries();
        }
    }
}
