using System.Globalization;
using Mvk.Engine;
using Mvk.Engine.Command;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Games;
using Mvk.Engine.Realms;
using Mvk.Engine.World;
using Mvk.Game.Entity;
using Mvk.Game.Games;

namespace Mvk.Desktop
{
    /// <summary>
    /// Серверный мод Малювек с отладочными командами хоста. Включается только ключом --cmd
    /// (снимки и проверка рендера без человека), в обычной игре команд нет.
    /// </summary>
    internal sealed class GameModServerDebug : GameModServerMvk
    {
        public override void InitCommand(ManagerCommand managerCommand)
        {
            base.InitCommand(managerCommand);
            managerCommand.Registration(new CommandSummon(Server));
        }
    }

    /// <summary>
    /// /summon [количество] [расстояние]: поставить кур перед игроком (по направлению взгляда)
    /// </summary>
    internal sealed class CommandSummon : CommandBase
    {
        public CommandSummon(GameServer server) : base(server) => Name = "summon";

        public override string UseCommand(CommandSender sender)
        {
            PlayerServer player = sender.GetPlayer();
            if (player == null) return ChatStyle.Red + "summon: нет игрока";
            string[] p = sender.GetCommandParams();
            int count = p.Length > 0 ? int.Parse(p[0], CultureInfo.InvariantCulture) : 1;
            float distance = p.Length > 1 ? float.Parse(p[1], CultureInfo.InvariantCulture) : 4f;
            WorldServer world = player.GetWorldServer();
            float sin = System.MathF.Sin(player.RotationYaw);
            float cos = System.MathF.Cos(player.RotationYaw);
            for (int i = 0; i < count; i++)
            {
                // Ряд поперёк взгляда, шаг 1.5 блока
                float side = (i - (count - 1) / 2f) * 1.5f;
                EntityBase entity = Ce.Entities.CreateEntityServer(EntitiesRegMvk.ChickenId, world);
                entity.PosX = player.PosX + sin * distance + cos * side;
                entity.PosZ = player.PosZ - cos * distance + sin * side;
                entity.PosY = world.GetHeightMap((int)System.MathF.Floor(entity.PosX), (int)System.MathF.Floor(entity.PosZ));
                world.SpawnEntityInWorld(entity);
            }
            return "";
        }
    }
}
