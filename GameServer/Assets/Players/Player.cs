using System;
using System.Numerics;
using System.Xml.Schema;

namespace Potatotype.GameServer.Assets
{
    public class Player : GameObject
    {
        public string ConnectionId { get; private set; }
        public string Name { get; private set; }

        public int MaxHealth { get; private set; } = 100;
        public int Health { get; private set; }

        public int Demage { get; private set; } = 10;
        public float BulletSpeed { get; private set; } = 500f;

        public float Speed { get; private set; } = 5f;
        public color Color { get; private set; } = color.Red;

        public Player(string connectionId, string name, float x, float y, float rot = 0f, float width = 50f, float height = 50f, color Color = color.Red) 
            :base (x,y,rot,width,height)
        {
            ConnectionId = connectionId;
            Name = name;
            Health = MaxHealth;
            this.Color = Color;
        }

        public int Score { get; private set; } = 0;
        public void IncScore(int points) => Score += points;
        public void DecScore(int points) => Score -= points;


        public void GetDemage(int demage)
        {
            Health -= demage;
            if (Health <= 0) Health = 0;
            else if (Health > MaxHealth)    Health = MaxHealth;
        }
        public void Heal(int heal)
        {
            Health += heal;
            if (Health <= 0) Health = 0;
            else if (Health > MaxHealth) Health = MaxHealth;
        }

        public void RestoreHealth() => Health = MaxHealth;
    }

    public enum color
    {
        Red,
        Blue,
        Green,
        Yellow,
        Purple,
        Orange,
        Black
    }
}
