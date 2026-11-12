namespace Mvk.Game.World.Gen
{
    /// <summary>
    /// Интерфейс для генерации деревьев
    /// </summary>
    public interface IGenTree
    {
        /// <summary>
        /// Генерация деревьев
        /// </summary>
        GenTree Tree { get; }
    }
}
