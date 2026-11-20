"""[ПОВЕДЕНИЕ] Удар руки по требованию (добивающий удар после «креста» мечами, game_new/HandManagerMvkHands.cs):
HandManager.Strike выполняет клик-действие руки сразу, без отката, с рукой действия инвентаря этой руки."""
from portlib import load, save, rep

rel = r"Entity\Player\HandManager.cs"
t = load(rel)
t = rep(t, """        /// <summary>
        /// Нажатие руки с защитой (щитом), до начала защиты
        /// </summary>""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Удар руки сразу, без отката (добивающий удар): действие предмета этой руки
        /// </summary>
        public void Strike()
        {
            if (_player.IsSpectator() || _HandBusy()) return;
            _player.Inventory.HandLeft = _left;
            try
            {
                _actTicks = ActOpenTicks;
                _pauseAction = 0;
                _Action(true);
            }
            finally
            {
                _player.Inventory.HandLeft = false;
            }
        }

        /// <summary>
        /// Нажатие руки с защитой (щитом), до начала защиты
        /// </summary>""", rel, 1)
save(rel, t)
print("hands_strike ok")
