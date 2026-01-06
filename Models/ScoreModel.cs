namespace Potatotype.Models
{
    public class ScoreModel
    {
        public int Score { get; private set; }
        public string Name { get; private set; }
        public ScoreModel(string Name = "Unknown", int Score = 0)
        {
            this.Name = Name;
            this.Score = Score;
        }
    }
}
