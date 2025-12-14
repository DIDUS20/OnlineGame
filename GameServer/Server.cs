using Microsoft.AspNetCore.SignalR;
using Potatotype.GameServer.Assets;
using System.Collections.Concurrent;

namespace Potatotype.GameServer
{
    public class Input
    {
        public float x { get; private set; }
        public float y { get; private set; }
        public float rot { get; private set; }
        public bool Shoot => Keys.Contains("c");
        public DateTime LastShootTime { get; set; } = DateTime.MinValue;
        public TimeSpan FireRate { get; set; } = TimeSpan.FromMilliseconds(500);

        public HashSet<string> Keys { get; } = new();

        public void Recalculate()
        {
            x = 0;
            y = 0;
            rot = 0;

            const float speed = 5f;
            const float rot_speed = 5f;

            if (Keys.Contains("a")) x -= speed;
            if (Keys.Contains("d")) x += speed;
            if (Keys.Contains("w")) y -= speed;
            if (Keys.Contains("s")) y += speed;

            if (Keys.Contains("q")) rot -= rot_speed; 
            if (Keys.Contains("e")) rot += rot_speed; 
        }
    }

    public class Server
    {
        private readonly World _world = new();

        private readonly ConcurrentDictionary<string, Input> _inputs = new();

        private readonly ILogger<Server> _logger;
        private readonly IHubContext<InputHub> _hubContext;

        private const int TICKS_PER_SECOND = 60;
        private const int MS_PER_TICK = 1000 / TICKS_PER_SECOND;

        public Server(ILogger<Server> logger, IHubContext<InputHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;

            StartLoop();
        }
        public Task<bool> AddPlayer(string connectionId, string name)
            => Task.FromResult(_world.AddPlayer(connectionId, name));
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
                default:
                    return Task.CompletedTask;
            }

            inObj.Recalculate();
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
            _world.RemovePlayer(connectionId);
            return Task.CompletedTask;
        }

        public IReadOnlyDictionary<string, Input> GetInputs()
            => _inputs;

        private void StartLoop()
        {
            Task.Run(async () =>
            {
                _logger.LogInformation("Game server loop started.");
                while (true)
                {
                    var now = DateTime.UtcNow;
                    float deltaSeconds = MS_PER_TICK / 1000f;

                    foreach (var kv in _inputs)
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
                    
                    foreach (var bullet in _world.GetBullets())
                    {
                        if(!bullet.isAlive())
                        {
                            _world.RemoveBullet(bullet.GetId);
                            continue;
                        }

                        bullet.UpdatePosition(deltaSeconds);
                    }

                    try
                    {
                        await _hubContext.Clients.All.SendAsync("GetPlayers", _world.GetPlayers());
                        await _hubContext.Clients.All.SendAsync("GetBulets", _world.GetBullets());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while sending players.");
                    }

                    await Task.Delay(MS_PER_TICK);
                }
            });
        }
    }
}
