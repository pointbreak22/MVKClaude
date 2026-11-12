namespace Mvk.Game.Entity
{
    /// <summary>
    /// Виды рендера сущностей Малювек, дополнительно к EntityRenderKind движка.
    /// Фабрика клиента (Mvk.Game.Client) создаёт по ним объекты рендера.
    /// </summary>
    public static class EntityRenderKindMvk
    {
        /// <summary>
        /// Игрок-владелец (вид от первого лица, руки)
        /// </summary>
        public const string Owner = "Owner";
    }
}
