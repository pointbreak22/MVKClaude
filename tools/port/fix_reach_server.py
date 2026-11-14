"""[ПОВЕДЕНИЕ] Исправление бага оригинала (docs/known-issues.md п. 1, серверная часть):
сервер выполнял установку и активацию блока по координате из пакета клиента без проверки расстояния.
Теперь блок дальше досягаемости (луч клиента 8 блоков от глаз, запас на движение между тактами) отклоняется:
установка откатывается клиенту, как при неудаче, активация пропускается."""
from portlib import load, save, rep

rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """                    BlockPos blockPos = packet.GetBlockPos();
                    // Установить блок который в руке
                    if (!itemStack.Item.OnItemOnBlockPlacement(itemStack, this, blockPos, packet.Side,
                        packet.Facing, packet.Replaceable, RotationYaw))""", """                    BlockPos blockPos = packet.GetBlockPos();
                    // Установить блок который в руке, если блок в досягаемости
                    if (!_IsReachable(blockPos)
                        || !itemStack.Item.OnItemOnBlockPlacement(itemStack, this, blockPos, packet.Side,
                        packet.Facing, packet.Replaceable, RotationYaw))""", rel, 1)
t = rep(t, """                // Активация выбранного блока
                BlockPos blockPos = packet.GetBlockPos();
                _worldServer.GetBlockState(blockPos).GetBlock()
                    .OnBlockActivated(this, blockPos, packet.Side, packet.Facing);
            }
        }""", """                // Активация выбранного блока, если блок в досягаемости
                BlockPos blockPos = packet.GetBlockPos();
                if (_IsReachable(blockPos))
                {
                    _worldServer.GetBlockState(blockPos).GetBlock()
                        .OnBlockActivated(this, blockPos, packet.Side, packet.Facing);
                }
            }
        }

        /// <summary>
        /// Наибольшее расстояние от глаз игрока до центра блока, с которым сервер разрешает действие:
        /// луч клиента 8 блоков, плюс полдиагонали блока и запас на движение игрока между тактами
        /// </summary>
        private const float _ReachServer = 12f;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Блок в досягаемости игрока (known-issues п. 1): в оригинале сервер не проверял
        /// расстояние до блока из пакета клиента
        /// </summary>
        private bool _IsReachable(BlockPos blockPos)
        {
            float x = blockPos.X + .5f - PosX;
            float y = blockPos.Y + .5f - (PosY + GetEyeHeight());
            float z = blockPos.Z + .5f - PosZ;
            return x * x + y * y + z * z <= _ReachServer * _ReachServer;
        }""", rel, 1)
save(rel, t)
print("fix_reach_server ok")
