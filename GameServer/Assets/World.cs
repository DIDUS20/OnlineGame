using StackExchange.Redis;
//using Potatotype.Services;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Potatotype.GameServer.Assets
{
    public class World
    {
        private Dictionary<string, Player> _players = new();
        private Dictionary<string, Bullet> _bullets = new();

        //private IDatabase _redis = RedisConnectorHelper.Connection.GetDatabase();
        public bool AddPlayer(string connectionId, string name)
        {
            /* Redis Based Implementation
            HashEntry[]? ifExist = _redis.HashGetAll(connectionId);
            if (ifExist.Length != 0) return false;
            foreach(var player in ifExist)
                if (player.Name == name) return false;
            HashEntry[] userHash = new HashEntry[]
            {
                new HashEntry("ConnectionId", connectionId),
                new HashEntry("Name", name),
                new HashEntry("X", 0),
                new HashEntry("Y", 0)
            };
            _redis.HashSet($"player:{connectionId}", userHash);
            _redis.SetAdd("active_players", connectionId);
            */
            if (_players.ContainsKey(connectionId))
                return false;

            _players[connectionId] = new Player(connectionId, name);
            return true;
        }
        public bool AddBullet(string ownerConnectionId)
        {
            var player = GetPlayer(ownerConnectionId);
            if (player is null)
                return false;
            var nb = new Bullet(player.X, player.Y, player.Rot, ownerConnectionId);
            _bullets[nb.GetId] = nb;
            return true;
        }
        public bool RemovePlayer(string connectionId)
        {
            /* Redis Based Implementation
            if (_redis.SetRemove("active_players", connectionId) && _redis.KeyDelete($"player:{connectionId}"))
                return true;
            else
                return false;
            */

            return _players.Remove(connectionId);
        }
        public bool RemoveBullet(string bulletId) => _bullets.Remove(bulletId);
        public List<Player> GetPlayers()
        {
            /* Redis Based Implementation 
            var ids = _redis.SetMembers("active_players");
            List<Player> allPlayers = new();
            foreach (var id in ids)
            {
                if(!string.IsNullOrEmpty(id))
                    if(GetPlayer(id) is not null)
                        allPlayers.Add(GetPlayer(id));
            }
            return allPlayers;
            */

            return _players.Values.ToList();
        }
        public List<Bullet> GetBullets() => _bullets.Values.ToList();
        public void UpdatePlayerPosition(string connectionId, float deltaX, float deltaY, float deltaRot)
        {
            if (_players.ContainsKey(connectionId))
                _players[connectionId].UpdatePosition(deltaX, deltaY, deltaRot);

            /* Redis Based Implementation
            string key = $"player:{connectionId}";
            float x = (float)_redis.HashGet(key, "X");
            float y = (float)_redis.HashGet(key, "Y");
            _redis.HashSet(key, new HashEntry[]
            {
                new HashEntry("X", Math.Clamp(x + deltaX, 0f, 800f - 50f)),
                new HashEntry("Y", Math.Clamp(y + deltaY, 0f, 800f - 50f))
            });
            */
        }
        private Player? GetPlayer(string connectionId)
        {
            /* Redis Based Implementation
            string key = $"player:{connectionId}";
            var name = _redis.HashGet(key, "Name");

            if (!string.IsNullOrEmpty(name))
            {
                Player p = new Player(
                    connectionId,
                    name,
                    (float)_redis.HashGet(key, "X"),
                    (float)_redis.HashGet(key, "Y")
                );

                return p;
            }else
                return null;
            */

            if (_players.ContainsKey(connectionId))
                return _players[connectionId];
            else
                return null;
        }
    }
}
