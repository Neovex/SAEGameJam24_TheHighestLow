using System;
using System.Collections.Generic;
using System.Linq;
using BlackCoat;
using BlackCoat.Entities;
using BlackCoat.Entities.Shapes;
using SFML.Graphics;
using SFML.System;

namespace LowHigh
{
	class MainMenuScene : Scene
	{
		private Rectangle _StartCollider;
		private Rectangle _ExitCollider;
		private Rectangle _CredCollider;
		private Graphic _Credits;
		private Rectangle _Selection;
		private bool _InputLock = false;

		public MainMenuScene(Core core) : base(core, "MainMenue", "Assets") { }

		protected override bool Load()
		{
			GameManager.View.Size = GameManager.FullHD;
			GameManager.View.Center = GameManager.FullHD / 2;
			Layer_Background.View = GameManager.View;
			Layer_Game.View = GameManager.View;
			Layer_Overlay.View = GameManager.View;

			var tex = TextureLoader.Load("highestlowtitlesccreen");
			Layer_Background.Add(new Graphic(_Core, tex));

			Layer_Game.Add(_StartCollider = new Rectangle(_Core, new Vector2f(355, 80), Color.Green)
			{
				Alpha = .2f,
				Position = new(781, 498),
				Visible = false
			});

			Layer_Game.Add(_ExitCollider = new Rectangle(_Core, new Vector2f(190, 80), Color.Red)
			{
				Alpha = .2f,
				Position = new(862, 682),
				Visible = false
			});
			Layer_Game.Add(_CredCollider = new Rectangle(_Core, new Vector2f(245, 70), Color.Yellow)
			{
				Alpha = .2f,
				Position = new(838, 870),
				Visible = false
			});
			tex = TextureLoader.Load("credits");
			Layer_Overlay.Add(_Credits = new Graphic(_Core, tex)
			{
				Position = GameManager.FullHD / 2 - tex.Size.ToVector2f() / 2,
				Visible = false
			});
			Layer_Game.Add(_Selection = new Rectangle(_Core, new Vector2f(400, 10), Color.Cyan)
			{
				Alpha = .25f,
			});

			GameManager.Sfx.Play("Wind Atmo");
			return true;
		}

		protected override void Update(float deltaT)
		{
			
			if (_StartCollider.CollidesWith(GameManager.MapToCoords(Input.MousePosition, GameManager.View)))
			{
				if (_Selection.Visible == false) GameManager.Sfx.Play("Extra");
				_Selection.Visible = true;
				_Selection.Position = new Vector2f(GameManager.FullHD.X/2-_Selection.Size.X/2, 
												_StartCollider.Position.Y+_StartCollider.Size.Y);
			}
			else if (_ExitCollider.CollidesWith(GameManager.MapToCoords(Input.MousePosition, GameManager.View)))
			{
				if (_Selection.Visible == false) GameManager.Sfx.Play("Extra");
				_Selection.Visible = true;
				_Selection.Position = new Vector2f(GameManager.FullHD.X / 2 - _Selection.Size.X/2,
												_ExitCollider.Position.Y + _StartCollider.Size.Y);
			}
			else if (_CredCollider.CollidesWith(GameManager.MapToCoords(Input.MousePosition, GameManager.View)))
			{
				if (_Selection.Visible == false) GameManager.Sfx.Play("Extra");
				_Selection.Visible = true;
				_Selection.Position = new Vector2f(GameManager.FullHD.X / 2 - _Selection.Size.X/2,
												_CredCollider.Position.Y + _StartCollider.Size.Y);
			}
			else _Selection.Visible = false;

			if (Input.IsMButtonDown(SFML.Window.Mouse.Button.Left) && !_InputLock)
			{
				if (_Credits.Visible)
				{
					GameManager.PlayRandom("Bonus ", 3);
					_Credits.Visible = false;
					_InputLock = true;
					_Core.AnimationManager.Wait(.65f, () => _InputLock = false);
				}
				else
				{
					if (_StartCollider.CollidesWith(GameManager.MapToCoords(Input.MousePosition, GameManager.View)))
					{
						GameManager.PlayRandom("Bonus ", 3);
						GameManager.LoadNextLevel(true);
					}
					if (_ExitCollider.CollidesWith(GameManager.MapToCoords(Input.MousePosition, GameManager.View)))
					{
						GameManager.PlayRandom("Bonus ", 3);
						_Core.Exit();
					}
					if (_CredCollider.CollidesWith(GameManager.MapToCoords(Input.MousePosition, GameManager.View)))
					{
						GameManager.PlayRandom("Bonus ", 3);
						_Credits.Visible = true;
						_InputLock = true;
						_Core.AnimationManager.Wait(.65f, () => _InputLock = false);
					}
				}
			}
			if (GameManager.Input.IsJoystickButtonDown(0, 1)) GameManager.LoadNextLevel();
		}

		protected override void Destroy()
		{
		}
	}
}