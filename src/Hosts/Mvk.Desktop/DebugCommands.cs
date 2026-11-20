using Mvk.Engine.World.Block;
using Mvk.Game.Entity.List;
using Mvk.Game.Entity.Inventory;
using Mvk.Engine.Item.List;
using Mvk.Engine.Item;
using Mvk.Engine.Entity.List;
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
            managerCommand.Registration(new CommandThrow(Server));
            managerCommand.Registration(new CommandHealth(Server));
            managerCommand.Registration(new CommandWear(Server));
            managerCommand.Registration(new CommandHurt(Server));
            managerCommand.Registration(new CommandSetBlock(Server));
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

    /// <summary>
    /// /throw псевдоним [сила] [aim|-] [сущность]: бросить предмет снарядом от игрока (проверка EntityProjectile),
    /// aim — прямо в ближайшего моба, сущность — снаряд в реестре (например Dynamite)
    /// </summary>
    internal sealed class CommandThrow : CommandBase
    {
        public CommandThrow(GameServer server) : base(server) => Name = "throw";

        public override string UseCommand(CommandSender sender)
        {
            PlayerServer player = sender.GetPlayer();
            if (player == null) return ChatStyle.Red + "throw: нет игрока";
            string[] p = sender.GetCommandParams();
            if (p.Length == 0) return ChatStyle.Red + "throw: нужен псевдоним предмета";
            float power = p.Length > 1 ? float.Parse(p[1], CultureInfo.InvariantCulture) : 1f;
            for (int i = 0; i < Ce.Items.ItemAlias.Length; i++)
            {
                if (Ce.Items.ItemAlias[i] == p[0])
                {
                    EntityProjectile? projectile = EntityProjectile.Throw(player, new ItemStack(Ce.Items.ItemObjects[i], 1), power,
                        p.Length > 3 ? p[3] : EntityProjectile.Alias);
                    if (projectile != null && p.Length > 2 && p[2] == "aim") _Aim(player, projectile, power);
                    return "";
                }
            }
            return ChatStyle.Red + "throw: нет предмета " + p[0];
        }

        /// <summary>
        /// Направить снаряд прямо в ближайшую живую сущность (не игрока): проверка попадания без прицеливания
        /// </summary>
        private static void _Aim(PlayerServer player, EntityProjectile projectile, float power)
        {
            WorldServer world = player.GetWorldServer();
            EntityLiving? target = null;
            float best = float.MaxValue;
            for (int i = 0; i < world.LoadedEntityList.Count; i++)
            {
                if (world.LoadedEntityList.GetAt(i) is EntityLiving living && living is not PlayerServer)
                {
                    float dx = living.PosX - projectile.PosX, dz = living.PosZ - projectile.PosZ;
                    float d = dx * dx + dz * dz;
                    if (d < best) { best = d; target = living; }
                }
            }
            if (target == null) return;
            float tx = target.PosX - projectile.PosX;
            float len0 = System.MathF.Sqrt(tx * tx + (target.PosZ - projectile.PosZ) * (target.PosZ - projectile.PosZ));
            float ty = target.PosY + target.Size.GetHeight() * .5f - projectile.PosY + len0 * len0 * .004f;
            float tz = target.PosZ - projectile.PosZ;
            float len = System.MathF.Sqrt(tx * tx + ty * ty + tz * tz);
            float speed = 1.4f * power;
            projectile.Physics.MotionX = tx / len * speed;
            projectile.Physics.MotionY = ty / len * speed + .1f;
            projectile.Physics.MotionZ = tz / len * speed;
        }
    }

    /// <summary>
    /// /hp: напечатать в консоль здоровье живых сущностей мира, кроме игроков
    /// </summary>
    internal sealed class CommandHealth : CommandBase
    {
        public CommandHealth(GameServer server) : base(server) => Name = "hp";

        public override string UseCommand(CommandSender sender)
        {
            PlayerServer player = sender.GetPlayer();
            if (player == null) return "";
            WorldServer world = player.GetWorldServer();
            for (int i = 0; i < world.LoadedEntityList.Count; i++)
            {
                if (world.LoadedEntityList.GetAt(i) is EntityLiving living && living is not PlayerServer)
                {
                    System.Console.WriteLine($"hp {living.Id}: {living.GetHealth()} dead {living.IsDead} pos {living.PosX:0.0} {living.PosY:0.0} {living.PosZ:0.0} size {living.Size.GetHeight():0.00} coll {living.CanBeCollidedWith()}");
                }
            }
            return "";
        }
    }

    /// <summary>
    /// /wear псевдоним...: надеть одежду или броню в её слот (слот одежды = карманы + ключ части тела - 1)
    /// </summary>
    internal sealed class CommandWear : CommandBase
    {
        public CommandWear(GameServer server) : base(server) => Name = "wear";

        public override string UseCommand(CommandSender sender)
        {
            if (sender.GetPlayer() is not PlayerServerMvk player) return ChatStyle.Red + "wear: нет игрока";
            foreach (string alias in sender.GetCommandParams())
            {
                for (int i = 0; i < Ce.Items.ItemAlias.Length; i++)
                {
                    if (Ce.Items.ItemAlias[i] != alias || Ce.Items.ItemObjects[i] is not ItemCloth cloth) continue;
                    for (byte key = 2; key <= InventoryPlayerMvk.ClothCount; key++)
                    {
                        if (cloth.CheckSlotClothKey(key))
                        {
                            player.InvPlayer.SetInventorySlotContents(InventoryPlayerMvk.PocketCount + key - 1, new ItemStack(cloth, 1));
                            break;
                        }
                    }
                }
            }
            return "";
        }
    }

    /// <summary>
    /// /hurt урон [высота]: нанести игроку урон атакой (высота -1 — всё тело) и напечатать здоровье до и после
    /// </summary>
    internal sealed class CommandHurt : CommandBase
    {
        public CommandHurt(GameServer server) : base(server) => Name = "hurt";

        public override string UseCommand(CommandSender sender)
        {
            PlayerServer player = sender.GetPlayer();
            if (player?.Damage == null) return "";
            string[] p = sender.GetCommandParams();
            float damage = p.Length > 0 ? float.Parse(p[0], CultureInfo.InvariantCulture) : 1f;
            float hitY = p.Length > 1 ? float.Parse(p[1], CultureInfo.InvariantCulture) : -1f;
            float before = player.GetHealth();
            player.Damage.OnAttack((byte)EnumDamageSource.Player, hitY, damage, null!);
            System.Console.WriteLine($"hurt {damage} at {hitY}: {before} -> {player.GetHealth()}");
            return "";
        }
    }

    /// <summary>
    /// /setblock псевдоним [расстояние] [сбоку]: поставить блок на землю перед игроком (по взгляду), сбоку — смещение
    /// </summary>
    internal sealed class CommandSetBlock : CommandBase
    {
        public CommandSetBlock(GameServer server) : base(server) => Name = "setblock";

        public override string UseCommand(CommandSender sender)
        {
            PlayerServer player = sender.GetPlayer();
            if (player == null) return "";
            string[] p = sender.GetCommandParams();
            if (p.Length == 0) return ChatStyle.Red + "setblock: нужен псевдоним блока";
            float distance = p.Length > 1 ? float.Parse(p[1], CultureInfo.InvariantCulture) : 3f;
            float side = p.Length > 2 ? float.Parse(p[2], CultureInfo.InvariantCulture) : 0f;
            for (int i = 0; i < Ce.Blocks.BlockAlias.Length; i++)
            {
                if (Ce.Blocks.BlockAlias[i] != p[0]) continue;
                WorldServer world = player.GetWorldServer();
                float sin = System.MathF.Sin(player.RotationYaw), cos = System.MathF.Cos(player.RotationYaw);
                int x = (int)System.MathF.Floor(player.PosX + sin * distance + cos * side);
                int z = (int)System.MathF.Floor(player.PosZ - cos * distance + sin * side);
                int y = world.GetHeightMap(x, z);
                world.SetBlockState(new BlockPos(x, y, z), new BlockState(i), BlockFlag.cNotify);
                BlockState near = world.GetBlockState(new BlockPos(x + 1, y, z));
                System.Console.WriteLine($"setblock {p[0]} at {x} {y} {z}, рядом свет блока {near.LightBlock} неба {near.LightSky}");
                return "";
            }
            return ChatStyle.Red + "setblock: нет блока " + p[0];
        }
    }
}
