using Potatotype.GameServer.Assets.Objects;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Potatotype.GameServer.Assets
{
    public class World
    {
        private Dictionary<string, Player> _players = new();
        private Dictionary<string, Bullet> _bullets = new();
        private Dictionary<string, HealBox> _healboxes = new();

        public bool AddPlayer(string connectionId, string name)
        {
            if (_players.ContainsKey(connectionId))
                return false;

            if (_players.Values.FirstOrDefault(p => p.Name == name) is not null)
                return false;

            _players[connectionId] = new Player(
                connectionId: connectionId, 
                name: name, 
                x: 0, 
                y: 0, 
                Color: color.Blue);
            _players[connectionId].NewPosition();
            return true;
        }

        public Task AddHealBox(float x, float y, int heal)
        {
            HealBox newbox = new HealBox(heal, x, y);
            _healboxes.Add(newbox.Id, newbox);
            return Task.CompletedTask;
        }

        public List<HealBox> GetHealthBoxes() => _healboxes.Values.ToList(); 
        public Task RemoveHealBox(string Id)
        {
            _healboxes.Remove(Id);
            return Task.CompletedTask;
        }
        public bool AddPlayer(Player player)
        {
            if (player is null) return false;
            if (_players.ContainsKey(player.ConnectionId))
                return false;
            if (_players.Values.FirstOrDefault(p => p.Name == player.Name) is not null)
                return false;

            _players[player.ConnectionId] = player;
            return true;
        }

        public bool AddBullet(string ownerConnectionId)
        {
            var player = GetPlayer(ownerConnectionId);
            if (player is null)
                return false;

            var nb = new Bullet(
                player.X,
                player.Y,
                player.Rot, ownerConnectionId, 
                player.BulletSpeed
            );
            _bullets[nb.Id] = nb;
            return true;
        }
        public bool RemovePlayer(string connectionId) => _players.Remove(connectionId);
        public bool RemoveBullet(string bulletId) => _bullets.Remove(bulletId);
        public List<Player> GetPlayers() => _players.Values.ToList();
        public List<Bullet> GetBullets() => _bullets.Values.ToList();
        public void Move(string objectId, float speed, float deltaX, float deltaY)
        {
            if (_players.ContainsKey(objectId))
                _players[objectId].Move(speed, deltaX, deltaY);
            else if (_bullets.ContainsKey(objectId))
                _bullets[objectId].Move(speed, deltaX, deltaY);
        }
        public void UpdatePlayerPosition(string connectionId, float deltaX, float deltaY, float deltaRot)
        {
            if (_players.ContainsKey(connectionId))
                _players[connectionId].UpdatePosition(deltaX, deltaY, deltaRot);
        }
        public Player? GetPlayer(string connectionId)
        {
            if (_players.ContainsKey(connectionId))
                return _players[connectionId];
            else
                return null;
        }
    }
}
