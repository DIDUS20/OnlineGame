using System.Drawing;

namespace Potatotype.GameServer.Assets
{
    public class Bullet : GameObject
    {
        public float Speed { get; set; }
        public string OwnerConnectionId { get; private set; }

        private readonly TimeSpan LifeTime = TimeSpan.FromSeconds(4);
        private DateTime creationTime { get; set; }

        public Color color { get; set; } = Color.Black;

        public Bullet(
                float x, float y, 
                float rot, 
                string ownerId,
                float speed)
            : base(x, y, rot, 20, 20)
        {
            OwnerConnectionId = ownerId;
            creationTime = DateTime.Now;
            Speed = speed;
        }

        public bool isAlive() => (DateTime.Now - creationTime) < LifeTime;

    }
}
