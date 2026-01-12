using Microsoft.AspNetCore.SignalR;
using Potatotype.GameServer.Assets;
using Potatotype.Services;
using System.Collections.Concurrent;

namespace Potatotype.GameServer
{
    public class Server
    {
        private readonly World _world = new();
        
        private readonly ConcurrentDictionary<string, Input> _inputs = new();

        private readonly ILogger<Server> _logger;
        private readonly IHubContext<InputHub> _hubContext;
        private readonly SaveService _saveService;
        

        private const int TICKS_PER_SECOND = 60;
        private const int MS_PER_TICK = 1000 / TICKS_PER_SECOND;

        private int PointsOnKill = 10;

        // Healbox timer
        private int HealBoxCountDown = 0;
        private Random rng = new Random();

        public Server(ILogger<Server> logger, IHubContext<InputHub> hubContext)
        {
            _saveService = new SaveService();
            _logger = logger;
            _hubContext = hubContext;

            StartLoop();
        }
        public async Task<bool> AddPlayer(string connectionId, string name)
        {
            try
            {
                var saved = await _saveService.GetSavedPlayer(connectionId, name);
                if (saved is not null)
                {
                    return _world.AddPlayer(saved);
                }else
                {
                    Player newPlayerSave = new Player(connectionId, name, 0f, 0f, Color:color.Blue);
                    await _saveService.SavePlayer(newPlayerSave);
                    return _world.AddPlayer(_saveService.GetSavedPlayer(connectionId, name).Result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load saved player {Name}, creating new.", name);
            }

            return _world.AddPlayer(connectionId, name);
        }
        public Task<bool> AddBullet(string ownerConnectionId)
            => Task.FromResult(_world.AddBullet(ownerConnectionId));
        public Task AddInput(string connectionId, string input)
        {
            if (connectionId is null) return Task.CompletedTask;

            var inObj = _inputs.GetOrAdd(connectionId, _ => new Input());

            if (string.IsNullOrEmpty(input))
                return Task.CompletedTask;

            var parts = input.Split(':', 2);
            string action, key;
            if (parts.Length == 2)
            {
                action = parts[0].ToLowerInvariant();
                key = parts[1].ToLowerInvariant();
            }
            else
            {
                action = "keydown";
                key = parts[0].ToLowerInvariant();
            }

            switch (action)
            {
                case "keydown":
                    inObj.Keys.Add(key);
                    break;
                case "keyup":
                    inObj.Keys.Remove(key);
                    break;
                case "rot":
                    inObj.Rotation(key);
                    //_logger.LogInformation($"Rotation : {key}");
                    break;
                default:
                    return Task.CompletedTask;
            }

            inObj.Recalculate(_world.GetPlayer(connectionId)?.Speed ?? 5f);
            _inputs[connectionId] = inObj;

            return Task.CompletedTask;
        }
        public Task UpdatePlayerPosition(string connectionId, float deltaX, float deltaY, float deltaRot)
        {
            _world.UpdatePlayerPosition(connectionId, deltaX, deltaY, deltaRot);
            return Task.CompletedTask;
        }
        public Task RemovePlayer(string connectionId)
        {
            _inputs.TryRemove(connectionId, out _);
            Player? playerSave = _world.GetPlayer(connectionId);
            
            if (playerSave is not null)
                _saveService.SavePlayer(playerSave);
            
            _world.RemovePlayer(connectionId);
            return Task.CompletedTask;
        }

        public async Task PlayerDeath(string connectionId, string killerName, int? killerScore)
        {
            Player? backup = _world.GetPlayer(connectionId) ?? null;
            if (backup is not null && !string.IsNullOrEmpty(connectionId))
            {
                _world.GetPlayer(connectionId)?.RestoreHealth();
                _world.GetPlayer(connectionId)?.NewPosition();
                await RemovePlayer(connectionId);
                await AddPlayer(connectionId, backup.Name);
            }

            if (!string.IsNullOrEmpty(killerName) && killerScore is not null)
                _saveService.SaveHighScore(killerName, (int)killerScore);

        }

        public IReadOnlyDictionary<string, Input> GetInputs() => _inputs;

        private void StartLoop()
        {
            Task.Run(async () =>
            {
                _logger.LogInformation("Game server loop started.");
                while (true)
                {
                    var now = DateTime.UtcNow;
                    float deltaSeconds = MS_PER_TICK / 1000f;

                    foreach (var kv in _inputs) // Input Handle
                    {
                        var connId = kv.Key;
                        var input = kv.Value;
                        _world.UpdatePlayerPosition(connId, input.x, input.y, input.rot);

                        if (input.Shoot && now - input.LastShootTime > input.FireRate)
                        {
                            _world.AddBullet(connId);
                            input.LastShootTime = now;
                        }
                    }

                    foreach (var player in _world.GetPlayers()) 
                    {
                        _world.Move(player.ConnectionId, player.Speed * deltaSeconds, 0, 0);
                        foreach (var bullet in _world.GetBullets())
                        {
                            if(!bullet.isAlive()) // Bullet Handle
                            {
                                _world.RemoveBullet(bullet.Id);
                                continue;
                            }
                            _world.Move(bullet.Id, bullet.Speed * deltaSeconds, 1, 1);

                            if (bullet.OwnerConnectionId == player.ConnectionId) // Collision Handle
                                continue;
                            bool isColliding = player.Hitbox.IntersectsWith(bullet.Hitbox);
                            if (isColliding)
                            {
                                _world.RemoveBullet(bullet.Id);
                                player.GetDemage(_world.GetPlayer(bullet.OwnerConnectionId)?.Demage ?? 0);
                            }

                            if (player.Health <= 0) // Player Dead
                            {
                                string killerName =
                                    _world.GetPlayer(bullet.OwnerConnectionId)?.Name ?? "Unknown";

                                _logger.LogInformation(
                                    $"Player {player.Name} killed by {killerName}");
                                _world.GetPlayer(bullet.OwnerConnectionId)?.IncScore(PointsOnKill);

                                await PlayerDeath(player.ConnectionId, killerName, _world.GetPlayer(bullet.OwnerConnectionId)?.Score);
                            }
                        }

                        foreach (var hbox in _world.GetHealthBoxes())
                        {
                            if (player.Hitbox.IntersectsWith(hbox.Hitbox)){
                                player.Heal(hbox.Heal);
                                await _world.RemoveHealBox(hbox.Id);
                            }       
                        }
                    }

                    HealBoxCountDown++;
                    if(HealBoxCountDown >= 1000) { 
                        await _world.AddHealBox(rng.Next(50,750),rng.Next(50,550), 20);
                        HealBoxCountDown = 0;
                    }

                    try
                    {
                        await _hubContext.Clients.All.SendAsync("GetPlayers", _world.GetPlayers());
                        await _hubContext.Clients.All.SendAsync("GetBulets", _world.GetBullets());
                        await _hubContext.Clients.All.SendAsync("GetHealBoxes", _world.GetHealthBoxes());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while sending.");
                    }

                    await Task.Delay(MS_PER_TICK);
                }
            });
        }
    }
}
