"""[ПОВЕДЕНИЕ] Полёт снарядов (замечание пользователя 21.11, по образцу Майнкрафта): у предмета в json множитель
скорости броска FlightSpeed и гравитации FlightGravity (стрела и пуля летят быстро и почти прямо), признак
FlightAlign — в полёте модель предмета повёрнута по вектору скорости (рендер клиента)."""
from portlib import load, save, rep

rel = r"Item\ItemBase.cs"
t = load(rel)
t = rep(t, """                        else if (json.IsKey("ThrowCharge")) ThrowCharge = Math.Max(1, json.GetInt());""",
        """                        else if (json.IsKey("ThrowCharge")) ThrowCharge = Math.Max(1, json.GetInt());
                        else if (json.IsKey("FlightSpeed")) FlightSpeed = json.GetFloat();
                        else if (json.IsKey("FlightGravity")) FlightGravity = json.GetFloat();
                        else if (json.IsKey("FlightAlign")) FlightAlign = json.GetBool();""", rel, 1)
t = rep(t, """        public int ThrowCharge { get; protected set; } = 20;""", """        public int ThrowCharge { get; protected set; } = 20;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Множитель скорости снаряда (json FlightSpeed)
        /// </summary>
        public float FlightSpeed { get; protected set; } = 1;
        /// <summary>
        /// [ПОВЕДЕНИЕ] Множитель гравитации снаряда (json FlightGravity)
        /// </summary>
        public float FlightGravity { get; protected set; } = 1;
        /// <summary>
        /// [ПОВЕДЕНИЕ] В полёте модель повёрнута по вектору скорости (json FlightAlign)
        /// </summary>
        public bool FlightAlign { get; protected set; }""", rel, 1)
save(rel, t)

rel = r"Entity\List\EntityProjectile.cs"
t = load(rel)
t = t.replace("""            float speed = 1.4f * power;""", """            float speed = 1.4f * power * stack.Item.FlightSpeed;
            projectile.Physics.SetGravity(stack.Item.FlightGravity);""")
assert t.count("stack.Item.FlightSpeed") == 2
save(rel, t)
print("projectile_flight ok")

t = load(rel)
t = rep(t, """            IsFlying = false;
            EntityLiving? thrower""", """            IsFlying = false;
            // Дальше обычный предмет: обычная гравитация
            Physics.SetGravity(1);
            EntityLiving? thrower""", rel, 1)
save(rel, t)
print("projectile_flight impact ok")
