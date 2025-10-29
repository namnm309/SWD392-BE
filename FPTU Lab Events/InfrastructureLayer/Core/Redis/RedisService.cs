using StackExchange.Redis;
using System.Text.Json;

namespace InfrastructureLayer.Core.Redis
{
    public interface IRedisService
    {
        Task SetOtpAsync(string email, string otp, TimeSpan expiry);
        Task<string?> GetOtpAsync(string email);
        Task DeleteOtpAsync(string email);
        Task<bool> ValidateOtpAsync(string email, string otp);
    }

    public class RedisService : IRedisService
    {
        private readonly IDatabase _database;
        private const string OTP_PREFIX = "otp:";

        public RedisService(IConnectionMultiplexer connectionMultiplexer)
        {
            _database = connectionMultiplexer.GetDatabase();
        }

        public async Task SetOtpAsync(string email, string otp, TimeSpan expiry)
        {
            var key = $"{OTP_PREFIX}{email.ToLowerInvariant()}";
            await _database.StringSetAsync(key, otp, expiry);
        }

        public async Task<string?> GetOtpAsync(string email)
        {
            var key = $"{OTP_PREFIX}{email.ToLowerInvariant()}";
            return await _database.StringGetAsync(key);
        }

        public async Task DeleteOtpAsync(string email)
        {
            var key = $"{OTP_PREFIX}{email.ToLowerInvariant()}";
            await _database.KeyDeleteAsync(key);
        }

        public async Task<bool> ValidateOtpAsync(string email, string otp)
        {
            var storedOtp = await GetOtpAsync(email);
            if (storedOtp == null) return false;
            
            var isValid = storedOtp.Equals(otp, StringComparison.OrdinalIgnoreCase);
            if (isValid)
            {
                await DeleteOtpAsync(email);
            }
            
            return isValid;
        }
    }
}
