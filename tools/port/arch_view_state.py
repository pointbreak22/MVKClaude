"""Технический п. 1: состояние вида клиента (размер окна, Si, матрицы) из статического Gi в объект ViewState.
В Gi остаются константы, цвета, ресурсы трещин разрушения и текущий блок построения сеток ([ThreadStatic]).
Клиентская игра берёт вид у хоста: GameBase.View => Host.View (окно у Engine.Client, свой объект у безголового хоста)."""
import re
from portlib import load, save, rep, fail

VIEW = ("Width|Height|Si|WindowsChatWidthMessage|WindowsChatHeightMessage|IsDrawOrto|ZoomDrawOrto|DebugOrtoNext|"
        "UpdateSizeInterface|UpOrtho|Ortho|MatrixViewEye|MatrixViewDepthMap|MatrixView|ViewLightDir|EntityBrightness|"
        "BlockBrightness")
VIEW_RE = re.compile(r"\bGi\.(" + VIEW + r")\b")


def view(rel, accessor, expected):
    t = load(rel)
    t, n = VIEW_RE.subn(accessor + r".\1", t)
    if n != expected: fail(f"{rel}: Gi view {n}, expected {expected}")
    save(rel, t)


# Gi: убрать состояние вида
rel = "Gi.cs"
t = load(rel)
i = t.index("    /// <summary>\n    /// Ширина окна")
j = t.index("    /// <summary>\n    /// Цвет текста для чата")
t = t[:i] + t[j:]
i = t.index("    /// <summary>\n    /// Массив матрицы для проецирования")
j = t.index("    /// <summary>\n    /// Получить сетку разрушения блока")
t = t[:i] + t[j:]
t = rep(t, """/// GraphicalInterface
/// Класс статический данных для GUI""", """/// GraphicalInterface
/// Константы и общие ресурсы GUI. Состояние вида (размер окна, Si, матрицы) в объекте Mvk.Engine.ViewState""", rel, 1)
if re.search(r"\b(Width|MatrixView|ActiveTexture)\b", t): fail(rel + ": view left")
save(rel, t)

# Клиентская игра: вид от хоста
rel = r"Games\GameBase.cs"
t = load(rel)
t = rep(t, """        public float DeltaTime => Host.DeltaTime;""", """        public float DeltaTime => Host.DeltaTime;
        /// <summary>
        /// Состояние вида клиента (размер окна, матрицы), в оригинале статический Gi
        /// </summary>
        public ViewState View => Host.View;""", rel, 1)
save(rel, t)
view(rel, "View", 2)
view(r"Entity\Player\PlayerClientOwner.cs", "_game.View", 16)
print("arch_view_state ok")
