using Potatotype.GameServer.Assets;
using Potatotype.Models;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Potatotype.Services
{
    class SaveService
    {
        private readonly IDatabase _db;

        public SaveService()
        {
            _db =  RedisConnectorHelper.Connection.GetDatabase();
        }

        private static HashEntry[] CreateSaveHash(Player player)
            => new HashEntry[] { 
                new HashEntry("X", player.X),
                new HashEntry("Y", player.Y),
                new HashEntry("Rot", player.Rot),
                new HashEntry("Health", player.Health),
                new HashEntry("MaxHealth", player.MaxHealth),
                new HashEntry("Demage", player.Demage),
                new HashEntry("BulletSpeed", player.BulletSpeed),
                new HashEntry("Speed", player.Speed),
                new HashEntry("Color", player.Color.ToString()),
                new HashEntry("Score", player.Score)
            };

        public Task SavePlayer(Player player)
        {
            _db.HashSet("player_save:"+player.Name, CreateSaveHash(player));
            return Task.CompletedTask;
        }

        public Task<Player> GetSavedPlayer(string connectionId, string playerName) { 
            var save = _db.HashGetAll($"player_save:{playerName}");
    
            if (save is null || save.Length == 0)
                return Task.FromResult<Player>(null);

            string GetString(string key) => save.FirstOrDefault(e => e.Name == key).Value;
            bool TryGetFloat(string key, out float result)
            {
                result =0f;
                var s = GetString(key);
                return !string.IsNullOrEmpty(s) && float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result);
            }
            bool TryGetInt(string key, out int result)
            {
                result =0;
                var s = GetString(key);
                return !string.IsNullOrEmpty(s) && int.TryParse(s, out result);
            }

            // Parse position and rotation
            TryGetFloat("X", out var x);
            TryGetFloat("Y", out var y);
            TryGetFloat("Rot", out var rot);

            // Parse color first to use in constructor
            color ctorColor = color.Red;
            var colorStr = GetString("Color");
            if (!string.IsNullOrEmpty(colorStr))
            {
                Enum.TryParse(typeof(color), colorStr, true, out var parsedColor);
                if (parsedColor is not null)
                {
                    ctorColor = (color)parsedColor;
                }
            }

            // Create player with saved position and color
            var saved_player = new Player(connectionId, playerName, x, y, rot,50f,50f, ctorColor);

            // Parse and apply other properties safely without using private setters
            if (TryGetInt("Health", out var health))
            {
                // Player.Health is initialized to MaxHealth in ctor
                var max = saved_player.MaxHealth; // read-only
                if (health < max)
                {
                    saved_player.GetDemage(max - health);
                }
                else if (health > max)
                {
                    saved_player.Heal(health - max);
                }
            }

            if (TryGetInt("Score", out var score) && score >0)
            {
                // Score has private setter; use IncScore
                saved_player.IncScore(score);
            }

            return Task.FromResult(saved_player);
        }
        public void SaveHighScore(string playerName, int score)
        {
            var currentScore = _db.SortedSetScore("leaderboard", playerName);
            if (!currentScore.HasValue || score > (int)currentScore)
                _db.SortedSetAdd("leaderboard",playerName, score);
        }

        public List<ScoreModel> GetTop10()
        {
            var entries = _db.SortedSetRangeByRankWithScores(
                "leaderboard",
                0,
                9,
                Order.Descending
            );

            return entries.Select(e => new ScoreModel(Name: e.Element, Score: (int)e.Score)).ToList();
        }
    }
}
