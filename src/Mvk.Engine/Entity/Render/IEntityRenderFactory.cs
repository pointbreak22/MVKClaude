namespace Mvk.Engine.Entity.Render
{
    /// <summary>
    /// Фабрика объектов рендера сущностей. Реализует клиент (Engine.Client), сервер её не использует.
    /// Вид рендера задаёт сама сущность ключом (<see cref="EntityRenderKind"/>), мод может добавить свои ключи
    /// и зарегистрировать для них рендер на стороне клиента.
    /// </summary>
    public interface IEntityRenderFactory
    {
        /// <summary>
        /// Создать рендер сущности
        /// </summary>
        /// <param name="kind">Ключ вида рендера</param>
        /// <param name="entity">Сущность</param>
        /// <param name="resources">Ресурсы сущности (модель, текстуры), может быть null у предметов и частиц</param>
        EntityRenderBase Create(string kind, EntityBase entity, ResourcesEntity resources);
    }

    /// <summary>
    /// Ключи видов рендера сущностей движка
    /// </summary>
    public static class EntityRenderKind
    {
        /// <summary>Модель без анимации (EntityRenderClient)</summary>
        public const string Client = "Client";
        /// <summary>Модель с анимацией (EntityRenderAnimation)</summary>
        public const string Animation = "Animation";
        /// <summary>Модель с анимацией, глазами и ртом (EntityRenderEyeMouth)</summary>
        public const string EyeMouth = "EyeMouth";
        /// <summary>Выброшенный предмет (EntityRenderItem)</summary>
        public const string Item = "Item";
        /// <summary>Частица (EntityRenderParticle)</summary>
        public const string Particle = "Particle";
    }
}
