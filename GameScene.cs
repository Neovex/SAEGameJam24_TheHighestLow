using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlackCoat;
using BlackCoat.Entities;
using BlackCoat.Entities.Animation;
using BlackCoat.Entities.Shapes;
using BlackCoat.InputMapping;
using SFML.Audio;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace LowHigh
{
	class GameScene : Scene
	{
		private SimpleInputMap<GameAction> _InputMap;
		private Player _Player;
		private MapObject[] _MapObjects;
		private bool _ShowDebugCollisions = false;
		private Graphic _Shroom;
		private Graphic _ExitPortal;
		private BlittingAnimation _ExitPortalAnimation;
		private Graphic _ParalaxBg;
		private Vector2f _MapSize;

		public View View { get => GameManager.View; }


		public GameScene(Core core) : base(core, GameManager.LevelName)
		{
		}


		protected override bool Load()
		{
			// Assets
			TextureLoader.RootFolder = "Assets\\" + GameManager.LevelType;

			// Audio
			GameManager.CrossfadeToNextSong(false);

			// Input
			_InputMap = GameManager.CreateInput();
			_InputMap.MappedOperationInvoked += HandleInput;

			// BG
            var bgTex = TextureLoader.Load("Background");
            Layer_Background.Add(_ParalaxBg = new Graphic(_Core, bgTex));

			// Player
			_Player = new Player(_Core, _InputMap, TextureLoader)
			{
				CollidesWith = CollidesWith
			};

			// Tile Map
			var tex = TextureLoader.Load("tileset");
			var mapData = new MapData();
			mapData.Load(_Core, $"{TextureLoader.RootFolder}\\{Name}.tmx");
			foreach (var layer in mapData.Layer)
			{
				var mapRenderer = new MapRenderer(_Core, mapData.MapSize, tex, mapData.TileSize);
				for (int i = 0; i < layer.Tiles.Length; i++)
				{
					mapRenderer.AddTile(i * 4, layer.Tiles[i].Position, layer.Tiles[i].Coordinates);
				}
				if(layer.Data == "root") // inject player inbetween tile layers
				{
					Layer_Background.Add(_Player);
				}
				Layer_Background.Add(mapRenderer);
			}
			// Collision Layer
			_MapObjects = mapData.MapObjects;
			if (_ShowDebugCollisions)
			{
				foreach (var collision in _MapObjects)
				{
					Layer_Game.Add(new Rectangle(_Core, collision.Shape.Size, Color.Cyan, Color.Blue)
					{
						Position = collision.Shape.Position,
						Alpha = 0.5f
					});
				}
			}
			_MapSize = mapData.MapSize.ToVector2f().MultiplyBy(mapData.TileSize.ToVector2f());
			var scale = new Vector2f(_MapSize.X / _ParalaxBg.Texture.Size.X, _MapSize.Y / _ParalaxBg.Texture.Size.Y);
			_ParalaxBg.Scale = new Vector2f(Math.Max(scale.X, scale.Y), Math.Max(scale.X, scale.Y)) * 2;

			// View
			Layer_Background.View = GameManager.View;
			GameManager.View.Size = GameManager.ViewSize;
			Layer_Game.View = GameManager.View;

			// Player Position
			_Player.Position = _MapObjects.First(o => o.Type == CollisionType.Start).Shape.Position;
			_Player.InitialPosition = _Player.Position;

			// Shroom
			tex = TextureLoader.Load("Mushroom");
			_Shroom = new BlittingAnimation(_Core, .150f, tex, GameManager.CalcFrames(tex, 7,3).ToArray())
			{
				Position = _MapObjects.First(o => o.Type == CollisionType.Shroom).Shape.Position
			};
			Layer_Game.Add(_Shroom);

			// Exit Portal
			_ExitPortal = new Graphic(_Core, TextureLoader.Load("TreePortal"))
			{
				Position = _MapObjects.First(o => o.Type == CollisionType.Portal).Shape.Position
			};
			Layer_Game.Add(_ExitPortal);
			tex = TextureLoader.Load("TreePortalAnim");
			_ExitPortalAnimation = new BlittingAnimation(_Core, 0.15f, tex, GameManager.CalcFrames(tex, 9, 1).ToArray())
			{
				Position = _ExitPortal.Position,
				Visible = false
			};
			Layer_Game.Add(_ExitPortalAnimation);



			GameManager.View.Center = _Player.Position;
			return true;
		}

		private bool CollidesWith(Vector2f pos, CollisionType type = CollisionType.Normal)
			=> _MapObjects.Any(c => c.Type == type && c.CollidesWith(pos));

		private void HandleInput(GameAction action, bool activate)
		{
			//?
		}

		protected override void Update(float deltaT)
		{
			GameManager.View.Center += (_Player.Position - GameManager.View.Center + new Vector2f()) * 2 * deltaT;
			if (_ExitPortalAnimation.Visible)
			{
				if (_ExitPortal.CollisionShape.CollidesWith(_Player.CollisionShape))
				{
					GameManager.LoadNextLevel();
				}
			}
			else if (_Shroom.CollisionShape.CollidesWith(_Player.CollisionShape))
			{
				Layer_Game.Remove(_Shroom);
				Layer_Game.Remove(_ExitPortal);
				_ExitPortalAnimation.Visible = true;
				// Play portal/pickup SFX here
				GameManager.Sfx.Play("Spin Whoosh");
				_Core.AnimationManager.Wait(3, () => GameManager.PlayRandom("Bonus ", 3));
				_Core.AnimationManager.Wait(5, () => GameManager.PlayRandom("Bonus ", 3));
				_Core.AnimationManager.Wait(7, () => GameManager.PlayRandom("Bonus ", 3));
				GameManager.CrossfadeToNextSong(true);
				_Core.AnimationManager.Run(0, 360, 0.5f, v => GameManager.View.Rotation = v); // do a barrel roll!
			}

			//_ParalaxBg.Position = GameManager.View.Center * -0.2f - GameManager.View.Size/2;
			var r = new Vector2f(GameManager.View.Center.X / _MapSize.X, GameManager.View.Center.Y / _MapSize.Y);
			_ParalaxBg.Position = _ParalaxBg.Texture.Size.ToVector2f().MultiplyBy(r * -1)- GameManager.View.Size/2;
		}

		protected override void Destroy()
		{
		}
	}
}