"""Технический п. 2: Nullable в Mvk.Engine по папкам, начиная с Network и Util.
Файлы папок получают #nullable enable, поля и события, которые законно бывают null, помечены `?`
(сторона сокета у локального игрока — null, события без подписчиков, кэши пакета между частями).
Поведение не меняется; настоящие ошибки с null исправляются отдельными швами [ПОВЕДЕНИЕ]."""
import os, re
from portlib import load, save, rep, sub, ENG, fail

FOLDERS = ["Network", "Util"]

for folder in FOLDERS:
    for d, _, fs in os.walk(os.path.join(ENG, folder)):
        for f in fs:
            if not f.endswith(".cs"): continue
            rel = os.path.relpath(os.path.join(d, f), ENG)
            t = load(rel)
            if not t.startswith("#nullable"):
                t = "#nullable enable\n" + t
            # События без подписчиков — null
            t = re.sub(r"(\bevent [A-Za-z]+)( \w+;)", r"\1?\2", t)
            save(rel, t)


def edit(rel, pairs):
    t = load(rel)
    for a, b in pairs:
        t = rep(t, a, b, rel, 1)
    save(rel, t)


# Сторона сокета null — локальный игрок (одиночная игра)
edit(r"Network\PacketBufferEvent.cs", [("        public readonly SocketSide Side;", "        public readonly SocketSide? Side;")])
t = load(r"Network\PacketBufferEvent.cs")
t = t.replace("int count, SocketSide socketSide)", "int count, SocketSide? socketSide)")
save(r"Network\PacketBufferEvent.cs", t)
t = load(r"Network\ProcessServerPackets.cs")
t, n = re.subn(r"\(SocketSide socketSide, ", "(SocketSide? socketSide, ", t)
if n < 15: fail(f"ProcessServerPackets: {n}")
save(r"Network\ProcessServerPackets.cs", t)

# Неизвестный номер пакета — null
for name in ("InitServer", "InitClient"):
    edit(r"Network\PacketsInit.cs", [(f"        public static IPacket {name}(byte index)", f"        public static IPacket? {name}(byte index)")])

# Поля пакетов, которые бывают пустыми
edit(r"Network\Packets\Server\PacketS0FSpawnMob.cs", [("        public ItemStack[] Stacks { get; private set; }", "        public ItemStack[]? Stacks { get; private set; }")])
edit(r"Network\Packets\Server\PacketS21ChunkData.cs", [("        public byte[] BufferRead { get; private set; }", "        public byte[]? BufferRead { get; private set; }")])
edit(r"Network\Packets\Server\PacketS2FSetSlot.cs", [("        public ItemStack Stack { get; private set; }", "        public ItemStack? Stack { get; private set; }")])
edit(r"Network\ReadPacket.cs", [("        private byte[] _buffer;", "        private byte[] _buffer = Array.Empty<byte>();")])
edit(r"Network\ReceivingBytes.cs", [("        private byte[] bytesCache;", "        private byte[]? bytesCache;"),
                                     ("        private byte[] bytesCache5;", "        private byte[]? bytesCache5;")])
edit(r"Network\StateObject.cs", [("        public Socket WorkSocket { get; protected set; } = null;", "        public Socket? WorkSocket { get; protected set; }"),
                                  ("            return WorkSocket.RemoteEndPoint.ToString();", "            return WorkSocket?.RemoteEndPoint?.ToString() ?? \"\";")])
edit(r"Network\SocketServer.cs", [("        private Socket socket;", "        private Socket? socket;"),
                                   ("        private void NetThread(object obj)", "        private void NetThread(object? obj)"),
                                   ("                SocketSide socketClient = obj as SocketSide;", "                SocketSide socketClient = (SocketSide)obj!;"),
                                   ("        private void SocketSide_Connected(object sender, EventArgs e)", "        private void SocketSide_Connected(object? sender, EventArgs e)"),
                                   ("            SocketSide socketSide = sender as SocketSide;", "            SocketSide socketSide = (SocketSide)sender!;")])
edit(r"Network\SocketSide.cs", [("        private AutoResetEvent waitHandler;", "        private AutoResetEvent? waitHandler;"),
                                 ("            IPEndPoint iPEndPoint = socket.RemoteEndPoint as IPEndPoint;", "            IPEndPoint iPEndPoint = (IPEndPoint)socket.RemoteEndPoint!;")])

# Util
edit(r"Util\BufferedImage.cs", [("        private BufferedImage[] _images;", "        private BufferedImage[]? _images;"),
                                 ("        public BufferedImage GetLevelMipMap(int index) => _images[index];", "        public BufferedImage GetLevelMipMap(int index) => _images![index];")])
edit(r"Util\FileAssets.cs", [("                    string strLine = file.ReadLine();", "                    string? strLine = file.ReadLine();")])
edit(r"Util\OptionsFile.cs", [("                        string strLine = file.ReadLine();", "                        string? strLine = file.ReadLine();")])
t = load(r"Util\Logger.cs")
t, n = re.subn(r"params object\[\] args", "params object?[] args", t)
if n != 6: fail(f"Logger args {n}")
t = rep(t, "            public int CompareTo(object obj)", "            public int CompareTo(object? obj)", "Logger", 1)
t = rep(t, "            string stackTrace = e.InnerException == null", "            string? stackTrace = e.InnerException == null", "Logger", 1)
save(r"Util\Logger.cs", t)
edit(r"Util\MovingObjectPosition.cs", [("        public EntityBase Entity { get; private set; }", "        public EntityBase? Entity { get; private set; }"),
                                        ("        public override bool Equals(object obj)", "        public override bool Equals(object? obj)")])
t = load(r"Util\Options.cs")
t, n = re.subn(r"(public static string Path\w+ \{ get; private set; \})", lambda m: m.group(1) + ' = "";', t)
if n != 12: fail(f"Options paths {n}")
save(r"Util\Options.cs", t)
edit(r"Util\Profiler.cs", [("        private string _profilingSection;", "        private string _profilingSection = \"\";"),
                            ("        private string _sectionText;", "        private string _sectionText = \"\";")])
# Места, где значение не null по логике кода (оператор !); где null возможен на деле — шов nullable_fixes.py
edit(r"Network\Packets\Server\PacketS0FSpawnMob.cs", [("                int count = Stacks.Length;", "                int count = Stacks!.Length;")])
edit(r"Network\ProcessClientPackets.cs", [("_readPacket.Receive(PacketsInit.InitServer(buffer[0]))", "_readPacket.Receive(PacketsInit.InitServer(buffer[0])!)")])
edit(r"Network\ProcessServerPackets.cs", [("readPacket.Receive(PacketsInit.InitClient(buffer[0]))", "readPacket.Receive(PacketsInit.InitClient(buffer[0])!)"),
                                          ("            public SocketSide Side;", "            public SocketSide? Side;"),
                                          ("            public SocketPacket(SocketSide side, IPacket packet)", "            public SocketPacket(SocketSide? side, IPacket packet)")])
t = load(r"Network\ReceivingBytes.cs")
t = rep(t, "                    bytesCache[i + begin] = dataPacket[i];", "                    bytesCache![i + begin] = dataPacket[i];", "ReceivingBytes", 1)
t = rep(t, "                    bytesCache[i] = vs[i];", "                    bytesCache![i] = vs[i];", "ReceivingBytes", 1)
t = rep(t, "                OnReceive(new PacketBufferEventArgs(bytesCache, bodyFactLength));", "                OnReceive(new PacketBufferEventArgs(bytesCache!, bodyFactLength));", "ReceivingBytes", 1)
save(r"Network\ReceivingBytes.cs", t)
edit(r"Network\SocketServer.cs", [("                SocketSide_Error(obj, new ErrorEventArgs(e));", "                SocketSide_Error(obj!, new ErrorEventArgs(e));")])
edit(r"Network\SocketSide.cs", [("        private Socket socket;", "        private Socket? socket;"),
                                 ("                waitHandler.WaitOne();", "                waitHandler!.WaitOne();"),
                                 # Сокет уже закрыт в другом потоке: исключение ловит catch ниже, как в оригинале
                                 ("                bytesRead = socket.Receive(buff);", "                bytesRead = socket!.Receive(buff);"),
                                 ("""                packets.Add(ret);
                // Сигнализируем, что waitHandler в сигнальном состоянии
                waitHandler.Set();""", """                packets.Add(ret);
                // Сигнализируем, что waitHandler в сигнальном состоянии
                waitHandler!.Set();""")])
edit(r"Util\MovingObjectPosition.cs", [("                    return moving.Entity.Id == Entity.Id;", "                    return moving.Entity!.Id == Entity!.Id;")])
print("nullable_network_util ok")
