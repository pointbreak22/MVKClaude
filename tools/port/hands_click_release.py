"""[ПОВЕДЕНИЕ] Удержание не бьёт (замечание пользователя 21.11): у предметов с удержанием-действием (лук, оружие,
бросок, еда) клик выполняется при отпускании кнопки, если удержание не началось: короткое нажатие — удар или выстрел,
удержание — натяжение, перезарядка или замах без удара. Замах броска без анимации удара (взмах — при броске).
Наследник видит, что рука держит натяжение (_HoldingCharge), чтобы не играть анимацию удара."""
from portlib import load, save, rep

rel = r"Entity\Player\HandManager.cs"
t = load(rel)
t = rep(t, """                    else
                    {
                        _clickPending = true;
                    }
                }
                else if (_holding)""", """                    else if (_use == HandUse.Charge || _use == HandUse.Throw)
                    {
                        // Клик при отпускании: удержание не должно бить
                        _clickOnRelease = true;
                    }
                    else
                    {
                        _clickPending = true;
                    }
                }
                else if (_holding)""", rel, 1)
t = rep(t, """                else if (++_handTicks >= HoldTicks && !_clickPending)
                {
                    _holding = true;
                    _HoldBegin();
                }
            }
            else if (_handPrev)
            {
                if (_holding) _HoldEnd();""", """                else if (++_handTicks >= HoldTicks && !_clickPending)
                {
                    _clickOnRelease = false;
                    _holding = true;
                    _HoldBegin();
                }
            }
            else if (_handPrev)
            {
                if (_holding) _HoldEnd();
                else if (_clickOnRelease) _clickPending = true;
                _clickOnRelease = false;""", rel, 1)
t = rep(t, """        /// <summary>
        /// Кнопка руки нажата или отпущена (выживание)
        /// </summary>""", """        /// <summary>
        /// [ПОВЕДЕНИЕ] Клик выполнится при отпускании (предметы с удержанием-действием)
        /// </summary>
        private bool _clickOnRelease;

        /// <summary>
        /// [ПОВЕДЕНИЕ] Рука держит натяжение, перезарядку или замах (анимацию удара не играть)
        /// </summary>
        protected bool _HoldingCharge => _holding && (_use == HandUse.Charge || _use == HandUse.Throw);

        /// <summary>
        /// Кнопка руки нажата или отпущена (выживание)
        /// </summary>""", rel, 1)
i = t.index("            else if (_use == HandUse.Throw)\n            {\n                _flagAbort = false;")
anim = "\n                _player.Render.SetAnimationCodeAdd(_AnimationCode(\"ActionAxe\"));"
j = t.index(anim, i)
t = t[:j] + t[j + len(anim):]
save(rel, t)
print("hands_click_release ok")
