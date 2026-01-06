using System;
using Potatotype.GameServer.Assets;
using StackExchange.Redis;

namespace Potatotype.Services
{
    public static class RedisConnectorHelper
    {
        private static readonly Lazy<ConnectionMultiplexer> LazyConnection;

        static RedisConnectorHelper()
        {
            LazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                var redisConn = Environment.GetEnvironmentVariable("Redis__Connection") ?? "localhost:6379";
                return ConnectionMultiplexer.Connect(redisConn);
            });
        }

        public static ConnectionMultiplexer Connection => LazyConnection.Value;
    }
}
