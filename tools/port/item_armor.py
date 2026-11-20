"""[ПОВЕДЕНИЕ] Броня: у одежды (ItemCloth) параметр Protection из json — доля, на которую надетая вещь снижает урон
по своей части тела (0 — не защищает, в оригинале защиты не было). Снижение считает сервер (DamageLiving, шов
game_seam8_projectile_armor.py). В старом проекте автора Mvk у брони были только слоты, без защиты."""
from portlib import load, save, rep

rel = r"Item\List\ItemCloth.cs"
t = load(rel)
t = rep(t, """        public string PutOnBody { get; protected set; } = null!;""", """        public string PutOnBody { get; protected set; } = null!;
        /// <summary>
        /// Защита: доля, на которую вещь снижает урон по своей части тела (0..0.9), json Protection
        /// </summary>
        public float Protection { get; protected set; }""", rel, 1)
t = rep(t, """                    PutOnBody = state.GetString(Cti.PutOnBody);""", """                    PutOnBody = state.GetString(Cti.PutOnBody);
                    if (state.IsKey("Protection")) Protection = Math.Clamp(state.GetFloat("Protection"), 0, .9f);""", rel, 1)
save(rel, t)
print("item_armor ok")
