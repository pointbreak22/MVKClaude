using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using GoldenGen;
using Mvk.Engine.Entity;
using Mvk.Engine.Games;
using Mvk.Engine.Util;
using Mvk.Engine.World.Chunk;
using Mvk.Game.Games;
using Mvk.Game.World;

namespace Mvk.Game.Tests
{
    /// <summary>
    /// Локальная игра без окна: сервер и клиент в одном процессе, как при «Одиночной игре» оригинала.
    /// Проверяет, что логика клиента, отделённая от окна (ADR 0003), действительно работает:
    /// игрок входит в мир и получает чанки по пакетам.
    /// </summary>
    [Collection("Registries")]
    public class GameLocalHeadlessTests
    {
        [Fact]
        public void PlayerEntersWorldAndReceivesChunks()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            const int slot = 96;
            string pathGame = Options.PathGames + (slot + 1).ToString();
            if (Directory.Exists(pathGame)) Directory.Delete(pathGame, true);

            IslandGolden.InitRegistries();
            EntitiesReg.TextureManagerRun();
            Options.OverviewChunk = 4;

            var host = new HeadlessGameClientHost(new GameModServerMvk());
            var game = new GameLocal(host, new GameSettings(slot, 1), new AllWorldsMvk(), new GameModClientMvk());
            Exception? error = null;
            string? stopped = null;
            game.Error += (_, e) => error = e.Exception;
            game.Stoped += (_, e) => stopped = e.Notification;

            game.GameStarting();

            // Тикаем клиент, как окно оригинала (20 тактов в секунду), пока под игроком не появится чанк
            Stopwatch time = Stopwatch.StartNew();
            ChunkClient? chunk = null;
            int loaded = 0;
            while (time.Elapsed.TotalSeconds < 120 && error == null && stopped == null)
            {
                game.OnTick(50f);
                if (host.Started && game.World != null)
                {
                    chunk = game.World.ChunkPrClient.GetChunkClient(game.Player.ChunkPositionX, game.Player.ChunkPositionZ);
                    if (chunk != null && chunk.IsChunkPresent)
                    {
                        loaded = 0;
                        for (int x = -2; x <= 2; x++)
                            for (int z = -2; z <= 2; z++)
                            {
                                ChunkClient c = game.World.ChunkPrClient.GetChunkClient(
                                    game.Player.ChunkPositionX + x, game.Player.ChunkPositionZ + z);
                                if (c != null && c.IsChunkPresent) loaded++;
                            }
                        if (loaded == 25) break;
                    }
                }
                Thread.Sleep(10);
            }

            Assert.True(error == null, "Ошибка игры: " + error);
            Assert.True(stopped == null, "Игра остановилась: " + stopped);
            Assert.True(host.Started, "Игрок не вошёл в мир за 120 секунд");
            Assert.Equal(25, loaded);
            Assert.True(game.World!.Settings != null, "Клиент не получил настройки мира");

            // Чанк клиента не пустой: в нём есть блоки, пришедшие с сервера
            bool hasBlocks = false;
            foreach (ChunkStorage storage in chunk!.StorageArrays)
            {
                if (storage.Data != null && !storage.IsEmptyData()) hasBlocks = true;
            }
            Assert.True(hasBlocks, "Чанк под игроком пустой");

            // Остановка: сервер сохраняет мир и сообщает клиенту
            game.GameStoping("test", false);
            time.Restart();
            while (time.Elapsed.TotalSeconds < 60 && stopped == null && error == null)
            {
                game.OnTick(50f);
                Thread.Sleep(10);
            }
            Assert.True(error == null, "Ошибка при остановке: " + error);
            Assert.Equal("test", stopped);
            game.Dispose();
        }
    }
}
