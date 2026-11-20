"""[ПОВЕДЕНИЕ] Этап 8.2: добыча «часть за удар» на сервере (docs/controls-survival-spec.md).

В выживании (две руки) клиент больше не копит разрушение блока: каждый удар рукой или инструментом идёт серверу
пакетом C07 Hit с рукой. Сервер проверяет досягаемость и то, может ли предмет добывать блок (ItemBase.DigAcceleration,
та же логика, что OnAction инструментов на клиенте), копит удары в общем счёте блока (World.BlockHits: обе руки
и другие игроки складываются), пишет стадию трещин в байт разрушения, ломает блок и изнашивает предмет руки.
Спад трещин без ударов — прежний тик мира. Звук и частицы удара клиент играет у себя сразу.
Творческий режим и прежняя схема без изменений."""
from portlib import load, save, rep, add_using

# Пакет: удар по блоку
rel = r"Network\Packets\Client\PacketC07PlayerDigging.cs"
t = load(rel)
t = rep(t, """        public PacketC07PlayerDigging(BlockPos blockPos)
        {""", """        /// <summary>
        /// Удар по блоку (две руки, добычу считает сервер)
        /// </summary>
        public static PacketC07PlayerDigging Hit(BlockPos blockPos)
        {
            PacketC07PlayerDigging packet = new PacketC07PlayerDigging(blockPos);
            packet.Digging = EnumDigging.Hit;
            return packet;
        }

        public PacketC07PlayerDigging(BlockPos blockPos)
        {""", rel, 1)
t = rep(t, """            ProcessDestroy = 2
""", """            ProcessDestroy = 2,
            /// <summary>
            /// Удар по блоку: прогресс, разрушение и износ считает сервер
            /// </summary>
            Hit = 3
""", rel, 1)
save(rel, t)

# Предмет: может ли добывать блок и с каким ускорением (сервер)
rel = r"Item\ItemBase.cs"
t = load(rel)
t = rep(t, """        /// <summary>
        /// Получить урон для атаки предметом который в руке
        /// </summary>""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Ускорение добычи блока предметом (для сервера): ударов на разрушение = твёрдость × ускорение;
        /// 0 — предмет этот блок не добывает. Повторяет выбор OnAction клиента: без своего инструмента только блоки,
        /// которые ломаются рукой
        /// </summary>
        public virtual float DigAcceleration(BlockBase block, PlayerBase player)
            => player.CanDestroyedBlock(block) ? 1 : 0;

        /// <summary>
        /// Получить урон для атаки предметом который в руке
        /// </summary>""", rel, 1)
save(rel, t)

# Мир: счёт ударов
rel = r"World\WorldServer.cs"
t = load(rel)
t = rep(t, """    public class WorldServer : WorldBase
    {
""", """    public class WorldServer : WorldBase
    {
        /// <summary>
        /// [ПОВЕДЕНИЕ] Счёт ударов по блокам (добыча «часть за удар»)
        /// </summary>
        public readonly BlockHits BlockHits = new BlockHits();

""", rel, 1)
save(rel, t)

# Сервер: удар по блоку
rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """            else if (packet.Digging == PacketC07PlayerDigging.EnumDigging.ProcessDestroy)""", """            else if (packet.Digging == PacketC07PlayerDigging.EnumDigging.Hit)
            {
                _HitBlock(packet.GetBlockPos());
            }
            else if (packet.Digging == PacketC07PlayerDigging.EnumDigging.ProcessDestroy)""", rel, 1)
t = rep(t, """        private bool _IsReachable(BlockPos blockPos)""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Удар по блоку предметом руки действия: часть прочности, при полном счёте блок ломается
        /// с выпадением; предмет изнашивается за каждый удар
        /// </summary>
        private void _HitBlock(BlockPos blockPos)
        {
            if (IsSpectator() || !_IsReachable(blockPos)) return;
            WorldServer worldServer = GetWorldServer();
            BlockState blockState = worldServer.GetBlockState(blockPos);
            if (blockState.IsAir()) return;
            BlockBase block = blockState.GetBlock();
            BlockPos pos = block.ShiftPosition(blockPos, blockState);
            if (!pos.Equals(blockPos))
            {
                blockState = worldServer.GetBlockState(pos);
                block = blockState.GetBlock();
            }
            ItemStack stack = Inventory.GetCurrentItem();
            float acceleration = stack != null ? stack.Item.DigAcceleration(block, this)
                : (CanDestroyedBlock(block) ? 1 : 0);
            if (acceleration <= 0) return;
            float need = (int)(block.Hardness * acceleration);
            if (block.Hardness <= 0 || CreativeMode || worldServer.BlockHits.Hit(worldServer, pos, need, 1))
            {
                if (worldServer.SetBreakBlock(pos, this) && IsDropBreakingBlock())
                {
                    block.DropBlockAsItem(worldServer, this, pos, blockState);
                }
            }
            if (stack != null) ItemWear.DamageHeld(this, 1);
        }

        private bool _IsReachable(BlockPos blockPos)""", rel, 1)
save(rel, t)

# Клиент: в выживании удар вместо своего счёта разрушения
rel = r"Entity\Player\HandManager.cs"
t = load(rel)
t = rep(t, """        private void _BlockDigging(BlockPos blockPos, float power = 1)
        {""", """        private void _BlockDigging(BlockPos blockPos, float power = 1)
        {
            if (_TwoHands())
            {
                _BlockHit(blockPos);
                return;
            }""", rel, 1)
t = rep(t, """        /// <summary>
        /// Разрушили выбранный блок
        /// </summary>""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Удар по блоку (две руки): прогресс, разрушение и износ считает сервер, звук и частицы сразу
        /// </summary>
        private void _BlockHit(BlockPos blockPos)
        {
            BlockBase block = _game.World.GetBlockState(blockPos).GetBlock();
            _game.TrancivePacket(PacketC07PlayerDigging.Hit(blockPos).Hand(_left));
            _game.World.PlaySound(block.Material.SampleBreak(_game.World.Rnd),
                blockPos.ToVector3Center(), .5f, .9f + _game.World.Rnd.NextFloat() * .2f);
            _game.World.ParticleDiggingBlock(block, _player.MovingObject.RayHit, 5, true);
        }

        /// <summary>
        /// Разрушили выбранный блок
        /// </summary>""", rel, 1)
save(rel, t)
print("mining_server ok")
