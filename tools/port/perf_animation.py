"""Производительность: анимация костей без аллокаций в кадре (docs/improvements.md R18).
В оригинале AnimationClip._GetMixedAdjacentFrames через BoneAnimationFrame.Lerp создавал новый объект
на каждую кость, канал (позиция, вращение) и клип в каждом кадре: около 850 байт на курицу за кадр.
Теперь смешивание пишет X, Y, Z в out-параметры; формула и порядок операций те же."""
from portlib import load, save, rep

rel = r"Entity\Animation\AnimationClip.cs"
t = load(rel)
t = rep(t, """                    // Получаем среднее значение позиции кости в текущий момент времени
                    BoneAnimationFrame position
                        = _GetMixedAdjacentFrames(boneAnimationChannel.PositionFrames);

                    // Получаем среднее значение ориентации кости в текущий момент времени
                    BoneAnimationFrame rotation
                        = _GetMixedAdjacentFrames(boneAnimationChannel.OrientationFrames);

                    CurrentPoseBones[i].SetPositionRotation(
                        position.X, position.Y, position.Z,
                        rotation.X, rotation.Y, rotation.Z
                        );""", """                    // Получаем среднее значение позиции кости в текущий момент времени
                    _GetMixedAdjacentFrames(boneAnimationChannel.PositionFrames,
                        out float positionX, out float positionY, out float positionZ);

                    // Получаем среднее значение ориентации кости в текущий момент времени
                    _GetMixedAdjacentFrames(boneAnimationChannel.OrientationFrames,
                        out float rotationX, out float rotationY, out float rotationZ);

                    CurrentPoseBones[i].SetPositionRotation(
                        positionX, positionY, positionZ,
                        rotationX, rotationY, rotationZ
                        );""", rel, 1)

i = t.index("        private BoneAnimationFrame _GetMixedAdjacentFrames(BoneAnimationFrame[] positionFrames)")
j = t.index("            return positionFrames[positionFrames.Length - 1];\n        }", i) + len("            return positionFrames[positionFrames.Length - 1];\n        }")
t = t[:i] + """        private void _GetMixedAdjacentFrames(BoneAnimationFrame[] positionFrames, out float x, out float y, out float z)
        {
            BoneAnimationFrame frame;
            if (positionFrames.Length == 0)
            {
                frame = _boneAnimationFrameNull;
            }
            else if (positionFrames.Length == 1)
            {
                frame = positionFrames[0];
            }
            else
            {
                // Ищем ближайшие кадры
                frame = positionFrames[positionFrames.Length - 1];
                for (int i = 0; i < positionFrames.Length; i++)
                {
                    BoneAnimationFrame to = positionFrames[i];
                    if (to.Time == _currentTime)
                    {
                        frame = to;
                        break;
                    }
                    if (to.Time > _currentTime)
                    {
                        if (i > 0)
                        {
                            // BoneAnimationFrame.Lerp без нового объекта, коэффициент в диапазоне 0 .. 1
                            BoneAnimationFrame from = positionFrames[i - 1];
                            float timeIndex = (_currentTime - from.Time) / (to.Time - from.Time);
                            x = from.X + (to.X - from.X) * timeIndex;
                            y = from.Y + (to.Y - from.Y) * timeIndex;
                            z = from.Z + (to.Z - from.Z) * timeIndex;
                            return;
                        }
                        frame = positionFrames[0]; // ??
                        break;
                    }
                }
            }
            x = frame.X;
            y = frame.Y;
            z = frame.Z;
        }""" + t[j:]
save(rel, t)
print("perf_animation ok")
