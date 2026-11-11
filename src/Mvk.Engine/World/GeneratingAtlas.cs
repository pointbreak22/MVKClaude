using System;
using System.Collections.Generic;
using System.IO;
using Mvk.Core.Util;
using Mvk.Engine.Util;

namespace Mvk.Engine.World
{
    /// <summary>
    /// Создание атласа блоков и предметов
    /// </summary>
    public class GeneratingAtlas
    {
        /// <summary>
        /// Высота
        /// </summary>
        private int _height;
        /// <summary>
        /// Шаг строк ширина * 4
        /// </summary>
        private int _stride;
        /// <summary>
        /// Графический буфер
        /// </summary>
        private byte[] _buffer;
        /// <summary>
        /// Маска для понимания где уже имеется спрайт
        /// </summary>
        private bool[,] _mask; 

        /// <summary>
        /// Справочник текстур, название текстуры, индекс расположения текстуры 
        /// в атласе и количество кадров
        /// </summary>
        private readonly Dictionary<string, SpriteData> _textures = new Dictionary<string, SpriteData>();
        
        /// <summary>
        /// Количество спрайтов на стороне текстурного атласа
        /// </summary>
        private int _textureAtlasBlockCount = 64;

        /// <summary>
        /// Размер стороны атласа в пикселях
        /// </summary>
        public int Size => _height;
        /// <summary>
        /// Пиксели атласа (B, G, R, A построчно) для загрузки в видеокарту клиентом
        /// </summary>
        public byte[] Buffer => _buffer;
        /// <summary>
        /// Маска занятых ячеек атласа (для отладки)
        /// </summary>
        public bool[,] Mask => _mask;

        /// <summary>
        /// Создать картинку для атласа
        /// </summary>
        public void CreateImage()
        {
            _textureAtlasBlockCount = Ce.TextureAtlasBlockCount;
            _height = _textureAtlasBlockCount * Ce.TextureSpriteBlockSize;
            _stride = _height * 4;
            _buffer = new byte[_stride * _height];
            _mask = new bool[_textureAtlasBlockCount, _textureAtlasBlockCount];
        }



        /// <summary>
        /// Очистить буфер и справочник
        /// </summary>
        public void Clear()
        {
            _textures.Clear();
            _buffer = new byte[0];
        }

        /// <summary>
        /// Получить буфер спрайт из буфера атласа
        /// Пиксели идут друг за другом построчно, сверху вниз и слева направо, формата RGBA
        /// </summary>
        public byte[] GetSprite(SpriteData sprite)
        {
            int size = Ce.TextureSpriteBlockSize; // 16
            int sizeRGBA = size * 4; // 64
            int width = sprite.CountWidth * size;
            int height = sprite.CountHeight * size;
            byte[] buffer = new byte[width * height * 4];

            int sizeRGBAw = sizeRGBA * sprite.CountWidth;

            int index2 = sprite.Index / _textureAtlasBlockCount * _stride * size
                + sprite.Index % _textureAtlasBlockCount * sizeRGBA;

            for (int y = 0; y < height; y++)
            {
                Buffer.BlockCopy(_buffer, index2 + (_stride * y),
                    buffer, y * sizeRGBAw, sizeRGBAw);
            }

            return buffer;
        }

        public SpriteData AddSprite(string name)
        {
            if (name != "")
            {
                bool animation = name[0] == '@';
                if (animation)
                {
                    name = name.Substring(1);
                }

                if (_textures.ContainsKey(name))
                {
                    // Такая текстура сгенерированна ранее, возвращаем просто ID
                    return _textures[name];
                }

                string fileName = Options.PathTextures + name + ".png";
                if (File.Exists(fileName))
                {
                    //_profiler.StartSection(name);
                    int size = Ce.TextureSpriteBlockSize; // 16
                    int sizeRGBA = size * 4; // 64
                    BufferedImage buffered = BufferedFileImage.FileToBufferedImage(fileName);
                    int index = -1;
                    int w = 1;
                    if (buffered.Width == size && buffered.Height >= size)
                    {
                        index = buffered.Height > size
                            ? _GenIndexAnim(buffered.Height / size)
                            : _GenIndexOne();
                    }
                    else if (buffered.Width > size && buffered.Height > size)
                    {
                        w = buffered.Width / size;
                        if (buffered.Width == w * size)
                        {
                            index = _GenIndexAnim2d(buffered.Height / size, w);
                        }
                        else
                        {
                            throw new Exception(Sr.GetString(Sr.SpriteErrorWidthIsIncorrect, fileName, size));
                        }
                    }
                    if (index != -1)
                    {
                        SpriteData res = new SpriteData(index, w, 
                            _BufferCopy(index, w, buffered, name), animation);
                        _textures.Add(name, res);
                        return res;
                    }
                    //_profiler.EndSectionLog();
                }
            }
            return new SpriteData(-1);
        }

        private int _BufferCopy(int index, int w, BufferedImage buffered, string name)
        {
            int size = Ce.TextureSpriteBlockSize; // 16
            int sizeRGBA = size * 4; // 64
            int sizeRGBAw = sizeRGBA * w;
            int index2 = index / _textureAtlasBlockCount * _stride * size
                + index % _textureAtlasBlockCount * sizeRGBA;
            for (int y = 0; y < buffered.Height; y++)
            {
                Buffer.BlockCopy(buffered.Buffer, y * sizeRGBAw,
                    _buffer, index2 + (_stride * y), sizeRGBAw);
            }
            return buffered.Height / size;
        }

        /// <summary>
        /// Получить индекс для одного спрайта
        /// </summary>
        private int _GenIndexOne()
        {
             //for (int y = 0; y < _textureAtlasBlockCount; y++)
            for (int x = 0; x < _textureAtlasBlockCount; x++)
            {
                //for (int x = 0; x < _textureAtlasBlockCount; x++)
                for (int y = 0; y < _textureAtlasBlockCount; y++)
                {
                    if (!_mask[y, x])
                    {
                        _mask[y, x] = true;
                        return y * _textureAtlasBlockCount + x;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Получить индекс для анимационного спрайта, задаём количество спрайтов
        /// </summary>
        private int _GenIndexAnim(int lenght)
        {
            int count = _textureAtlasBlockCount - lenght + 1;
            for (int x = 0; x < _textureAtlasBlockCount; x++)
            {
                for (int y = 0; y < count; y++)
                {
                    if (!_mask[y, x])
                    {
                        if (_CheckHeight(x, y, lenght))
                        {
                            count = y + lenght;
                            for (int y2 = y; y2 < count; y2++)
                            {
                                _mask[y2, x] = true;
                            }
                            return y * _textureAtlasBlockCount + x;
                        }
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Получить индекс для анимационного спрайта, задаём количество спрайтов
        /// </summary>
        private int _GenIndexAnim2d(int lenght, int width)
        {
            //  lenght--;
            int count = _textureAtlasBlockCount - lenght + 1;
            for (int x = 0; x < _textureAtlasBlockCount; x++)
            {
                for (int y = 0; y < count; y++)
                {
                    if (!_mask[y, x])
                    {
                        if (_CheckHeight2d(x, y, lenght, width))
                        {
                            count = y + lenght;
                            int countW = x + width;
                            for (int y2 = y; y2 < count; y2++)
                            {
                                for (int x2 = x; x2 < countW; x2++)
                                {
                                    _mask[y2, x2] = true;
                                }
                            }
                            return y * _textureAtlasBlockCount + x;
                        }
                    }
                }
            }
            return -1;
        }

        private bool _CheckHeight(int x, int y, int lenght)
        {
            int count = y + lenght;
            for (int y2 = y; y2 < count; y2++)
            {
                if (_mask[y2, x]) return false;
            }
            return true;
        }


        private bool _CheckHeight2d(int x, int y, int lenght, int width)
        {
            int count = y + lenght;
            int countW = x + width;
            for (int y2 = y; y2 < count; y2++)
            {
                for (int x2 = x; x2 < countW; x2++)
                {
                    if (_mask[y2, x2]) return false;
                }
            }
            return true;
        }

    }
}
