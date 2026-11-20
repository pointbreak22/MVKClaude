"""[ПОВЕДЕНИЕ] Подбор в пустую руку. В выживании клик рукой по лежащему предмету поднимает его этой рукой.
Подбор в пустую руку (пользователь: «по умолчанию подбор на правую руку»): поднятый предмет ложится
в правую руку, если она пуста и в карманах нет стака такого предмета (иначе — как в оригинале, в карманы и рюкзак)."""
from portlib import load, save, rep

rel = r"Entity\Inventory\InventoryPlayer.cs"
t = load(rel)
t = rep(t, """            ItemStack stackSlot = itemStack?.Copy()!;

            // Сразу в карманы пробуем""", """            ItemStack stackSlot = itemStack?.Copy()!;

            // [ПОВЕДЕНИЕ] Пустая рука (правая или та, что подняла) получает предмет, если такого нет в карманах
            int hand = GetCurrentIndex();
            if (itemStack != null && _pocketCount > 0 && (HandLeft || _currentIndex < LimitPocket) && _items[hand] == null
                && !_InPockets(stackSlot))
            {
                SetInventorySlotContents(hand, stackSlot);
                return true;
            }

            // Сразу в карманы пробуем""", rel, 1)
t = rep(t, """        /// <summary>
        /// Поменять стаки двух слотов местами с рассылкой
        /// </summary>""", """        /// <summary>
        /// Есть ли в карманах стак такого предмета (туда подбор докладывает, как в оригинале)
        /// </summary>
        private bool _InPockets(ItemStack stack)
        {
            for (int i = 0; i < LimitPocket && i < _pocketCount; i++)
            {
                if (_items[i] != null && _items[i].Item == stack.Item) return true;
            }
            return false;
        }

        /// <summary>
        /// Поменять стаки двух слотов местами с рассылкой
        /// </summary>""", rel, 1)
save(rel, t)

# Клиент: в выживании клик рукой по лежащему предмету — поднять его этой рукой (в оригинале «взаимодействие» ПКМ)
rel = r"Entity\Player\HandManager.cs"
t = load(rel)
t = rep(t, """                _pauseAction = 8;
                return;
            }
            _Action(true);""", """                _pauseAction = 8;
                return;
            }
            if (moving.IsEntity() && moving.Entity is not EntityLiving)
            {
                // [ПОВЕДЕНИЕ] Поднять лежащий предмет этой рукой
                _game.TrancivePacket(new PacketC03UseEntity(moving.Entity!.Id, PacketC03UseEntity.EnumAction.Interact).Hand(_left));
                _pauseAction = 4;
                return;
            }
            _Action(true);""", rel, 1)
save(rel, t)
print("hands_pickup ok")
