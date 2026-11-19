"""Технический п. 1б: реестры мира объектом Registries, корректировка под таблицу мира повторяемая.
В оригинале корректировка очищала таблицы регистрации, и каждый запуск игры заново регистрировал все блоки,
предметы, сущности и строил атлас. Теперь таблица помнит порядок регистрации (RegTable.BeginCorrect),
вспомогательные json по-прежнему освобождаются после корректировки."""
from portlib import load, save, rep

def correct(rel, clear_old, clear_new):
    t = load(rel)
    t = rep(t, "            correct.CorrectRegLoad(Table);", "            Table.BeginCorrect();\n            correct.CorrectRegLoad(Table);", rel, 1)
    t = rep(t, clear_old, clear_new, rel, 1)
    save(rel, t)

correct(r"World\Block\BlocksReg.cs", """            Ce.Blocks = new BlockArrays();

            // Очистить таблицы и вспомогательные данные json
            _Clear();""", """            Ce.Blocks = new BlockArrays();

            // Очистить вспомогательные данные json (таблица регистрации остаётся для следующего мира)
            _parentStats.Clear();
            _shapes.Clear();""")
correct(r"Item\ItemsReg.cs", """            Ce.Items = new ItemArrays();

            // Очистить таблицы и вспомогательные данные json
            _Clear();""", """            Ce.Items = new ItemArrays();

            // Очистить вспомогательные данные json (таблица регистрации остаётся для следующего мира)
            _parentStats.Clear();
            _shapes.Clear();""")
correct(r"Entity\EntitiesReg.cs", """            Ce.Entities = new EntityArrays();

            // Очистить таблицы и вспомогательные данные json
            _Clear();""", """            Ce.Entities = new EntityArrays();

            // Очистить вспомогательные данные json (таблица регистрации остаётся для следующего мира)
            _jsonModels.Clear();
            _shapes.Clear();""")
correct(r"World\BlockEntity\BlocksEntityReg.cs", """            Ce.BlocksEntity = new BlockEntityArrays();

            // Очистить таблицы и вспомогательные данные json
            _Clear();""", """            Ce.BlocksEntity = new BlockEntityArrays();""")
correct(r"Entity\Particle\EntitiesFXReg.cs", """            Ce.EntitiesFX = new EntityFXArrays();
            // Очистить таблицы и вспомогательные данные json
            _Clear();""", """            Ce.EntitiesFX = new EntityFXArrays();""")

rel = r"Games\GameModServer.cs"
t = load(rel)
t = rep(t, """            EntitiesFXReg.Correct(Server.Settings.TableEntitiesFX);
            BlocksReg.Correct(Server.Settings.TableBlocks);
            ItemsReg.Correct(Server.Settings.TableItems);
            EntitiesReg.Correct(Server.Settings.TableEntities);
            BlocksEntityReg.Correct(Server.Settings.TableBlocksEntity);""",
"""            Server.Registries = Registries.Correct(Server.Settings.TableEntitiesFX, Server.Settings.TableBlocks,
                Server.Settings.TableItems, Server.Settings.TableEntities, Server.Settings.TableBlocksEntity);""", rel, 1)
save(rel, t)

rel = r"Games\GameModClient.cs"
t = load(rel)
t = rep(t, """            EntitiesFXReg.Correct(new CorrectTable(packet.EntitiesFX));
            BlocksReg.Correct(new CorrectTable(packet.Blocks));
            ItemsReg.Correct(new CorrectTable(packet.Items));
            EntitiesReg.Correct(new CorrectTable(packet.Entities));
            BlocksEntityReg.Correct(new CorrectTable(packet.BlocksEntity));""",
"""            Registries.Correct(new CorrectTable(packet.EntitiesFX), new CorrectTable(packet.Blocks),
                new CorrectTable(packet.Items), new CorrectTable(packet.Entities), new CorrectTable(packet.BlocksEntity));""", rel, 1)
save(rel, t)

rel = r"Games\GameServer.cs"
t = load(rel)
t = rep(t, """        public readonly GameModServer ModServer;""", """        public readonly GameModServer ModServer;
        /// <summary>
        /// Реестры мира под таблицы id этого сервера (задаёт GameModServer.CorrectObjects)
        /// </summary>
        public Registries Registries;""", rel, 1)
t = rep(t, """                ResponsePacket(e.Side, new PacketS02LoadingGame(Ce.Blocks.BlockAlias, 
                    Ce.Items.ItemAlias, Ce.Entities.EntitiesAlias, 
                    Ce.BlocksEntity.BlockEntityAlias, Ce.EntitiesFX.EntitiesFXAlias));""",
"""                ResponsePacket(e.Side, new PacketS02LoadingGame(Registries.Blocks.BlockAlias,
                    Registries.Items.ItemAlias, Registries.Entities.EntitiesAlias,
                    Registries.BlocksEntity.BlockEntityAlias, Registries.EntitiesFX.EntitiesFXAlias));""", rel, 1)
save(rel, t)
print("arch_registries ok")
