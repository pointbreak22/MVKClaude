using Mvk.Game.World.Block;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Item;
using Mvk.Engine.World.Block;

namespace Mvk.Game.Item.List.Tool
{
    /// <summary>
    /// Предмет лопата
    /// </summary>
    public class ItemShovel : ItemAbTool
    {
        /// <summary>
        /// Действие предмета ЛКМ.
        /// Для клиента.
        /// </summary>
        /// <param name="begin">Первый клик, без паузы</param>
        /// <summary>
        /// [ПОВЕДЕНИЕ] Сила удара по блоку (сервер): по своему материалу — DigBonus
        /// </summary>
        public override float DigStrength(BlockBase block, PlayerBase player)
            => block.Material.IndexMaterial == (int)EnumMaterial.Loose ? DigBonus : 1;

        /// <summary>
        /// Ускорение добычи блока (сервер): свои материалы и блоки, которые ломаются рукой
        /// </summary>
        public override float DigAcceleration(BlockBase block, PlayerBase player)
            => player.CanDestroyedBlock(block) || block.Material.IndexMaterial == (int)EnumMaterial.Loose ? Acceleration : (player.CanHitBlock(block) ? 1 : 0);

        public override ResultHandAction OnAction(bool begin, ItemStack stack, PlayerClientOwner player)
        {
            if (player.MovingObject.IsBlock())
            {
                // Опции для возможности копать, можно проверить типа блока player.MovingObject.Block
                BlockBase block = player.MovingObject.Block.GetBlock();
                if (player.CanDestroyedBlock(block) 
                    || block.Material.IndexMaterial == (int)EnumMaterial.Loose)
                {
                    return new ResultHandAction(Pause, Acceleration);
                }
            }

            return base.OnAction(begin, stack, player);
        }
    }
}
