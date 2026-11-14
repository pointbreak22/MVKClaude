using System.Reflection;
using Mvk.Core.Maths;
using Mvk.Engine;
using Mvk.Engine.Actions;
using Mvk.Engine.Audio;
using Mvk.Engine.Games;
using Mvk.Engine.Item;
using Mvk.Engine.Renderer.Font;
using Mvk.Engine.Renderer.Mesh;
using Mvk.Engine.World;
using Mvk.Engine.World.Block;
using Mvk.Game.Client.Gui.Screens;
using Mvk.Game.Client.Renderer;
using Mvk.Game.Entity;
using Mvk.Game.Entity.Particle;
using Mvk.Game.Games;
using Mvk.Game.Item;
using Mvk.Game.Util;
using Mvk.Game.World;
using Mvk.Game.World.Block;
using Mvk.Game.World.BlockEntity;
using Mvk.Rendering;

namespace Mvk.Game.Client
{
    /// <summary>
    /// Окно Малювек 2: перенос WindowMvk оригинала на WindowMain порта.
    /// Отличия от оригинала (не меняют игру): окно ОС создаёт хост платформы; клавиши чата и инвентаря,
    /// которые в оригинале обрабатывал GameModClientMvk через окно, обрабатываются здесь после игры
    /// в том же порядке.
    /// </summary>
    public class WindowMvk : WindowMain
    {
        /// <summary>Объект отрисовки Малювек</summary>
        private RenderMvk? _renderMvk;
        /// <summary>Текстуры неба острова</summary>
        private SkyTexturesMvk? _skyTextures;

        /// <summary>Запуск экранов Малювек</summary>
        public LaunchScreenMvk LScreenMvk { get; private set; } = null!;

        #region Initialized

        public WindowMvk(IWindowHost host, IGraphicsDevice device) : base(host, device) { }

        protected override void Initialized()
        {
            Version = "Test Малювек2 by SuperAnt ver. " + Assembly.GetExecutingAssembly().GetName().Version;
            LScreen = LScreenMvk = new LaunchScreenMvk(this);
            // Загружаем опции
            OptionsLoad();
        }

        protected override void OptionsLoad() => new OptionsFileClient().Load();
        public override void OptionsSave() => new OptionsFileMvk().Save();

        #endregion

        /// <summary>Объект отрисовки Малювек</summary>
        public RenderMvk GetRender() => _renderMvk!;

        #region Debug

        private string _textDebug = "";
        private bool _isTextDebug;
        private MeshGuiColor? _meshTextDebug;

        protected override void DrawDebug()
        {
            if (Ready && _meshTextDebug != null && Render.FontMain != null)
            {
                FontBase font = Render.FontMain;
                if (_isTextDebug)
                {
                    _isTextDebug = false;
                    font.Clear();
                    font.SetFontFX(EnumFontFX.Shadow).SetColor(new Vector3(.9f, .9f, .9f));
                    font.RenderText(10 * Gi.Si, 10 * Gi.Si, _textDebug);
                    font.RenderFX();
                    font.Reload(_meshTextDebug);
                    font.Clear();
                }
                // Прорисовка отладки
                Render.ShaderBindGuiColor();
                font.BindTexture();
                _meshTextDebug.Draw();
            }
        }

        protected override void OnTick()
        {
            base.OnTick();
            // Отладка на экране
            if (Ce.IsDebugDraw)
            {
                _textDebug = debug.ToText();
                _isTextDebug = true;
            }
        }

        #endregion

        #region OnKey

        public override void OnKeyDown(Keys keys)
        {
            // Клавиши мода в игре без открытого экрана (GameModClientMvk.OnKeyDown оригинала)
            bool inGame = Screen == null && Game != null;
            base.OnKeyDown(keys);
            if (!inGame || Game == null) return;
            switch (keys)
            {
                // Окно чата Клавиша "T" или "~"
                case Keys.T: case Keys.Oemtilde: LScreenMvk.Chat(); break;
                // скрин инвентаря
                case Keys.E: LScreenMvk.Inventory(); break;
                // скрин хранилища
                case Keys.R: LScreenMvk.StorageDebug(); break;
                // скрин креативного инвентаря
                case Keys.C:
                    if (Game.Player.CreativeMode) LScreenMvk.CreativeInventory(); break;
                // Скрин карты биомов (отладка)
                case Keys.F6: DebugMvkClient.ScreenFileBiomeArea(Game); break;
            }
        }

        #endregion

        #region WindowOverride

        protected override void RenderInitialized()
        {
            Render = _renderMvk = new RenderMvk(Context);
            _renderMvk.InitializeFirst();
            _skyTextures = new SkyTexturesMvk(Context.Device);
            _meshTextDebug = new MeshGuiColor(Render);
            // Цвет белый, фон загрузчика
            ClearColor(1, 1, 1, 1);
        }

        /// <summary>
        /// Небо острова, HUD Малювек и рендер игрока-владельца в хост новой игры
        /// </summary>
        public override void SetupHost(GameClientHost host) => MvkClient.SetupHost(host, _skyTextures!);

        #endregion

        #region Game

        protected override AllWorlds _CreateAllWorlds() => new AllWorldsMvk();

        protected override void _InitializationBlocks()
        {
            // Задать количество спрайтов в длину и ширину и размер спрайта
            Ce.SetSpriteAtlasSize(64, 16);
            base._InitializationBlocks();
            BlocksRegMvk.Initialization();
        }

        protected override void _InitializationItems()
        {
            // Так-как у нас спрайты в 2 раза больше блока (32 вместо 16), то фигуру надо указать в увеличенном от блока.
            // А размер спрайта (SizeSprite) не менять, оставить 16, он в атласе занимает 2*2=4 спрайта.
            ItemsReg.SizeShape = 32;
            base._InitializationItems();
            ItemsRegMvk.Initialization();
        }

        protected override void _InitializationEntities()
        {
            base._InitializationEntities();
            EntitiesRegMvk.Initialization();
        }

        protected override void _InitializationBlocksEntity()
        {
            base._InitializationBlocksEntity();
            BlocksEntityRegMvk.Initialization();
        }

        protected override void _InitializationEntitiesFX()
        {
            base._InitializationEntitiesFX();
            EntitiesFXRegMvk.Initialization();
        }

        protected override GameModClient _CreateGameModClient() => new GameModClientMvk();
        public override GameModServer CreateGameModServer() => new GameModServerMvk();

        #endregion

        #region Sound

        public override void SoundClick(float volume) => Audio.PlaySound(AudioReg.Click, 0, 0, 0, volume, 1);

        #endregion

        public override void Dispose()
        {
            base.Dispose();
            _meshTextDebug?.Dispose();
            _skyTextures?.Dispose();
        }
    }
}
