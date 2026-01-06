using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Potatotype.GameServer.Assets;
using Potatotype.Services;

namespace Potatotype.GameServer
{
    public class InputHub : Hub
    {
        private readonly Server _server;
        private readonly ILogger<InputHub> _logger;

        public InputHub(ILogger<InputHub> logger, Server server)
        {
            _logger = logger;
            _server = server;
        }

        public async Task Input(string input)
        {
            if (input is not null)
            {
                //_logger.LogInformation($"Received input from {Context.ConnectionId}: {input}");
                await _server.AddInput(Context.ConnectionId, input);
            }
        }

        public async Task AddPlayer(string username)
        {
            if (await _server.AddPlayer(Context.ConnectionId, username))
                _logger.LogInformation($"Player {username} joined to server.");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await _server.RemovePlayer(Context.ConnectionId);
            _logger.LogInformation("Connection {ConnectionId} disconnected. Player removed.", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
