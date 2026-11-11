using System;
using System.IO;
using StbImageSharp;

namespace Mvk.Engine.Util
{
    /// <summary>
    /// Загрузка картинок в BufferedImage.
    /// В оригинале через System.Drawing (только Windows), здесь через StbImageSharp (чистый C#, любые ОС).
    /// Формат буфера тот же, что давал GDI+ Format32bppArgb: построчно сверху вниз, байты B, G, R, A.
    /// </summary>
    public static class BufferedFileImage
    {
        /// <summary>
        /// Конвертировать картинку в структуру BufferedImage
        /// </summary>
        public static BufferedImage FileToBufferedImage(string fileName)
            => FileToBufferedImage(File.ReadAllBytes(fileName));

        /// <summary>
        /// Конвертировать картинку в структуру BufferedImage
        /// </summary>
        public static BufferedImage FileToBufferedImage(byte[] bufferPng)
        {
            ImageResult image = ImageResult.FromMemory(bufferPng, ColorComponents.RedGreenBlueAlpha);
            byte[] data = image.Data;
            // RGBA -> BGRA, как в GDI+
            for (int i = 0; i < data.Length; i += 4)
            {
                (data[i], data[i + 2]) = (data[i + 2], data[i]);
            }
            return new BufferedImage(image.Width, image.Height, data);
        }

        /// <summary>
        /// Оставить половину сверху
        /// </summary>
        public static BufferedImage LeaveHalfOnTop(BufferedImage buffered)
        {
            int count = buffered.Buffer.Length / 2;
            byte[] buffer = new byte[count];
            Buffer.BlockCopy(buffered.Buffer, 0, buffer, 0, count);
            return new BufferedImage(buffered.Width, buffered.Height / 2, buffer);
        }

        /// <summary>
        /// Оставить половину снизу
        /// </summary>
        public static BufferedImage LeaveHalfOnBottom(BufferedImage buffered)
        {
            int count = buffered.Buffer.Length / 2;
            byte[] buffer = new byte[count];
            Buffer.BlockCopy(buffered.Buffer, count, buffer, 0, count);
            return new BufferedImage(buffered.Width, buffered.Height / 2, buffer);
        }
    }
}
