using Mvk.Game.Util;

namespace Mvk.Game.Client
{
    /// <summary>
    /// Файл опций клиента Малювек: как OptionsFileMvk, но если папку Assets выбрал хост
    /// (<see cref="MvkClient.SetAssets"/>), строка PathAssets из options.ini пропускается
    /// </summary>
    public class OptionsFileClient : OptionsFileMvk
    {
        protected override bool _ReadLine(string key, string value)
        {
            if (key == "PathAssets" && MvkClient.AssetsByHost) return true;
            return base._ReadLine(key, value);
        }
    }
}
