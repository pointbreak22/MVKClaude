"""[ПОВЕДЕНИЕ] Колющий выпад копьём (просьба пользователя 21.11, как копьё в Майнкрафте): удар по сущности с признаком
Thrust (бит 0x40 действия пакета C03) получает множитель урона от скорости игрока: 1 + скорость × ThrustSpeed
(json предмета, 0 — выпада нет). Скорость считает сервер по пакетам позиции (как разгон для крита и удара щитом)."""
from portlib import load, save, rep

rel = r"Network\Packets\Client\PacketC03UseEntity.cs"
t = load(rel)
t = rep(t, """        public PacketC03UseEntity Hand(bool left)
        {
            Left = left;
            return this;
        }
""", """        public PacketC03UseEntity Hand(bool left)
        {
            Left = left;
            return this;
        }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Колющий выпад (урон от скорости игрока)
        /// </summary>
        public bool Thrust { get; private set; }

        /// <summary>
        /// Пометить удар как колющий выпад
        /// </summary>
        public PacketC03UseEntity AsThrust()
        {
            Thrust = true;
            return this;
        }
""", rel, 1)
t = rep(t, """            Left = (action & 0x80) != 0;
            Action = (EnumAction)(action & 0x7F);""", """            Left = (action & 0x80) != 0;
            Thrust = (action & 0x40) != 0;
            Action = (EnumAction)(action & 0x3F);""", rel, 1)
t = rep(t, "            stream.Byte((byte)((byte)Action | (Left ? 0x80 : 0)));",
        "            stream.Byte((byte)((byte)Action | (Left ? 0x80 : 0) | (Thrust ? 0x40 : 0)));", rel, 1)
save(rel, t)

rel = r"Item\ItemBase.cs"
t = load(rel)
t = rep(t, """                        else if (json.IsKey("HandLight")) HandLight = json.GetInt();""",
        """                        else if (json.IsKey("HandLight")) HandLight = json.GetInt();
                        else if (json.IsKey("ThrustSpeed")) ThrustSpeed = json.GetFloat();""", rel, 1)
t = rep(t, """        public int HandLight { get; protected set; } = 0;""", """        public int HandLight { get; protected set; } = 0;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Колющий выпад: урон × (1 + скорость игрока × ThrustSpeed) (json ThrustSpeed, 0 — нет выпада)
        /// </summary>
        public float ThrustSpeed { get; protected set; } = 0;""", rel, 1)
save(rel, t)

rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """            || (_server.TickCounterGlobal - _speedTick <= 3 && _speed > .3f);""", """            || (_server.TickCounterGlobal - _speedTick <= 3 && _speed > .3f);

        /// <summary>
        /// [ПОВЕДЕНИЕ] Скорость игрока за последние 3 такта (блоков за такт), 0 — стоит
        /// </summary>
        public float MoveSpeed() => _server.TickCounterGlobal - _speedTick <= 3 ? _speed : 0;""", rel, 1)
t = rep(t, """                        if (PresenceBlocks.IsInLiquid) damage *= .75f;""", """                        // [ПОВЕДЕНИЕ] Колющий выпад: урон от скорости игрока
                        if (packet.Thrust && stack != null && stack.Item.ThrustSpeed > 0)
                        {
                            damage *= 1 + MoveSpeed() * stack.Item.ThrustSpeed;
                        }
                        if (PresenceBlocks.IsInLiquid) damage *= .75f;""", rel, 1)
save(rel, t)
print("spear_thrust ok")
