"""[ПОВЕДЕНИЕ] Исправление багов оригинала (docs/known-issues.md п. 2 и 3), вариант А по решению пользователя:
согласованные шкалы ведра и блока жидкости (Item/LiquidScale.cs).
- п. 3: уровень блока 1..15 и ведро 1..7 обрезались, а не пересчитывались: источник давал полное ведро, но
  полное ведро выливало текущую жидкость уровня 7 (полублок), а текущая жидкость уровня 8..14 давала полное ведро.
  Теперь источник ⇄ полное ведро, текущие уровни пропорционально (из потока не больше 6, источник не создать).
- п. 2: при наборе в неполное ведро излишек обрезался, а блок удалялся целиком. Теперь ведро берёт только то,
  что влезает, остаток остаётся в блоке (и в доп. жидкости другого блока) с тактом жидкости на сервере.
Ведро из творческого инвентаря (Met 0) считается полным, как в оригинале (выливало источник)."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

# Пустое ведро: из блока уровня L — пропорциональное количество, блок целиком уходит (всё влезает)
rel = r"Item\List\ItemBucket.cs"
t = load(rel, GAME)
t = rep(t, """            if (water == 0 || water > 7) water = 7;
""", """            // Уровень блока в количество ведра: источник — полное ведро, текущая жидкость пропорционально;
            // в пустое ведро влезает всё, блок жидкости убирается целиком
            water = LiquidScale.Scoop(0, LiquidScale.LevelOfMet(water)).Bucket;
""", rel, 1)
save(rel, t, GAME)

rel = r"Item\List\ItemLiquid.cs"
t = load(rel, GAME)
# Подсказка: литров вдвое больше количества, ведро из творческого инвентаря полное
t = rep(t, """            => string.Format(_toolTip, stack.Met * 2, stack.ToStringDamaged());""",
"""            => string.Format(_toolTip, LiquidScale.AmountOfBucket(stack.Met) * 2, stack.ToStringDamaged());""", rel, 1)
# Вылив: полное ведро — источник, неполное — уровень пропорционально
t = rep(t, """            // 0 - Значит максимум
            // 7 - Минимум
            int water = stack.Met;
            return new BlockState(Block.IndexBlock | water << 12);""", """            int level = LiquidScale.LevelOfBucket(LiquidScale.AmountOfBucket(stack.Met));
            return new BlockState(Block.IndexBlock | LiquidScale.MetOfLevel(level) << 12);""", rel, 1)
# Уровень жидкости в блоке 1..15 вместо обрезки до 7
t = rep(t, """                water = blockState.Met & 0xF;
                if (water == 0 || water > 7) water = 7;
                idBlockLiquid = block.IndexBlock;""", """                water = LiquidScale.LevelOfMet(blockState.Met & 0xF);
                idBlockLiquid = block.IndexBlock;""", rel, 1)
t = rep(t, """                water = block.GetAddLiquidMet(blockState.Met);
                if (water == 0 || water > 7) water = 7;
                idBlockLiquid = block.GetAddLiquid(blockState.Met).IndexBlock;""", """                water = LiquidScale.LevelOfMet(block.GetAddLiquidMet(blockState.Met));
                idBlockLiquid = block.GetAddLiquid(blockState.Met).IndexBlock;""", rel, 1)
# Набор в неполное ведро: только то, что влезает, остаток в блоке
t = rep(t, """                        if (stack.Met < 7)
                        {
                            water += stack.Met;
                            if (water > 7) water = 7;
                            stack.SetMet((byte)water);
                            player.Inventory.SetCurrentItem(stack);
                            world.SetLiquidBlockToAir(blockPos);
                            return true;
                        }""", """                        int have = LiquidScale.AmountOfBucket(stack.Met);
                        if (have < LiquidScale.BucketFull)
                        {
                            (int bucket, int rest) = LiquidScale.Scoop(have, water);
                            stack.SetMet((byte)bucket);
                            player.Inventory.SetCurrentItem(stack);
                            if (rest == 0)
                            {
                                world.SetLiquidBlockToAir(blockPos);
                            }
                            else
                            {
                                // Остаток жидкости в блоке (или доп. жидкостью в другом блоке), дальше течёт как обычно
                                world.SetBlockState(blockPos, new BlockState(Block.IndexBlock
                                    | LiquidScale.MetOfLevel(rest) << 12), BlockFlag.cNotify);
                                if (world is WorldServer worldServer && Block is BlockLiquid blockLiquid)
                                {
                                    blockLiquid.ScheduleTick(worldServer, blockPos);
                                }
                            }
                            return true;
                        }""", rel, 1)
for u in ["using System;", "using Mvk.Engine.World.Block.List;"]:
    if u not in t:
        t = u + "\n" + t.lstrip("﻿")
save(rel, t, GAME)
print("game_seam8_fix_liquid ok")
