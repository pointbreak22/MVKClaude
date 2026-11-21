"""[ПОВЕДЕНИЕ] Враждебные мобы (просьба пользователя 21.11): наибольшее здоровье сущности доступно снаружи
(полоска здоровья на клиенте), признак «полоска всегда» у враждебных мобов."""
from portlib import load, save, rep

rel = r"Entity\EntityLiving.cs"
t = load(rel)
t = rep(t, """        public float GetHealth() => MetaData.GetFloat(1);""", """        public float GetHealth() => MetaData.GetFloat(1);

        /// <summary>
        /// [ПОВЕДЕНИЕ] Наибольшее здоровье сущности
        /// </summary>
        public float GetHealthMax() => _GetHelathMax();

        /// <summary>
        /// [ПОВЕДЕНИЕ] Полоска здоровья видна всегда (враждебные мобы), иначе только при неполном здоровье
        /// </summary>
        public virtual bool HealthBarAlways => false;""", rel, 1)
save(rel, t)
print("mobs_hostile ok")

rel = r"Entity\List\EntityProjectile.cs"
t = load(rel)
t = rep(t, """        /// <summary>
        /// Индекс сущности в активных реестрах""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Выстрел снарядом от глаз живой сущности (моба) в направлении yaw, pitch
        /// </summary>
        public static EntityProjectile? ThrowFrom(EntityLiving thrower, ItemStack stack, float power, string alias,
            float yaw, float pitch)
        {
            WorldServer worldServer = thrower.GetWorldServer();
            if (worldServer == null) return null;
            int index = _IndexEntity(alias);
            if (index < 0) return null;
            if (Ce.Entities.CreateEntityServer((ushort)index, worldServer) is not EntityProjectile projectile) return null;
            projectile.SetEntityItemStack(stack);
            projectile.ThrowerId = thrower.Id;
            projectile.Power = power;
            projectile.IsFlying = true;
            float cosPitch = Glm.Cos(pitch);
            float dx = Glm.Sin(yaw) * cosPitch, dy = Glm.Sin(pitch), dz = -Glm.Cos(yaw) * cosPitch;
            projectile.PosX = thrower.PosX + dx * .5f;
            projectile.PosY = thrower.PosY + thrower.SizeLiving.GetEye() - .1f + dy * .5f;
            projectile.PosZ = thrower.PosZ + dz * .5f;
            float speed = 1.4f * power;
            projectile.Physics.MotionX = dx * speed;
            projectile.Physics.MotionY = dy * speed;
            projectile.Physics.MotionZ = dz * speed;
            projectile.BeforeDrop();
            worldServer.SpawnEntityInWorld(projectile);
            return projectile;
        }

        /// <summary>
        /// Индекс сущности в активных реестрах""", rel, 1)
save(rel, t)
print("mobs_hostile projectile ok")

# Названия текстур формы берутся после очистки текстур слоёв (как сами текстуры): иначе индекс текстуры
# после текстур слоёв в bbmodel (свои текстуры мобов) выходит за массив глубин. Прежние индексы не меняются.
rel = r"Entity\Shape\ShapeEntity.cs"
t = load(rel)
t = rep(t, """            _nameTextures = _definition.GenNameTextures();
            Textures = _definition.GenTextures();""", """            Textures = _definition.GenTextures();
            _nameTextures = _definition.GenNameTextures();""", rel, 1)
save(rel, t)
print("mobs_hostile shape ok")
