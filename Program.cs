using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace CrystalShrine
{
    internal static class Program
    {
        static void Main()
        {
            var gws = GameWindowSettings.Default;
            var nws = new NativeWindowSettings()
            {
                Size = new Vector2i(1600, 900),
                Title = "Crystal Shrine",
                WindowBorder = WindowBorder.Resizable
            };

            using (var game = new Game(gws, nws))
            {
                game.Run();
            }
        }
    }
}