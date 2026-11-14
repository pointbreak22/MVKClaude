"""[ПОВЕДЕНИЕ] Исправление бага оригинала (docs/known-issues.md п. 1, ведро):
пустое ведро на сервере черпало из любого блока: координата приходит из пакета, а проверка, что блок жидкий
или содержит доп. жидкость, была только у клиента (ItemBucket.OnSecond). Блок без жидкости брал
GetAddLiquid (жидкость по умолчанию), ведро наполнялось, а блок уходил в SetLiquidBlockToAir.
Теперь без жидкости действие не выполняется (сервер откатывает блок клиенту, как при неудаче)."""
import os
from portlib import load, save, rep, ROOT
GAME = os.path.join(ROOT, r"src\Mvk.Game")

rel = r"Item\List\ItemBucket.cs"
t = load(rel, GAME)
t = rep(t, """            BlockBase block = blockState.GetBlock();
            int water;
            int idBlockLiquid;
""", """            BlockBase block = blockState.GetBlock();
            int water;
            int idBlockLiquid;

            // Черпать можно только жидкость или блок с доп. жидкостью (координата могла прийти из пакета)
            if (!block.Material.Liquid && !block.IsAddLiquid(blockState.Met)) return false;
""", rel, 1)
save(rel, t, GAME)
print("game_seam8_fix_bucket ok")
