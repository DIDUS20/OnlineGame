namespace Potatotype.GameServer.Assets.Objects
{
    public class HealBox : GameObject
    { 
        public int Heal { get; private set; }

        public HealBox (int heal, float x, float y, float rot = 0, float width = 25, float height = 25) 
            : base(x,y)
        {
            this.Heal = heal;
        }
    }
}
