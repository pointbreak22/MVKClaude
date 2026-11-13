using System;
using System.IO;
using GoldenGen;
using Mvk.Engine;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.Animation;
using Mvk.Engine.Entity.Model;

namespace Mvk.Game.Tests
{
    /// <summary>
    /// Позы костей всех клипов анимации всех сущностей Малювек совпадают с кодом оригинала.
    /// Эталонный хэш снят до переноса смешивания кадров без аллокаций (improvements R18):
    /// любое изменение формулы или порядка операций меняет хэш.
    /// </summary>
    [Collection("Registries")]
    public class AnimationPoseTests
    {
        /// <summary>Хэш поз, снятый на коде перенесённом из оригинала без изменений</summary>
        private const ulong ExpectedHash = 7125550931140094123UL;

        [Fact]
        public void PosesSameAsOriginal()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            // Таблицы сущностей заполняет запуск сервера (EntitiesReg.Correct), как в golden-тесте острова
            IslandGolden.Generate(1, 89, 0);

            ulong hash = 14695981039346656037UL;
            int clips = 0;
            for (ushort id = 0; id < Ce.Entities.Count; id++)
            {
                if (!(Ce.Entities.GetResourcesEntity(id) is ResourcesEntity resources)) continue;
                foreach (ModelAnimationClip model in resources.ModelAnimationClips)
                {
                    AnimationClip clip = new AnimationClip(resources, model);
                    clip.Reset(1);
                    clips++;
                    // Неровный шаг кадра, чтобы попадать между ключевыми кадрами и на них
                    for (int step = 0; step < 400; step++)
                    {
                        clip.IncreaseCurrentTime(step % 3 == 0 ? 16f : 13.37f);
                        clip.GenBoneCurrentPose();
                        foreach (BonePose pose in clip.CurrentPoseBones)
                        {
                            hash = _Mix(hash, pose.PositionX);
                            hash = _Mix(hash, pose.PositionY);
                            hash = _Mix(hash, pose.PositionZ);
                            hash = _Mix(hash, pose.RotationX);
                            hash = _Mix(hash, pose.RotationY);
                            hash = _Mix(hash, pose.RotationZ);
                        }
                    }
                }
            }
            Assert.True(clips > 0, "Нет клипов анимации");
            Assert.Equal(ExpectedHash, hash);
        }

        private static ulong _Mix(ulong hash, float value)
            => (hash ^ (uint)BitConverter.SingleToInt32Bits(value)) * 1099511628211UL;
    }
}
