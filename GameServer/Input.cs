namespace Potatotype.GameServer
{
    public class Input
    {
        public float x { get; private set; }
        public float y { get; private set; }
        public float rot { get; private set; }
        public DateTime LastShootTime { get; set; } = DateTime.MinValue;
        public TimeSpan FireRate { get; set; } = TimeSpan.FromMilliseconds(500);

        public HashSet<string> Keys { get; } = new();
        public bool Shoot => Keys.Contains("c");

        public void Recalculate(float speed)
        {
            x = 0;
            y = 0;

            if (Keys.Contains("a")) x -= speed;
            if (Keys.Contains("d")) x += speed;
            if (Keys.Contains("w")) y -= speed;
            if (Keys.Contains("s")) y += speed;
        }

        public void Rotation(string rotation)
        {
            if(float.TryParse(rotation, out var deltaRot))
                this.rot = deltaRot;
        }
    }

}
