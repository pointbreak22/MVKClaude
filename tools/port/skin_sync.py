"""[ПОВЕДЕНИЕ] Скин пользователя виден другим игрокам (просьба пользователя 27.11): клиент передаёт выбор скина
(Options.PlayerSkin) в пакете настроек игрока PacketC15PlayerSetting, сервер ставит свободный флаг 2 метаданных
живой сущности (IsUserSkin), метаданные расходятся всем клиентам, и рендер рисует такого игрока текстурой UserSkin
(шов клиента client_seam9_user_skin.py). Пакет настроек дополнительно отправляется при входе в мир и при смене
галочки в настройках; трекеры сущностей пересчитываются только при настоящей смене обзора."""
from portlib import load, save, rep

rel = r"Network\Packets\Client\PacketC15PlayerSetting.cs"
t = load(rel)
t = rep(t, """        public PacketC15PlayerSetting(byte overviewChunk) => OverviewChunk = overviewChunk;

        public void ReadPacket(ReadPacket stream) => OverviewChunk = stream.Byte();
        public void WritePacket(WritePacket stream) => stream.Byte(OverviewChunk);""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Игрок со скином пользователя (UserSkin)
        /// </summary>
        public bool UserSkin { get; private set; }

        public PacketC15PlayerSetting(byte overviewChunk, bool userSkin)
        {
            OverviewChunk = overviewChunk;
            UserSkin = userSkin;
        }

        public void ReadPacket(ReadPacket stream)
        {
            OverviewChunk = stream.Byte();
            UserSkin = stream.Bool();
        }

        public void WritePacket(WritePacket stream)
        {
            stream.Byte(OverviewChunk);
            stream.Bool(UserSkin);
        }""", rel, 1)
save(rel, t)

rel = r"Entity\Player\PlayerClientOwner.cs"
t = load(rel)
t = rep(t, """                _game.TrancivePacket(new PacketC15PlayerSetting(OverviewChunk));""",
        """                SendPlayerSetting();""", rel, 1)
t = rep(t, """        /// <summary>
        /// Задать обзор чанков у клиента
        /// </summary>""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Отправить серверу настройки игрока: обзор и выбор скина
        /// </summary>
        public void SendPlayerSetting()
            => _game.TrancivePacket(new PacketC15PlayerSetting(OverviewChunk, Options.PlayerSkin));

        /// <summary>
        /// Задать обзор чанков у клиента
        /// </summary>""", rel, 1)
save(rel, t)

rel = r"Games\GameBase.cs"
t = load(rel)
t = rep(t, """            Player.SetOverviewChunk(Options.OverviewChunk, false);
            // Запущена игра""", """            Player.SetOverviewChunk(Options.OverviewChunk, false);
            // [ПОВЕДЕНИЕ] Выбор скина (и обзор) — серверу, чтобы другие игроки видели скин
            Player.SendPlayerSetting();
            // Запущена игра""", rel, 1)
save(rel, t)

rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """            SetOverviewChunk(packet.OverviewChunk);
            // Изменён обзор корректировка трекеров сущностей
            _flagOverviewChunkChanged = true;""", """            SetUserSkin(packet.UserSkin);
            if (packet.OverviewChunk == OverviewChunk) return;
            SetOverviewChunk(packet.OverviewChunk);
            // Изменён обзор корректировка трекеров сущностей
            _flagOverviewChunkChanged = true;""", rel, 1)
save(rel, t)

rel = r"Entity\EntityLiving.cs"
t = load(rel)
t = rep(t, """        public void SetSneaking(bool sneaking) => SetFlag(1, sneaking);""", """        public void SetSneaking(bool sneaking) => SetFlag(1, sneaking);

        /// <summary>
        /// [ПОВЕДЕНИЕ] Игрок со скином пользователя (свободный флаг 2 метаданных, расходится всем клиентам)
        /// </summary>
        public bool IsUserSkin() => GetFlag(2);

        /// <summary>
        /// [ПОВЕДЕНИЕ] Задать скин пользователя
        /// </summary>
        public void SetUserSkin(bool userSkin) => SetFlag(2, userSkin);""", rel, 1)
save(rel, t)
print("skin_sync ok")
