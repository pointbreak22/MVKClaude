#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mvk.Engine.Entity;
using Mvk.Engine.Entity.List;
using Mvk.Engine.Entity.Player;
using Mvk.Engine.Entity.Render;
using Mvk.Engine.Entity.Shape;
using Mvk.Engine.Games;
using Mvk.Engine.Item;
using Mvk.Engine.Renderer.Shaders;
using Mvk.Core.Util;
using Mvk.Engine.Util;
using Mvk.Rendering;

namespace Mvk.Engine.Renderer.World.Entity
{
    /// <summary>
    /// Объект рендера всех сущностей на RHI. Перенос EntitiesRenderer оригинала:
    /// массивы текстур сущностей, сетки всех типов сущностей и предметов, проходы кадра.
    /// Тени: проход в карту теней DrawDepthMap.
    /// </summary>
    public class EntitiesRenderer : IDisposable
    {
        /// <summary>
        /// Объект буфера кеш для слоёв
        /// </summary>
        public readonly VertexLayersBuffer Buffer = new VertexLayersBuffer();
        /// <summary>
        /// Шейдер сущностей
        /// </summary>
        public readonly ShadersEntity ShsEntity;
        /// <summary>
        /// Видим ли мы хитбокс сущности (F3+B)
        /// </summary>
        public bool IsHitBox = false;
        /// <summary>
        /// Объект для отладки хитбокса сущности, создаётся при первом показе
        /// </summary>
        private HitboxEntityRender? _hitbox;
        /// <summary>
        /// Общие объекты рендера
        /// </summary>
        public readonly RenderContext Context;

        /// <summary>
        /// Формы с большими текстурами, у которых глубина уже сдвинута на 65536 (метка большой текстуры
        /// для шейдера). В оригинале сдвиг делался один раз на процесс; здесь игра может запускаться
        /// в процессе несколько раз, а формы общие.
        /// </summary>
        private static readonly HashSet<ShapeBase> _shapesDepthBig = new HashSet<ShapeBase>();

        private readonly GameBase _game;
        private readonly WorldRenderer _worldRenderer;
        /// <summary>
        /// Метод чанков для прорисовки
        /// </summary>
        private readonly ArrayFast<ChunkRender> _arrayChunkRender;
        /// <summary>
        /// Количество сущностей которые попали в FrustumCulling чанка, т.е. видимых для прорисовки
        /// </summary>
        private int _countEntitiesFC;
        /// <summary>
        /// Количество предметов которые попали в FrustumCulling чанка, т.е. видимых для прорисовки
        /// </summary>
        private int _countItemsFC;
        /// <summary>
        /// Массив всех типов сущностей
        /// </summary>
        private EntityRender?[] _entityRender = Array.Empty<EntityRender?>();
        /// <summary>
        /// Массив всех типов предметов
        /// </summary>
        private EntityRender[] _itemRender = Array.Empty<EntityRender>();
        /// <summary>
        /// Массив всех типов предметов для Gui
        /// </summary>
        private ItemRender[] _itemGuiRender = Array.Empty<ItemRender>();

        /// <summary>
        /// Отрисовка интерфейса (в оригинале EntitiesRenderer.Render), для сущности в GUI
        /// </summary>
        public RenderMain Render => Gui!;

        /// <summary>
        /// Отрисовка интерфейса, для предметов в GUI (задаёт хост клиента)
        /// </summary>
        public RenderMain? Gui
        {
            get => ShsEntity.Gui;
            set => ShsEntity.Gui = value;
        }
        /// <summary>
        /// Массив маленьких текстур
        /// </summary>
        private ITexture? _textureSmall;
        /// <summary>
        /// Массив больших текстур
        /// </summary>
        private ITexture? _textureBig;

        public EntitiesRenderer(GameBase game, WorldRenderer worldRenderer, ArrayFast<ChunkRender> arrayChunkRender, string shaderEntityLow)
        {
            _game = game;
            _worldRenderer = worldRenderer;
            Context = worldRenderer.Context;
            _arrayChunkRender = arrayChunkRender;
            // Имя шейдера GUI сущностей задано движком (RenderMain оригинала)
            ShsEntity = new ShadersEntity(Context, shaderEntityLow, "EntityHigh", "EntityDepthMap", "EntityGui");
        }

        /// <summary>
        /// Текстуры и сетки созданы
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Объект игрока для клиента
        /// </summary>
        public PlayerClientOwner Player
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _game.Player;
        }

        /// <summary>
        /// Игра запущена, всё загружено из библиотек: текстуры сущностей и сетки всех типов
        /// </summary>
        public void GameStarting()
        {
            IGraphicsDevice device = Context.Device;
            // В оригинале CLAMP_TO_BORDER и NEAREST без мипмапов
            SamplerState sampler = new SamplerState(TextureFilter.Nearest, TextureFilter.Nearest,
                MipFilter.None, TextureWrap.ClampToBorder);
            var manager = EntitiesReg.TextureManager;
            // Без маленьких текстур в оригинале слот оставался пустым; RHI нужна текстура, берём 1x1
            _textureSmall = manager.DepthSmall != 0
                ? device.CreateTexture(TextureDescription.Texture2DArray(manager.WidthSmall, manager.HeightSmall, manager.DepthSmall, sampler))
                : device.CreateTexture(TextureDescription.Texture2DArray(1, 1, 1, sampler));
            _textureBig = device.CreateTexture(TextureDescription.Texture2DArray(
                manager.WidthBig, manager.HeightBig, manager.DepthBig, sampler));

            // Заносим все текстуры сущностей
            for (int i = 0; i < EntitiesReg.Shapes.Count; i++) _SetImageTexture2dArray(EntitiesReg.Shapes[i]);
            for (int i = 0; i < EntitiesReg.LayerShapes.Count; i++) _SetImageTexture2dArray(EntitiesReg.LayerShapes[i]);

            // Создаём объекты рендора всех типов сущностей
            int count = Ce.Entities.Count;
            _entityRender = new EntityRender?[count];
            for (ushort id = 0; id < count; id++)
            {
                if (Ce.Entities.GetResourcesEntity(id) is ResourcesEntity resourcesEntity)
                {
                    _entityRender[id] = new EntityRender(this, resourcesEntity.BufferMesh);
                }
            }
            // Создаём объекты рендора всех типов предметов.
            // Буфер предмета не очищается (в оригинале BufferClear): игра может запуститься в процессе ещё раз.
            count = Ce.Items.Count;
            _itemRender = new EntityRender[count];
            _itemGuiRender = new ItemRender[count];
            for (ushort id = 0; id < count; id++)
            {
                VertexEntityBuffer? buffer = Ce.Items.ItemObjects[id].Buffer.GetBuffer();
                if (buffer == null)
                {
                    throw new Exception(ClientStrings.GetString(ClientStrings.BufferItemIsMissing, Ce.Items.ItemAlias[id]));
                }
                _itemRender[id] = new EntityRender(this, buffer);
                _itemGuiRender[id] = new ItemRender(id, Gui!, ShsEntity, Ce.Items.ItemObjects[id].Buffer);
            }
            IsStarted = true;
        }

        /// <summary>
        /// Внести текстуры формы в массив текстур (ShapeBase.SetImageTexture2dArray оригинала)
        /// </summary>
        private void _SetImageTexture2dArray(ShapeBase shape)
        {
            bool adjusted = !shape.TextureSmall && _shapesDepthBig.Contains(shape);
            ITexture texture = shape.TextureSmall ? _textureSmall! : _textureBig!;
            for (int i = 0; i < shape.Textures.Length; i++)
            {
                BufferedImage image = shape.Textures[i];
                int layer = adjusted ? shape.DepthTextures[i] - 65536 : shape.DepthTextures[i];
                texture.SetSubData(image.Buffer, PixelFormat.Bgra8, 0, 0, image.Width, image.Height, layer);
                if (!shape.TextureSmall && !adjusted)
                {
                    // Если текстура большая, делаем корректировку по данным для будушего шейдора
                    shape.DepthTextures[i] += 65536;
                }
            }
            if (!shape.TextureSmall) _shapesDepthBig.Add(shape);
        }

        /// <summary>
        /// Начать проход сущностей: конвейер, текстуры и юниформы кадра
        /// </summary>
        private void _BindUniformBegin(CommandList commands, bool eye)
        {
            if (eye)
            {
                ShsEntity.BindUniformBeginEye(commands, _textureSmall!, _textureBig!,
                    _worldRenderer.Atlas.Sharpness, _worldRenderer.LightMap.Texture, _worldRenderer.Shadow.DepthTexture);
            }
            else
            {
                ShsEntity.BindUniformBegin(commands, _textureSmall!, _textureBig!,
                    _worldRenderer.Atlas.Sharpness, _worldRenderer.LightMap.Texture, _worldRenderer.Shadow.DepthTexture);
            }
        }

        /// <summary>
        /// Метод для прорисовки кадра
        /// </summary>
        /// <param name="timeIndex">коэффициент времени от прошлого TPS клиента в диапазоне 0 .. 1</param>
        public void Draw(CommandList commands, float timeIndex)
        {
            _countEntitiesFC = 0;
            _countItemsFC = 0;
            int count = _arrayChunkRender.Count;
            int countEntity;
            ChunkRender chunkRender;
            EntityBase entity;
            int playerId = _game.Player.Id;

            // Параметрв шейдоров
            _BindUniformBegin(commands, false);

            // Сущности
            for (int i = 0; i < count; i++)
            {
                chunkRender = _arrayChunkRender[i];
                countEntity = chunkRender.ListEntities.Count;
                for (int j = 0; j < countEntity; j++)
                {
                    entity = chunkRender.ListEntities.GetAt(j);
                    if (!(entity is EntityItem) && entity.Id != playerId)
                    {
                        entity.Render.Draw(timeIndex, _game.DeltaTimeFrame);
                        _countEntitiesFC++;
                    }
                }
            }

            // Предметы
            ShsEntity.BindUniformItems();
            for (int i = 0; i < count; i++)
            {
                chunkRender = _arrayChunkRender[i];
                countEntity = chunkRender.ListEntities.Count;
                for (int j = 0; j < countEntity; j++)
                {
                    entity = chunkRender.ListEntities.GetAt(j);
                    if (entity is EntityItem)
                    {
                        entity.Render.Draw(timeIndex, _game.DeltaTimeFrame);
                        _countItemsFC++;
                    }
                }
            }

            if (IsHitBox)
            {
                // Есть хит бокс сущности: пробегаемся заново по всем сущностям
                float x = _game.Player.PosFrameX;
                float y = _game.Player.PosFrameY;
                float z = _game.Player.PosFrameZ;
                _hitbox ??= new HitboxEntityRender(Render);
                for (int i = 0; i < count; i++)
                {
                    chunkRender = _arrayChunkRender[i];
                    countEntity = chunkRender.ListEntities.Count;
                    for (int j = 0; j < countEntity; j++)
                    {
                        entity = chunkRender.ListEntities.GetAt(j);
                        if (entity.Id != playerId)
                        {
                            Render.ShaderBindLine(Gi.MatrixView,
                                entity.GetPosFrameX(timeIndex) - x,
                                entity.GetPosFrameY(timeIndex) - y,
                                entity.GetPosFrameZ(timeIndex) - z, 1f);
                            _hitbox.Draw(timeIndex, entity);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Метод для прорисовки кадра теней: сущности и предметы ближних чанков (DrawDepthMap оригинала)
        /// </summary>
        public void DrawDepthMap(CommandList commands, float timeIndex)
        {
            int count = _arrayChunkRender.Count;
            if (count > ShadowMapping.CountChunkShadowMap) count = ShadowMapping.CountChunkShadowMap;
            int countEntity;
            ChunkRender chunkRender;
            EntityBase entity;

            // Параметры шейдоров
            ShsEntity.BindUniformBeginDepthMap(commands, _textureSmall!, _textureBig!, _worldRenderer.Atlas.Sharpness);
            // Сущности
            for (int i = 0; i < count; i++)
            {
                chunkRender = _arrayChunkRender[i];
                countEntity = chunkRender.ListEntities.Count;
                for (int j = 0; j < countEntity; j++)
                {
                    entity = chunkRender.ListEntities.GetAt(j);
                    if (!(entity is EntityItem))
                    {
                        entity.Render.Draw(timeIndex, _game.DeltaTimeFrame);
                    }
                }
            }

            // Предметы
            ShsEntity.BindUniformItems();
            for (int i = 0; i < count; i++)
            {
                chunkRender = _arrayChunkRender[i];
                countEntity = chunkRender.ListEntities.Count;
                for (int j = 0; j < countEntity; j++)
                {
                    entity = chunkRender.ListEntities.GetAt(j);
                    if (entity is EntityItem)
                    {
                        entity.Render.Draw(timeIndex, _game.DeltaTimeFrame);
                    }
                }
            }
        }

        /// <summary>
        /// Обновить матрицы сущностей видимых чанков для кадра
        /// </summary>
        /// <param name="timeIndex">коэффициент времени от прошлого TPS клиента в диапазоне 0 .. 1</param>
        public void UpdateMatrix(float timeIndex)
        {
            int count = _arrayChunkRender.Count;
            int countEntity;
            ChunkRender chunkRender;
            for (int i = 0; i < count; i++)
            {
                chunkRender = _arrayChunkRender[i];
                countEntity = chunkRender.ListEntities.Count;
                for (int j = 0; j < countEntity; j++)
                {
                    chunkRender.ListEntities.GetAt(j).Render.UpdateMatrix(timeIndex, _game.DeltaTimeFrame);
                }
            }
        }

        /// <summary>
        /// Получить объект рендера сущности по индексу
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityRender GetEntityRender(int indexEntity) => _entityRender[indexEntity]!;
        /// <summary>
        /// Получить объект рендера предмета по индексу
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityRender GetItemRender(int indexEntity) => _itemRender[indexEntity];
        /// <summary>
        /// Получить объект рендера предмета для Gui по индексу
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ItemRender GetItemGuiRender(int indexEntity) => _itemGuiRender[indexEntity];

        /// <summary>
        /// Метод для прорисовки основного игрока, когда камера не с глаз
        /// </summary>
        /// <param name="timeIndex">коэффициент времени от прошлого TPS клиента в диапазоне 0 .. 1</param>
        public void DrawOwner(CommandList commands, float timeIndex)
        {
            if (_game.Player.ViewCamera != EnumViewCamera.Eye)
            {
                _BindUniformBegin(commands, false);
                _game.Player.Render.Draw(timeIndex, _game.DeltaTimeFrame);
            }

            if (IsHitBox)
            {
                _hitbox ??= new HitboxEntityRender(Render);
                Render.ShaderBindLine(Gi.MatrixView, 0, 0, 0, 1f);
                _hitbox.Draw(timeIndex, _game.Player);
            }
        }

        /// <summary>
        /// Метод для прорисовки основного игрока с глаз (руки)
        /// </summary>
        /// <param name="timeIndex">коэффициент времени от прошлого TPS клиента в диапазоне 0 .. 1</param>
        public void DrawOwnerEye(CommandList commands, float timeIndex)
        {
            if (_game.Player.ViewCamera == EnumViewCamera.Eye)
            {
                // Перерасчёт матрицы игрока, без головы и с изменениями
                _game.Player.Render.UpdateMatrixOwnerEye(timeIndex);
                _BindUniformBegin(commands, true);
                _game.Player.Render.Draw(timeIndex, _game.DeltaTimeFrame);
            }
        }

        public void Dispose()
        {
            _hitbox?.Dispose();
            _textureSmall?.Dispose();
            _textureSmall = null;
            _textureBig?.Dispose();
            _textureBig = null;
            for (int i = 0; i < _entityRender.Length; i++) _entityRender[i]?.Dispose();
            for (int i = 0; i < _itemRender.Length; i++) _itemRender[i].Dispose();
            for (int i = 0; i < _itemGuiRender.Length; i++) _itemGuiRender[i].Dispose();
            ShsEntity.Dispose();
            IsStarted = false;
        }

        public override string ToString() => "El:" + _countEntitiesFC + " Ei:" + _countItemsFC;
    }
}
