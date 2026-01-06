using System.Drawing;
using System.Numerics;

namespace Potatotype.GameServer.Assets
{
    public abstract class GameObject
    {
        public string Id => id;
        private readonly string id = Guid.NewGuid().ToString();
        public float Width => width;
        private float width;
        public float Height => height;
        private float height;
        public float X => x;
        private float x;
        public float Y => y;
        private float y;
        public float Rot => rot;
        private float rot;

        public RectangleF Hitbox => new RectangleF(x, y, width, height);

        public GameObject(float x = 0f, float y = 0f, float rot = 0f, float width = 50f, float height = 50f)
        {
            this.x = x;
            this.y = y;
            this.rot = rot;
            this.width = width;
            this.height = height;
        }

        public void UpdatePosition(float deltaX, float deltaY, float deltaRot)
        {
            x = Math.Clamp(x + deltaX, 0f, 800f - 50f); 
            y = Math.Clamp(y + deltaY, 0f, 600f - 50f);

            rot = deltaRot;
        }

        public void Move(float speed, float deltaX, float deltaY)
        {
            float radians = Rot * (float)(Math.PI / 180);
            x += (float)(Math.Cos(radians) * speed * deltaX);
            y += (float)(Math.Sin(radians) * speed * deltaY);
        }


        Random rng = new();
        public void NewPosition()
        {
            x = rng.Next(100, 700);
            y = rng.Next(100, 700);
        }

    }
}
