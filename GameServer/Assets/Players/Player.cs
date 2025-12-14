using System;
using System.Numerics;
using System.Xml.Schema;

namespace Potatotype.GameServer.Assets
{
    public class Player
    {
        public string ConnectionId { get; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Rot { get; private set; }
        public string Name { get; }

        public Player(string connectionId, string name)
        {
            ConnectionId = connectionId;
            X = 100;
            Y = 100;
            Rot = 0;
            Name = name;
        }

        public void UpdatePosition(float deltaX, float deltaY, float deltaRot)
        {
            X = Math.Clamp(X + deltaX, 0f, 800f - 50f);
            Y = Math.Clamp(Y + deltaY, 0f, 600f - 50f);
            
            if (Rot + deltaRot >= 360f)
                Rot -= 360f;
            else if (Rot + deltaRot <= 0f)
                Rot += 360f;
            Rot = Math.Clamp(Rot + deltaRot, 0f, 360f);

        }

        public Player(string connectionId, string name, float x, float y)
        {
            ConnectionId = connectionId;
            X = x;
            Y = y;
            Name = name;
        }
    }

    public class Bullet
    {

        private readonly string Id = Guid.NewGuid().ToString();
        public string GetId => Id;
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Rot { get; private set; }
        private float Speed = 20f; // pixels per second
        public string OwnerConnectionId { get; private set; }

        private readonly TimeSpan LifeTime = TimeSpan.FromSeconds(4);
        private DateTime creationTime { get; set; }

        public Bullet(float x, float y, float rot, string ownerId)
        {
            X = x + 50;
            Y = y + 25;
            Rot = rot;
            OwnerConnectionId = ownerId;
            creationTime = DateTime.Now;
        }

        public void UpdatePosition(float deltaSeconds)
        {
            float rad = Rot * (float)(Math.PI / 180.0);
            X += MathF.Cos(rad) * Speed * deltaSeconds;
            Y += MathF.Sin(rad) * Speed * deltaSeconds;
        }

        public bool isAlive() => (DateTime.Now - creationTime) < LifeTime;
        
    }
}
