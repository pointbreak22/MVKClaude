"""[ПОВЕДЕНИЕ] Свет предмета в руке (факел, json HandLight) и испуг мобов (идеи рук): сервер каждый такт переносит
невидимый светящийся блок в клетку головы игрока (World/HandLight.cs, блок задаёт мод); мобы, которые и так
убегают от игрока (EntityAIRunFromFear), от игрока с огнём в руке убегают всегда."""
from portlib import load, save, rep

rel = r"Item\ItemBase.cs"
t = load(rel)
t = rep(t, """                        else if (json.IsKey("ThrowMin")) ThrowMin = json.GetFloat();""",
        """                        else if (json.IsKey("ThrowMin")) ThrowMin = json.GetFloat();
                        else if (json.IsKey("HandLight")) HandLight = json.GetInt();""", rel, 1)
t = rep(t, """        public bool Throwable => ThrowMax > 0;""", """        public bool Throwable => ThrowMax > 0;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Свет предмета в руке (json HandLight, 0 — не светит)
        /// </summary>
        public int HandLight { get; protected set; } = 0;""", rel, 1)
save(rel, t)

rel = r"Entity\Player\PlayerServer.cs"
t = load(rel)
t = rep(t, """            // [ПОВЕДЕНИЕ] Фитиль в руке
            _UpdateFuse();""", """            // [ПОВЕДЕНИЕ] Фитиль в руке
            _UpdateFuse();
            // [ПОВЕДЕНИЕ] Свет предмета в руке
            HandLight.Update(this, ref _handLightPos);""", rel, 1)
t = rep(t, """        public uint FuseEnd;""", """        public uint FuseEnd;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Клетка света предмета в руке (HandLight) или null
        /// </summary>
        private BlockPos? _handLightPos;""", rel, 1)
save(rel, t)

rel = r"Entity\AI\Move\EntityAIRunFromFear.cs"
t = load(rel)
t = rep(t, """                float probability = _probability;""", """                float probability = _probability;
                // [ПОВЕДЕНИЕ] Огонь в руке игрока пугает всегда
                if (_entity.Tracker.ClosestPlayer.Inventory.GetCurrentRightItem()?.Item.HandLight > 0
                    || _entity.Tracker.ClosestPlayer.Inventory.GetCurrentLeftItem()?.Item.HandLight > 0)
                {
                    probability = 1;
                }""", rel, 1)
save(rel, t)
rel = r"World\Block\BlockBase.cs"
t = load(rel)
t = rep(t, """        public void InitAir()
        {""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Блок как воздух со своим псевдонимом (свет факела в руке)
        /// </summary>
        public void InitAirAlias(string alias)
        {
            InitAir();
            Alias = alias;
        }

        public void InitAir()
        {""", rel, 1)
save(rel, t)
print("hand_light ok")
