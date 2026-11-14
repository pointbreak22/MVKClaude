using System;
using System.Globalization;
using Mvk2.Entity;
using Mvk2.Games;
using Vge;
using Vge.Command;
using Vge.Entity;
using Vge.Entity.Player;
using Vge.Games;
using Vge.Realms;
using Vge.World;

namespace OrigShot
{
    /// <summary>
    /// Серверный мод оригинала с той же отладочной командой /summon, что у Mvk.Desktop (DebugCommands.cs).
    /// Оригинал не меняется: команда регистрируется в подклассе мода внутри OrigShot.
    /// </summary>
    internal sealed class GameModServerShot : GameModServerMvk
    {
        public override void InitCommand(ManagerCommand managerCommand)
        {
            base.InitCommand(managerCommand);
            managerCommand.Registration(new CommandSummon(Server));
        }
    }

    /// <summary>
    /// /summon [количество] [расстояние]: поставить кур перед игроком (по направлению взгляда), код как в порте
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
            float sin = MathF.Sin(player.RotationYaw);
            float cos = MathF.Cos(player.RotationYaw);
            for (int i = 0; i < count; i++)
            {
                float side = (i - (count - 1) / 2f) * 1.5f;
                EntityBase entity = Ce.Entities.CreateEntityServer(EntitiesRegMvk.ChickenId, world);
                entity.PosX = player.PosX + sin * distance + cos * side;
                entity.PosZ = player.PosZ - cos * distance + sin * side;
                entity.PosY = world.GetHeightMap((int)MathF.Floor(entity.PosX), (int)MathF.Floor(entity.PosZ));
                world.SpawnEntityInWorld(entity);
            }
            return "";
        }
    }
}
