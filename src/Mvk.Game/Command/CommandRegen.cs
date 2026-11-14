using System.Diagnostics;
using Mvk.Engine.Command;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Games;
using Mvk.Engine.Realms;
using Mvk.Engine.World;
using Mvk.Engine.World.Chunk;

namespace Mvk.Game.Command
{
    /// <summary>
    /// Команда регенерации
    /// </summary>
    public class CommandRegen : CommandBase
    {
        public CommandRegen(GameServer server) : base(server)
        {
            Name = "regen";
        }

        /// <summary>
        /// Возвращает true, если отправителю данной команды разрешено использовать эту команду
        /// </summary>
        public override string UseCommand(CommandSender sender)
        {
            PlayerServer player = sender.GetPlayer();
            if (player == null)
            {
                return ChatStyle.Red + L.S("CommandsRegenNotPlayer");
            }
            //string[] commandParams = sender.GetCommandParams();
            WorldServer worldServer = player.GetWorldServer();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            ChunkServer chunk = worldServer.GetChunkServer(player.ChunkPositionX, player.ChunkPositionZ);
            if (chunk == null)
            {
                return ChatStyle.Red + L.S("CommandsRegenNoChunk");
            }
            chunk.Regen();
            string result = L.S("RegenChunk[{0}:{1}] {2:0.00}ms",
                player.ChunkPositionX, player.ChunkPositionZ,
                stopwatch.ElapsedTicks / (float)(Stopwatch.Frequency / 1000f)
            );
            worldServer.Filer.Log.Log(result);
            return ChatStyle.Gray + result;
        }
    }
}
