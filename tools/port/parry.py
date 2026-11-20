"""[ПОВЕДЕНИЕ] Парирование щитом (идеи рук): блок, поднятый нажатием кнопки щита (пакет C0B с признаком Parry),
первые ParryTicks тактов на сервере снимает весь урон и отбрасывает атакующего (DamageLiving)."""
from portlib import load, save, rep

rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """        public void PacketBlocking(PacketC0BBlocking packet)
        {
            IsBlocking = packet.Blocking;""", """        public void PacketBlocking(PacketC0BBlocking packet)
        {
            IsBlocking = packet.Blocking;
            if (packet.Blocking && packet.Parry) ParryTick = _server.TickCounterGlobal;""", rel, 1)
t = rep(t, """        public bool IsDrawing { get; private set; }""", """        public bool IsDrawing { get; private set; }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Такт сервера, когда блок щитом подняли нажатием (окно парирования)
        /// </summary>
        public uint ParryTick { get; private set; }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Окно парирования в тактах (0.25 с)
        /// </summary>
        public const int ParryTicks = 5;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Идёт ли окно парирования
        /// </summary>
        public bool IsParrying() => IsBlocking && ParryTick != 0 && _server.TickCounterGlobal - ParryTick <= ParryTicks;""", rel, 1)
save(rel, t)
print("parry ok")
