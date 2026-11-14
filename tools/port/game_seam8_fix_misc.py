"""[ПОВЕДЕНИЕ] Мелкие баги оригинала (docs/known-issues.md п. 7):
- /regen без загруженного чанка под игроком падал с NullReferenceException: теперь сообщение в чат;
- GenLayerIsland при ошибке писал её в лог и возвращал пустой массив, на котором генерация падала дальше
  с IndexOutOfRange: теперь после записи в лог ошибка пробрасывается как есть;
- ItemAbTool при ошибке чтения статов из json терял исходное исключение: теперь оно во InnerException.
Общий ChunkPrimer генератора правки не требует: чанки генерируются только в потоке сервера."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Command\CommandRegen.cs"
t = load(rel, GAME)
t = rep(t, """            worldServer.GetChunkServer(player.ChunkPositionX, player.ChunkPositionZ).Regen();""",
"""            ChunkServer chunk = worldServer.GetChunkServer(player.ChunkPositionX, player.ChunkPositionZ);
            if (chunk == null)
            {
                return ChatStyle.Red + L.S("CommandsRegenNoChunk");
            }
            chunk.Regen();""", rel, 1)
if "using Mvk.Engine.World.Chunk;" not in t:
    t = rep(t, "using Mvk.Engine.World;", "using Mvk.Engine.World;\nusing Mvk.Engine.World.Chunk;", rel, 1)
save(rel, t, GAME)

rel = r"World\Gen\Layer\GenLayerIsland.cs"
t = load(rel, GAME)
t = rep(t, """                Logger.Crash(ex);
                return new int[0];""", """                Logger.Crash(ex);
                throw;""", rel, 1)
save(rel, t, GAME)

rel = r"Item\List\Tool\ItemAbTool.cs"
t = load(rel, GAME)
t = rep(t, """                catch
                {
                    throw new Exception(Sr.GetString(Sr.ErrorReadJsonItemStat, Alias));""", """                catch (Exception ex)
                {
                    throw new Exception(Sr.GetString(Sr.ErrorReadJsonItemStat, Alias), ex);""", rel, 1)
save(rel, t, GAME)
print("game_seam8_fix_misc ok")
