using Mvk.Platform.Input;
using Silk.NET.Input;

namespace Mvk.Platform.Tests
{
    public class InputActionMapTests
    {
        [Fact]
        public void PressHoldRelease()
        {
            var map = new InputActionMap();
            InputAction jump = map.Register("Jump", InputBinding.Of(Key.Space));

            map.Feed(InputBinding.Of(Key.Space), true);
            Assert.True(map.IsDown(jump));
            Assert.True(map.WasPressed(jump));

            map.EndFrame();
            Assert.True(map.IsDown(jump));
            Assert.False(map.WasPressed(jump));

            map.Feed(InputBinding.Of(Key.Space), false);
            Assert.False(map.IsDown(jump));
            Assert.True(map.WasReleased(jump));
        }

        [Fact]
        public void AutoRepeatIsNotSecondPress()
        {
            var map = new InputActionMap();
            InputAction jump = map.Register("Jump", InputBinding.Of(Key.Space));
            map.Feed(InputBinding.Of(Key.Space), true);
            map.EndFrame();
            map.Feed(InputBinding.Of(Key.Space), true);
            Assert.False(map.WasPressed(jump));
            map.Feed(InputBinding.Of(Key.Space), false);
            Assert.False(map.IsDown(jump));
        }

        [Fact]
        public void TwoBindingsHoldUntilBothReleased()
        {
            var map = new InputActionMap();
            InputAction attack = map.Register("Attack",
                InputBinding.Of(MouseButton.Left), InputBinding.Of(Key.F));
            map.Feed(InputBinding.Of(MouseButton.Left), true);
            map.Feed(InputBinding.Of(Key.F), true);
            map.Feed(InputBinding.Of(MouseButton.Left), false);
            Assert.True(map.IsDown(attack));
            Assert.False(map.WasReleased(attack));
            map.Feed(InputBinding.Of(Key.F), false);
            Assert.False(map.IsDown(attack));
            Assert.True(map.WasReleased(attack));
        }

        [Fact]
        public void KeyAndMouseCodesDoNotCollide()
        {
            var map = new InputActionMap();
            // Key и MouseButton это разные перечисления, числа могут совпасть
            InputAction a = map.Register("A", new InputBinding(InputSource.Key, 1));
            InputAction b = map.Register("B", new InputBinding(InputSource.MouseButton, 1));
            map.Feed(new InputBinding(InputSource.MouseButton, 1), true);
            Assert.False(map.IsDown(a));
            Assert.True(map.IsDown(b));
        }

        [Fact]
        public void RebindAndReleaseAll()
        {
            var map = new InputActionMap();
            InputAction jump = map.Register("Jump", InputBinding.Of(Key.Space));
            map.Unbind(jump);
            map.Bind(jump, InputBinding.Of(Key.J));
            map.Feed(InputBinding.Of(Key.Space), true);
            Assert.False(map.IsDown(jump));
            map.Feed(InputBinding.Of(Key.J), true);
            Assert.True(map.IsDown(jump));
            map.ReleaseAll();
            Assert.False(map.IsDown(jump));
            Assert.True(map.WasReleased(jump));
        }
    }
}
