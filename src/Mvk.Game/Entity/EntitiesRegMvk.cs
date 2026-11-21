using Mvk.Game.Entity.List;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.List;

namespace Mvk.Game.Entity
{
    /// <summary>
    /// Регистрация моделей сущностей для Малювеки 2
    /// </summary>
    public sealed class EntitiesRegMvk
    {
        /// <summary>
        /// Индекс курочки
        /// </summary>
        public static ushort ChickenId { get; private set; }
        /// <summary>
        /// [ПОВЕДЕНИЕ] Индексы враждебных мобов
        /// </summary>
        public static ushort ZombieId { get; private set; }
        public static ushort BanditId { get; private set; }

        /// <summary>
        /// Инициализация Id
        /// </summary>
        public static void InitId()
        {
            string alias;
            for (ushort i = 0; i < Ce.Entities.Count; i++)
            {
                alias = Ce.Entities.EntitiesAlias[i];
                if (alias == "Chicken") ChickenId = i;
                else if (alias == EntityZombieMvk.AliasZombie) ZombieId = i;
                else if (alias == EntityBanditMvk.AliasBandit) BanditId = i;
                //else if (alias == "Droplet") DropletId = i;
            }
        }

        public static void Initialization()
        {
            EntitiesReg.RegisterEntityClass("Item", typeof(EntityItemMvk), true);
            // Снаряд: брошенный предмет, попадание считает сервер
            EntitiesReg.RegisterEntityClass(EntityProjectile.Alias, typeof(EntityProjectileMvk), true);
            // Динамит: снаряд, который взрывается при попадании
            EntitiesReg.RegisterEntityClass(EntityDynamiteMvk.AliasDynamite, typeof(EntityDynamiteMvk), true);
            EntitiesReg.RegisterEntityClass(EntityArrowMvk.AliasArrow, typeof(EntityArrowMvk), true);
            EntitiesReg.RegisterEntityClass(EntityBulletMvk.AliasBullet, typeof(EntityBulletMvk), true);
            EntitiesReg.RegisterEntityClass(EntityTorchMvk.AliasTorch, typeof(EntityTorchMvk), true);
            EntitiesReg.RegisterEntityClass(EntityHookMvk.AliasHook, typeof(EntityHookMvk), true);
            EntitiesReg.RegisterEntityClass("Chicken", typeof(EntityChicken));
            // [ПОВЕДЕНИЕ] Враждебные мобы порта
            EntitiesReg.RegisterEntityClass(EntityZombieMvk.AliasZombie, typeof(EntityZombieMvk));
            EntitiesReg.RegisterEntityClass(EntityBanditMvk.AliasBandit, typeof(EntityBanditMvk));

            //ModelEntitiesReg.RegisterModelEntityClass("Chick");
            //EntitiesReg.RegisterEntityClass("Robinson", typeof(EntityThrowable));

            //ModelEntitiesReg.RegisterModelEntityClass("Robinson");
            //ModelEntitiesReg.RegisterModelEntityClass("Skeleton");
            //ModelEntitiesReg.RegisterModelEntityClass("Robinson2");
            //ModelEntitiesReg.RegisterModelEntityClass("Robinson3");
            //ModelEntitiesReg.RegisterModelEntityClass("Robinson4");
            //ModelEntitiesReg.RegisterModelEntityClass("Chicken3");
            //ModelEntitiesReg.RegisterModelEntityClass("Chicken4");
            //ModelEntitiesReg.RegisterModelEntityClass("Robinson5");
        }
    }
}
