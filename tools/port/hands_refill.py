"""[ПОВЕДЕНИЕ] Автопополнение руки (docs/controls-survival-spec.md, идеи рук п. 3): на сервере, когда предмет в руке
расходуется (блок поставлен, брошен, съеден), стак в руке доливается до максимума таким же предметом из карманов
и рюкзака; кончился — такой же стак сам встаёт в эту руку. Обе руки."""
from portlib import load, save, rep

rel = r"Entity\Inventory\InventoryPlayer.cs"
t = load(rel)
t = rep(t, """        /// <summary>
        /// Поменять стаки двух слотов местами с рассылкой
        /// </summary>""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Расход предмета руки; кончился — рука пополняется таким же стаком из инвентаря (сервер)
        /// </summary>
        public override void DecrCurrentItem(byte amount)
        {
            int index = GetCurrentIndex();
            ItemStack before = _items[index];
            base.DecrCurrentItem(amount);
            if (_playerServer == null || before == null) return;
            if (_items[index] == null) _RefillHand(index, before);
            else _TopUpHand(index);
        }

        /// <summary>
        /// [ПОВЕДЕНИЕ] Долить стак руки до максимума таким же предметом (тот же износ и состояние) из карманов и рюкзака
        /// </summary>
        private void _TopUpHand(int index)
        {
            ItemStack hand = _items[index];
            int max = hand.Item.MaxStackSize;
            if (hand.Amount >= max) return;
            int backpack = _pocketCount + _clothCount;
            int amount = hand.Amount;
            for (int i = 0; i < backpack + LimitBackpack && i < _allCount && amount < max; i++)
            {
                if (i == index || i == _currentIndex || (i >= LimitPocket && i < backpack)) continue;
                ItemStack stack = _items[i];
                if (stack == null || stack.Item != hand.Item || stack.ItemDamaged != hand.ItemDamaged || stack.Met != hand.Met) continue;
                int take = System.Math.Min(max - amount, stack.Amount);
                amount += take;
                SetInventorySlotContents(i, stack.Amount > take ? stack.Copy((byte)(stack.Amount - take)) : null!);
            }
            if (amount != hand.Amount) SetInventorySlotContents(index, hand.Copy((byte)amount));
        }

        /// <summary>
        /// Найти такой же предмет (карманы, затем рюкзак) и переложить его стак в слот руки
        /// </summary>
        private void _RefillHand(int index, ItemStack used)
        {
            int backpack = _pocketCount + _clothCount;
            for (int i = 0; i < backpack + LimitBackpack && i < _allCount; i++)
            {
                if (i == index || i == _currentIndex || (i >= LimitPocket && i < backpack)) continue;
                ItemStack stack = _items[i];
                if (stack != null && stack.Item == used.Item && stack.ItemDamaged == used.ItemDamaged)
                {
                    SetInventorySlotContents(i, null!);
                    SetInventorySlotContents(index, stack);
                    return;
                }
            }
        }

        /// <summary>
        /// Поменять стаки двух слотов местами с рассылкой
        /// </summary>""", rel, 1)
save(rel, t)
print("hands_refill ok")
