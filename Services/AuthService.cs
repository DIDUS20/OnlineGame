using StackExchange.Redis;
using Microsoft.AspNetCore.Identity;

namespace Potatotype.Services;

public class AuthService
{
    private readonly IDatabase _redis;
    private readonly IPasswordHasher<string> _hasher;

    // Konstruktor akceptuje zależności (ułatwia testy / DI).
    // Jeśli nie podano IDatabase, używa RedisConnectorHelper (dotychczasowe zachowanie).
    public AuthService(IDatabase? redis = null, IPasswordHasher<string>? hasher = null)
    {
        _redis = redis ?? RedisConnectorHelper.Connection.GetDatabase();
        _hasher = hasher ?? new PasswordHasher<string>();
    }

    // Rejestracja: przyjmuje surowe hasło, normalizuje username, zapisuje hash.
    public async Task<bool> Register(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        username = username.Trim().ToLowerInvariant();
        var userKey = $"users:{username}";

        if (await _redis.KeyExistsAsync(userKey))
            return false;

        // Tworzymy hash przy użyciu PasswordHasher (można zmienić na BCrypt jeśli wolisz)
        var hash = _hasher.HashPassword(username, password);

        await _redis.HashSetAsync(userKey, new HashEntry[]
        {
            new("passwordHash", hash),
            new("createdAt", DateTime.UtcNow.ToString("O"))
        });

        return true;
    }

    // Logowanie: przyjmuje surowe hasło, pobiera hash i weryfikuje.
    // Obsługuje również istniejące hash'e BCrypt (jeżeli zaczynają się od "$2").
    public async Task<string?> Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        username = username.Trim().ToLowerInvariant();
        var userKey = $"users:{username}";

        if (!await _redis.KeyExistsAsync(userKey))
            return null;

        var hash = await _redis.HashGetAsync(userKey, "passwordHash");

        if (hash.IsNullOrEmpty)
            return null;

        var storedHash = hash.ToString();

        bool verified = false;

        // Jeśli hash wygląda jak BCrypt (np. zaczyna się od $2), użyj BCrypt
        if (storedHash.StartsWith("$2"))
        {
            try
            {
                verified = BCrypt.Net.BCrypt.Verify(password, storedHash);
            }
            catch
            {
                verified = false;
            }
        }
        else
        {
            // W przeciwnym razie użyj PasswordHasher
            var result = _hasher.VerifyHashedPassword(username, storedHash, password);
            verified = result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
        }

        if (!verified)
            return null;

        var token = Guid.NewGuid().ToString("N");

        await _redis.StringSetAsync(
            $"session:{token}",
            username,
            TimeSpan.FromHours(24)
        );

        return token;
    }

    public async Task<string?> GetUserFromToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var value = await _redis.StringGetAsync($"session:{token}");
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task Logout(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;

        await _redis.KeyDeleteAsync($"session:{token}");
    }
}
