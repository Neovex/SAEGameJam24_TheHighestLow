using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using SFML.System;
using SFML.Window;
using SFML.Graphics;
using SFML.Audio;
using BlackCoat;
using BlackCoat.InputMapping;
using BlackCoat.AssetHandling;

namespace LowHigh
{
	static class GameManager
	{
		private static readonly Vector2f _ASPECT_RATIO = new Vector2f(9f / 16f, 16f / 9f);
		public const string TITLE = "The Highest Low";
		public static int MAX_VOL = 25;

		private static Core _Core;
		private static Device _Device;
		private static MusicLoader _MusicLoader;
		private static Dictionary<string, (Music, Music)> _Songs;
		private static Music _CurrentSong;

		public static SfxManager Sfx { get; private set; }

		public static Vector2f FullHD;

		public static View View { get; private set; }
		public static Input Input { get; private set; }

		private static string[] _LevelTypes = ["PixelArt", "Stylized", "SemiRealistic"];
		public static string LevelType { get; private set; } = _LevelTypes[0];
		private static Dictionary<string, string[]> _LevelNames;
		public static string LevelName { get; private set; }
		public static bool Teleport { get; set; }
		
		public static Jump Jump => LevelType switch
		{
			"PixelArt" => new Jump() { ApexGravity = 30 },
			"Stylized" => new Jump() { JumpForce = -1600 },
			"SemiRealistic" => new Jump() { JumpForce = -3000, TerminalVelocity = 3500 },
			_ => default
		};
		public static Walk Walk => LevelType switch
		{
			"PixelArt" => new Walk() { MaxSpeed = 500 },
			"Stylized" => new Walk() { MaxSpeed = 700, Acceleration = 8 },
			"SemiRealistic" => new Walk() { Acceleration = 9, MaxSpeed = 1600 },
			_ => default
		};
		public static Vector2f ViewSize => LevelType switch
		{
			"PixelArt" => new Vector2f(960, 540),
			"Stylized" => new Vector2f(2880, 1620),
			"SemiRealistic" => new Vector2f(6435, 3620),
			_ => default
		};


		internal static void Initialize(Core core, Device device)
		{
			_Core = core;
			_Device = device;
			Input = new Input(_Core, true, true, true);
			Input.MouseButtonPressed += b
				=> Teleport = b == Mouse.Button.Left && _Core.Debug && _Core.SceneManager.CurrentSceneName != "MainMenuScene";

			PreloadAssets();

			// View
			FullHD = new Vector2f(1920, 1080);
			View = new View(FullHD / 2, FullHD);
			_Core.DeviceResized += HandleDeviceResized;
			HandleDeviceResized(_Core.DeviceSize);

			// Cheats
			_Core.ConsoleCommand += HandleCustomConsoleCommands;
		}

		private static bool HandleCustomConsoleCommands(string arg)
		{
			if (arg == "win")
			{
				LoadNextLevel();
				return true;
			}
			else if (arg == "tel")
			{
				return Teleport = true;
			}
			else if (arg == "mute")
			{
				MAX_VOL = 0;
				if(_CurrentSong != null) _CurrentSong.Volume = MAX_VOL;
				return true;
			}
			else if (arg == "unmute")
			{
				MAX_VOL = 25;
				if (_CurrentSong != null) _CurrentSong.Volume = MAX_VOL;
				return true;
			}
			return false;
		}

		private static void PreloadAssets()
		{
			// Global Textures
			var assetRoot = "Assets\\";

			// GameConfig
			_LevelNames = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(assetRoot + "Levels.json"));
			Log.Debug(_LevelNames.Sum(kvp => kvp.Value.Length), "levels loaded");
			LevelName = _LevelNames[LevelType][0];

			// Global Sound
			var loader = new SfxLoader(assetRoot + "Sfx");
			Sfx = new SfxManager(loader, () => MAX_VOL + 20);
			Sfx.LoadFromDirectory(parallelSounds: 4);

			// Global Music
			_MusicLoader = new MusicLoader();
			_Songs = _LevelTypes.ToDictionary(
				t => t,
				t =>
				{
					_MusicLoader.RootFolder = $"Assets\\{t}";
					return (_MusicLoader.Load(t + "Bounce"),
							_MusicLoader.Load(t + "BounceFlange"));
				});
			foreach (var song in _Songs.SelectMany(kvp => new Music[] { kvp.Value.Item1, kvp.Value.Item2 }))
			{
				song.Loop = true;
				song.Volume = 0;
				song.Play();
			}
		}

		public static void CrossfadeToNextSong(bool flang)
		{
			const float fadeTime = 3.6f;
			var set = _Songs[LevelType];

			var newSong = flang ? set.Item2 : set.Item1;
			if (_CurrentSong == newSong) return;

			if (_CurrentSong != null)
			{
				var old = _CurrentSong;
				_Core.AnimationManager.Run(old.Volume, 0, fadeTime, v => old.Volume = v);
			}

			_CurrentSong = newSong;
			_Core.AnimationManager.Run(0, MAX_VOL, fadeTime, v => newSong.Volume = v);
		}

		public static SimpleInputMap<GameAction> CreateInput()
		{
			var map = new SimpleInputMap<GameAction>(Input);
			map.AddKeyboardMapping(Keyboard.Key.A, GameAction.Left);
			map.AddKeyboardMapping(Keyboard.Key.D, GameAction.Right);
			map.AddKeyboardMapping(Keyboard.Key.W, GameAction.Jump);
			map.AddKeyboardMapping(Keyboard.Key.Space, GameAction.Jump);
			map.AddKeyboardMapping(Keyboard.Key.F, GameAction.Act);

			map.AddJoystickButtonMapping(0, GameAction.Act);
			map.AddJoystickButtonMapping(1, GameAction.Jump);
			map.AddJoystickButtonMapping(2, GameAction.Act);
			map.AddJoystickMovementMapping(Joystick.Axis.PovX, 10f, GameAction.Right);
			map.AddJoystickMovementMapping(Joystick.Axis.PovX, -10f, GameAction.Left);
			map.AddJoystickMovementMapping(Joystick.Axis.X, 10f, GameAction.Right);
			map.AddJoystickMovementMapping(Joystick.Axis.X, -10f, GameAction.Left);
			return map;
		}

		public static void LoadNextLevel(bool init = false) // needs moar smart
		{
			var levels = _LevelNames[LevelType];
			var levelIndex = init ? 0 : Array.IndexOf(levels, LevelName) + 1;

			if (levelIndex == levels.Length)
			{
				var typeIndex = Array.IndexOf(_LevelTypes, LevelType) + 1;
				if (typeIndex == _LevelTypes.Length)
				{
					_Core.SceneManager.ChangeScene(new MainMenuScene(_Core));
					CrossfadeToNextSong(false);
				}
				else
				{
					LevelType = _LevelTypes[typeIndex];
					LoadNextLevel(true); // weired stuff happens when uncommented.. weired
				}
			}
			else
			{
				LevelName = levels[levelIndex];
				_Core.SceneManager.ChangeScene(new GameScene(_Core));
			}
		}


		public static Vector2f MapToPixel(Vector2f pos, View view) => _Device.MapCoordsToPixel(pos, view).ToVector2f();
		public static Vector2f MapToCoords(Vector2f pos, View view) => _Device.MapPixelToCoords(pos.ToVector2i(), view);
		public static void HandleDeviceResized(Vector2f size)
		{
			var corrected = size.MultiplyBy(_ASPECT_RATIO);
			var port = new FloatRect(0, 0, 1, 1);
			if (size.X > corrected.Y)
			{
				port.Width = corrected.Y / size.X;
				port.Left = (1 - port.Width) / 2;
			}
			else if (size.Y > corrected.X)
			{
				port.Height = corrected.X / size.Y;
				port.Top = (1 - port.Height) / 2;
			}
			View.Viewport = port;
		}


		public static IEnumerable<IntRect> CalcFrames(Texture source, int width, int height)
		{
			var x = (int)source.Size.X / width;
			var y = (int)source.Size.Y / height;
			for (int i = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					yield return new IntRect(j * x, i * y, x, y);
				}
			}
		}

		public static void PlayRandom(string name, int max)
		{
			try
			{
				Sfx.Play(name + _Core.Random.Next(1, max + 1));
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}
	}
}