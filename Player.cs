using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlackCoat;
using BlackCoat.Entities;
using BlackCoat.Entities.Shapes;
using BlackCoat.InputMapping;
using SFML.System;
using SFML.Graphics;
using BlackCoat.Entities.Animation;
using BlackCoat.AssetHandling;
using SFML.Audio;
using BlackCoat.Collision.Shapes;
using BlackCoat.Animation;

namespace LowHigh
{

    class Player : Container
    {
        public Vector2f InitialPosition { get; set; }
        public Func<Vector2f, CollisionType, bool> CollidesWith { get; set; }
        public Walk Walk { get; set; }
        public Jump Jump { get; set; }

		private SimpleInputMap<GameAction> _InputMap;
		private readonly TextureLoader _Loader;
		private BlittingAnimation _Walk;
        private BlittingAnimation _Idle;
        private Graphic _Jump;
        private Graphic _Fall;


		private Vector2f _Dimensions;
        private Vector2f _WallCheckOffsetLeft;
        private Vector2f _WallCheckOffsetRight;
        private Vector2f _OffsetToCenterGround;
        private Vector2f _Direction;
        private Vector2f _Velocity;

        public bool FacingRight => _Walk.Scale.X == 1;

        public Player(Core core, SimpleInputMap<GameAction> inputMap, TextureLoader loader) : base(core)
        {
            _InputMap = inputMap;
			_Loader = loader;
			_InputMap.MappedOperationInvoked += HandleInput;

            Walk = GameManager.Walk;
            Jump = GameManager.Jump;

			var tex = _Loader.Load("CharacterJump");
			_Dimensions = tex.Size.ToVector2f();
            _OffsetToCenterGround = new Vector2f(_Dimensions.X / 2, _Dimensions.Y);

            var maxSlope = 14;
            _WallCheckOffsetLeft = new Vector2f(0, _Dimensions.Y - maxSlope);
            _WallCheckOffsetRight = new Vector2f(_Dimensions.X, _Dimensions.Y - maxSlope);

            CollisionShape = new RectangleCollisionShape(_Core.CollisionSystem, Position, _Dimensions);

            // PlayerGfx
            //Add(new Rectangle(_Core, _Dimensions, Color.Yellow));
            var pos = new Vector2f(_OffsetToCenterGround.X, 0);
            var origin = new Vector2f(_OffsetToCenterGround.X, 0);
            tex = _Loader.Load("CharacterWalk");
            _Walk = new BlittingAnimation(_Core, 0.2f, tex, GameManager.CalcFrames(tex, 4, 1).ToArray())
            {
                Position = pos,
                Origin = origin
            };
            Add(_Walk);
            tex = _Loader.Load("CharacterJump");
            _Jump = new Graphic(_Core, tex)
            {
                Position = pos,
                Origin = origin
            };
            Add(_Jump);
			tex = _Loader.Load("CharacterIdle");
			_Idle = new BlittingAnimation(_Core, 0.15f, tex, GameManager.CalcFrames(tex, 4, 1).ToArray())
			{
				Position = pos,
				Origin = origin
			};
			Add(_Idle);
			tex = _Loader.Load("CharacterFall");
			_Fall = new Graphic(_Core, tex)
			{
				Position = pos,
				Origin = origin
			};
			Add(_Fall);
		}


        private void HandleInput(GameAction action, bool activate)
        {
            switch (action)
            {
                case GameAction.Left:
                    _Direction = new Vector2f(activate ? -1 : (_Direction.X == -1 ? 0 : _Direction.X), _Direction.Y);
                    break;
                case GameAction.Right:
                    _Direction = new Vector2f(activate ? 1 : (_Direction.X == 1 ? 0 : _Direction.X), _Direction.Y);
                    break;
                case GameAction.Jump:
                    if (activate)
                    {
                        if(_Velocity.Y == 0) GameManager.PlayRandom("Jump ", 4);
						_Direction = new Vector2f(_Direction.X, -1);
                    }
                    else
                        _Core.AnimationManager.Wait(Jump.JumpCutoffTime, () => _Direction = new Vector2f(_Direction.X, 0));
                    break;
            }
        }

        public override void Update(float deltaT)
        {
            base.Update(deltaT);

            // Calc Jump
            if (CollidesWith.Invoke(Position + _OffsetToCenterGround, CollisionType.Normal)) // OnGround
            {
                _Velocity.Y = _Direction.Y == -1 ? Jump.JumpForce : 0;
                while (CollidesWith.Invoke(Position + _OffsetToCenterGround, CollisionType.Normal)) // should clean
                {
                    Position = new Vector2f(Position.X, Position.Y - 1);
                }
                Position = new Vector2f(Position.X, Position.Y + 1);
			}
			else
            {
                var gravity = _Velocity.Y > 0 || _Direction.Y == 0 ? Jump.ApexGravity : Jump.DefaultGravity;
                _Velocity.Y = Math.Min(Jump.TerminalVelocity, _Velocity.Y + gravity);
            }

            // Calc Move
            float speedChange;
            if (_Direction.X != 0)
            {
                if (_Velocity.X != 0 && MathF.Sign(_Direction.X) != MathF.Sign(_Velocity.X))
                {
                    speedChange = Walk.TurnSpeed;
                }
                else
                {
                    speedChange = Walk.Acceleration;
                }
            }
            else
            {
                speedChange = Walk.Deceleration;
            }

            var desiredVelocity = _Direction.X * Walk.MaxSpeed;
            _Velocity.X = LERP(_Velocity.X, desiredVelocity, Math.Min(1, speedChange * deltaT));
            while (CollidesWith.Invoke(Position + _WallCheckOffsetRight, CollisionType.Normal))
            {
                _Velocity.X = 0;
                Position = new Vector2f(Position.X - 1, Position.Y);
            }
            while (CollidesWith.Invoke(Position + _WallCheckOffsetLeft, CollisionType.Normal))
            {
                _Velocity.X = 0;
                Position = new Vector2f(Position.X + 1, Position.Y);
            }
            while (CollidesWith(Position+new Vector2f(_OffsetToCenterGround.X, 0), CollisionType.Normal))
            {
                _Velocity.Y = 0;
                Position = new Vector2f(Position.X, Position.Y + 1);
            }
            // MOVE
            Position += _Velocity * deltaT;
            (CollisionShape as RectangleCollisionShape).Position = Position;

            // Update GFX
            if (_Velocity.X != 0) UpdateScaleForDirectionMirroring(_Velocity.X > 0);
            if (_Velocity.Y > -5f && _Velocity.Y < 5f)
            {
                if (_Velocity.X > -5f && _Velocity.X < 5f) // -> idle
                {
                    UpdateAnim(_Idle, _Walk, _Jump, _Fall);
                }
                else
                {
                    UpdateAnim(_Walk, _Idle, _Jump, _Fall);
                    if(!_SfL)
                    {
                        _SfL = true;
					    GameManager.PlayRandom("Grass Footstep ", 5);
                        _Core.AnimationManager.Wait(.5f, () => _SfL = false);
					}
				}
            }
			else if(_Velocity.Y > 0)
			{
				UpdateAnim(_Fall, _Walk, _Idle, _Jump);
			}
            else
			{
				UpdateAnim(_Jump, _Fall, _Walk, _Idle);
			}

            // Out of bounds failsave
            if (Position.Y > 20000) Position = InitialPosition;
            if (CollidesWith.Invoke(Position + _OffsetToCenterGround, CollisionType.Damage))
            {
                Position = InitialPosition;
                GameManager.PlayRandom("Damage ", 5);

			}
            //Cheats
            if(GameManager.Teleport)
            {
                GameManager.Teleport = false;
                Position = GameManager.MapToCoords(GameManager.Input.MousePosition, View);
            }
        }
        private bool _SfL = false;

        private static float LERP(float v0, float v1, float t) => (1 - t) * v0 + t * v1;

        private static void UpdateAnim(params Graphic[] animations)
        {
            foreach (var anim in animations)
            {
                anim.Visible = false;
            }
            animations[0].Visible = true;
        }
        private void UpdateScaleForDirectionMirroring(bool right)
        {
            _Walk.Scale = _Idle.Scale = _Jump.Scale = _Fall.Scale = new Vector2f(right ? 1 : -1, 1);
        }

        public void Destroy()
        {
            _InputMap.MappedOperationInvoked -= HandleInput;
        }
    }
}