using Mvk.Platform.Input;
using Silk.NET.Input;

namespace Mvk.Platform.Tests
{
    /// <summary>
    /// Раскладка кнопок мыши в мире: по умолчанию правая ломает, левая ставит; пресет оригинала наоборот
    /// </summary>
    public class WorldMouseLayoutTests
    {
        [Fact]
        public void SwappedRightBreaksLeftUses()
        {
            var actions = new WorldMouseActions(new InputActionMap(), WorldMouseLayout.Swapped);
            Assert.Equal(0, actions.ActionOf(MouseButton.Right));
            Assert.Equal(1, actions.ActionOf(MouseButton.Left));
            Assert.Equal(-1, actions.ActionOf(MouseButton.Middle));
        }

        [Fact]
        public void OriginalLeftBreaksRightUses()
        {
            var actions = new WorldMouseActions(new InputActionMap(), WorldMouseLayout.Original);
            Assert.Equal(0, actions.ActionOf(MouseButton.Left));
            Assert.Equal(1, actions.ActionOf(MouseButton.Right));
        }

        [Fact]
        public void LayoutCanBeSwitchedBack()
        {
            var map = new InputActionMap();
            var actions = new WorldMouseActions(map, WorldMouseLayout.Swapped);
            actions.SetLayout(WorldMouseLayout.Original);
            Assert.Equal(0, actions.ActionOf(MouseButton.Left));
            Assert.Equal(1, actions.ActionOf(MouseButton.Right));
            // Удержание действия по физической кнопке
            map.Feed(InputBinding.Of(MouseButton.Left), true);
            Assert.True(map.IsDown(actions.Break));
        }
    }
}
