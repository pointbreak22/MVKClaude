"""Замеры этапа 7: время этапов чанка на сервере (Util/ChunkStats.cs). На поведение не влияет:
вокруг вызовов только метки времени Stopwatch.GetTimestamp и Interlocked-счётчики."""
from portlib import load, save, rep

rel = r"World\Chunk\ChunkProviderServer.cs"
t = load(rel)
t = rep(t, """                    chunk.LoadFileChunk();""", """                    long statsBegin = ChunkStats.Begin();
                    chunk.LoadFileChunk();
                    ChunkStats.End(ChunkStats.Stage.Load, statsBegin);""", rel, 1)
t = rep(t, """                    // Начинаем генерацию рельефа
                    ChunkGenerate.Relief(chunk);""", """                    // Начинаем генерацию рельефа
                    long statsBegin = ChunkStats.Begin();
                    ChunkGenerate.Relief(chunk);
                    ChunkStats.End(ChunkStats.Stage.Relief, statsBegin);""", rel, 1)
save(rel, t)

rel = r"World\Chunk\ChunkServer.cs"
t = load(rel)
t = rep(t, """                provider.ChunkGenerate.Decoration(provider, this);""", """                long statsBegin = ChunkStats.Begin();
                provider.ChunkGenerate.Decoration(provider, this);
                ChunkStats.End(ChunkStats.Stage.Decoration, statsBegin);""", rel, 1)
t = rep(t, """                if (World.Settings.HasNoSky)
                {
                    Light.GenerateHeightMap();
                }
                else
                {
                    Light.GenerateHeightMapSky(); // 0.09 - 0.13 мс
                }""", """                long statsBegin = ChunkStats.Begin();
                if (World.Settings.HasNoSky)
                {
                    Light.GenerateHeightMap();
                }
                else
                {
                    Light.GenerateHeightMapSky(); // 0.09 - 0.13 мс
                }
                ChunkStats.End(ChunkStats.Stage.HeightMap, statsBegin);""", rel, 1)
t = rep(t, """                Light.StartRecheckGaps(World.Settings.HasNoSky); // 0.12 - 0.2 мс""", """                long statsBegin = ChunkStats.Begin();
                Light.StartRecheckGaps(World.Settings.HasNoSky); // 0.12 - 0.2 мс
                ChunkStats.End(ChunkStats.Stage.SideLight, statsBegin);""", rel, 1)
save(rel, t)
rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """                    SendPacket(new PacketS21ChunkData(chunk, true, uint.MaxValue));""", """                    long statsBegin = ChunkStats.Begin();
                    SendPacket(new PacketS21ChunkData(chunk, true, uint.MaxValue));
                    ChunkStats.End(ChunkStats.Stage.Pack, statsBegin);""", rel, 1)
save(rel, t)

rel = r"Network\ProcessClientPackets.cs"
t = load(rel)
t = rep(t, """            long time = Game.Time();
            if (Game.World.ChunkPrClient.PacketChunckData(packet))""", """            long time = Game.Time();
            long statsBegin = ChunkStats.Begin();
            bool received = Game.World.ChunkPrClient.PacketChunckData(packet);
            ChunkStats.End(ChunkStats.Stage.Unpack, statsBegin);
            if (received)""", rel, 1)
save(rel, t)
print("perf_chunkstats ok")
