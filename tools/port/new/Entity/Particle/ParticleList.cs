#nullable enable
using System.Collections.Generic;
using Mvk.Core.Maths;
using Mvk.Engine.Entity.Render;
using Mvk.Engine.World;

namespace Mvk.Engine.Entity.Particle
{
    /// <summary>
    /// Частицы клиента: создание с ограничением количества, физика и жизнь в такте игры.
    /// В оригинале списки и физика жили в рендере частиц (ParticlesRenderer); теперь рендер только рисует
    /// частицы из этих списков (технический п. 3), порядок частиц и такт те же.
    /// </summary>
    public sealed class ParticleList
    {
        /// <summary>
        /// Наибольшее количество объёмных частиц (кубиков)
        /// </summary>
        public const int MaxAmount3d = 1000;
        /// <summary>
        /// Наибольшее количество плоских частиц
        /// </summary>
        public const int MaxAmount2d = 4000;

        private readonly List<EntityFX> _list3d = new List<EntityFX>(MaxAmount3d);
        private readonly List<EntityFX> _list2d = new List<EntityFX>(MaxAmount2d);

        /// <summary>
        /// Объёмные частицы в порядке создания
        /// </summary>
        public IReadOnlyList<EntityFX> List3d => _list3d;
        /// <summary>
        /// Плоские частицы в порядке создания
        /// </summary>
        public IReadOnlyList<EntityFX> List2d => _list2d;

        /// <summary>
        /// Создать частицу; при переполнении уходит самая старая
        /// </summary>
        public void Spawn(ushort particleId, WorldClient world, IEntityRenderFactory renderFactory,
            Vector3 pos, Vector3 motion, int parameter)
        {
            EntityFX entity = Ce.EntitiesFX.CreateEntityClient(particleId, world, renderFactory, parameter);
            entity.InitRun(pos, motion);
            if (entity.IsCube)
            {
                while (_list3d.Count >= MaxAmount3d) _list3d.RemoveAt(0);
                _list3d.Add(entity);
            }
            else
            {
                while (_list2d.Count >= MaxAmount2d) _list2d.RemoveAt(0);
                _list2d.Add(entity);
            }
        }

        /// <summary>
        /// Игровой такт: физика частиц и их жизнь
        /// </summary>
        public void Update(WorldClient world, float deltaTime)
        {
            _Update(_list3d, world, deltaTime);
            _Update(_list2d, world, deltaTime);
        }

        private static void _Update(List<EntityFX> list, WorldClient world, float deltaTime)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].IsDead)
                {
                    list.RemoveAt(i);
                }
                else
                {
                    list[i].UpdateClient(world, deltaTime);
                    list[i].Update();
                }
            }
        }

        /// <summary>
        /// Очистить частицы
        /// </summary>
        public void Clear()
        {
            _list3d.Clear();
            _list2d.Clear();
        }

        public override string ToString() => "Fx2d:" + _list2d.Count + " Fx3d:" + _list3d.Count;
    }
}
