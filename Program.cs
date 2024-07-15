using System;
using System.Drawing;
using BlackCoat;
using SFML.Window;

namespace LowHigh
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
#if !DEBUG
            var launcher = new Launcher()
            {
                BannerImage = Image.FromFile("Assets\\banner.png"),
                Text = GameManager.TITLE
            };
            var device = Device.Create(launcher, GameManager.TITLE);
            if (device == null) return;
#endif

#if DEBUG
            var desktop = VideoMode.DesktopMode;
            var vm = new VideoMode(desktop.Width/2, desktop.Height/2);
            var device = Device.Create(vm, GameManager.TITLE, Styles.Default, 0, false, 120);
#endif
            using (var core = new Core(device))
            {
#if DEBUG
                core.Debug = true;
#endif
                // Setup Game Manager
                GameManager.Initialize(core, device);
                core.SceneManager.ChangeScene(new MainMenuScene(core));
                core.Run();
            }
        }
    }
}