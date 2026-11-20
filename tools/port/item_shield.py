"""[ПОВЕДЕНИЕ] Щит: пакет C0B «блок щитом» (new/Network/Packets/Client) и флаг блока игрока на сервере.
Сервер решает, работает ли блок (щит в левой руке, атакующий спереди), снижает урон и изнашивает щит (DamageLiving)."""
from portlib import load, save, rep

rel = r"Network\PacketsInit.cs"
t = load(rel)
t = rep(t, """                case 0x0A: return new PacketC0APlayerAnimation();""", """                case 0x0A: return new PacketC0APlayerAnimation();
                case 0x0B: return new PacketC0BBlocking();""", rel, 1)
save(rel, t)

rel = r"Network\ProcessServerPackets.cs"
t = load(rel)
t = rep(t, """                    case 0x0A: _Handle0APlayerAnimation(sp.Side, (PacketC0APlayerAnimation)sp.Packet); break;""",
"""                    case 0x0A: _Handle0APlayerAnimation(sp.Side, (PacketC0APlayerAnimation)sp.Packet); break;
                    case 0x0B: _Handle0BBlocking(sp.Side, (PacketC0BBlocking)sp.Packet); break;""", rel, 1)
t = rep(t, """        private void _Handle0APlayerAnimation(SocketSide? socketSide, PacketC0APlayerAnimation packet)
            => _server.Players.FindPlayerBySocket(socketSide!)?.PacketPlayerAnimation(packet);""", """        private void _Handle0APlayerAnimation(SocketSide? socketSide, PacketC0APlayerAnimation packet)
            => _server.Players.FindPlayerBySocket(socketSide!)?.PacketPlayerAnimation(packet);

        /// <summary>
        /// Блок щитом начат или закончен
        /// </summary>
        private void _Handle0BBlocking(SocketSide? socketSide, PacketC0BBlocking packet)
            => _server.Players.FindPlayerBySocket(socketSide!)?.PacketBlocking(packet);""", rel, 1)
save(rel, t)

rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """        public virtual void PacketUseEntity(PacketC03UseEntity packet)""", """        /// <summary>
        /// Игрок держит блок щитом (вторая кнопка зажата); работает ли блок, решает урон сервера
        /// </summary>
        public bool IsBlocking { get; private set; }

        /// <summary>
        /// Пакет: блок щитом начат или закончен
        /// </summary>
        public void PacketBlocking(PacketC0BBlocking packet) => IsBlocking = packet.Blocking;

        public virtual void PacketUseEntity(PacketC03UseEntity packet)""", rel, 1)
save(rel, t)
# Наследник (HandManagerMvk) отправляет пакет блока
rel = r"Entity\Player\HandManager.cs"
t = load(rel)
t = rep(t, "        private readonly GameBase _game;", "        protected readonly GameBase _game;", rel, 1)
save(rel, t)
print("item_shield ok")
