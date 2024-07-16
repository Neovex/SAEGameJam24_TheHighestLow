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
                Text = GameManager.TITLE,
                EffectVolume = GameManager.MAX_VOL,
                MusicVolume = GameManager.MAX_VOL
			};
            var device = Device.Create(launcher, GameManager.TITLE);
            if (device == null) return;
            GameManager.MAX_VOL = Math.Min(launcher.EffectVolume, launcher.MusicVolume);
#endif

#if DEBUG
            var desktop = VideoMode.DesktopMode;
            var vm = new VideoMode(desktop.Width/2, desktop.Height/2);
            var device = Device.Create(vm, GameManager.TITLE, Styles.Default, 0, false, 120, true);
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