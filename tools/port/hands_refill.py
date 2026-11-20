"""[ПОВЕДЕНИЕ] Автопополнение руки (docs/controls-survival-spec.md, идеи рук п. 3): на сервере, когда предмет в руке
кончился от расхода (блок поставлен, брошен, съеден), такой же стак из карманов или рюкзака сам встаёт в эту руку."""
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
            if (_playerServer != null && before != null && _items[index] == null) _RefillHand(index, before);
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
