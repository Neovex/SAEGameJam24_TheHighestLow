using System;
using System.Collections.Generic;
using System.Linq;
using BlackCoat;
using BlackCoat.Entities;
using SFML.System;

namespace LowHigh
{
    class EndScene : Scene
    {
        public EndScene(Core core) : base(core, "WinScreen", "Assets")
        { }

        protected override bool Load()
        {
            var tex = TextureLoader.Load("WinBG");
            Layer_Game.Add(new Graphic(_Core, tex)
            {
                Origin = tex.Size.ToVector2f() / 2,
                Scale = new Vector2f(4, 4)
            });
            _Core.DeviceResized += HandleDeviceResized;
            HandleDeviceResized(_Core.DeviceSize);
            return true;
        }

        private void HandleDeviceResized(Vector2f size)
        {
            Layer_Game.GetFirst<Graphic>().Position = size / 2;
        }

        protected override void Update(float deltaT)
        { }

        protected override void Destroy()
        { }
    }
}