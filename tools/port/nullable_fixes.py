"""[ПОВЕДЕНИЕ] Ошибки с null, найденные при включении Nullable в Network (технический п. 2б, improvements R31).
- Пакет с неизвестным номером: PacketsInit возвращает null, ReadPacket.Receive падал с NullReferenceException
  в потоке сети (на сервере это мог вызвать любой клиент). Теперь такой пакет пропускается.
- SocketSide.SendPacket до запуска потока отправки: waitHandler ещё null, отправка падала. Теперь пакет встаёт
  в очередь, поток отправит его при старте (обработчик создаётся в сигнальном состоянии)."""
from portlib import load, save, rep

rel = r"Network\ProcessClientPackets.cs"
t = load(rel)
t = rep(t, "            _ReceivePacket(_readPacket.Receive(PacketsInit.InitServer(buffer[0])!));",
"""            IPacket? packet = PacketsInit.InitServer(buffer[0]);
            // Неизвестный номер пакета пропускаем (в оригинале исключение в потоке сети)
            if (packet == null) return;
            _ReceivePacket(_readPacket.Receive(packet));""", rel, 1)
save(rel, t)

rel = r"Network\ProcessServerPackets.cs"
t = load(rel)
t = rep(t, """            ReadPacket readPacket = new ReadPacket();
            readPacket.SetBuffer(buffer);
            _ReceivePacket(socketSide, readPacket.Receive(PacketsInit.InitClient(buffer[0])!));""",
"""            IPacket? packet = PacketsInit.InitClient(buffer[0]);
            // Неизвестный номер пакета от клиента пропускаем (в оригинале исключение в потоке сети)
            if (packet == null) return;
            ReadPacket readPacket = new ReadPacket();
            readPacket.SetBuffer(buffer);
            _ReceivePacket(socketSide, readPacket.Receive(packet));""", rel, 1)
save(rel, t)

rel = r"Network\SocketSide.cs"
t = load(rel)
t = rep(t, """                // Сигнализируем, что waitHandler в сигнальном состоянии
                waitHandler!.Set();""", """                // Сигнализируем, что waitHandler в сигнальном состоянии; до запуска потока отправки
                // пакет ждёт в очереди
                waitHandler?.Set();""", rel, 1)
save(rel, t)
print("nullable_fixes ok")
