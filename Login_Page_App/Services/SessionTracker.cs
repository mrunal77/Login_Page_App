using System;
using System.Collections.Concurrent;

namespace Login_Page_App.Services
{
    public class SessionInfo
    {
        public string UserId { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
        public bool WarningSent { get; set; }
    }

    public interface ISessionTracker
    {
        void AddOrUpdateSession(string userId, DateTimeOffset expiresAt);
        void RemoveSession(string userId);
        SessionInfo? GetSession(string userId);
        ConcurrentDictionary<string, SessionInfo> GetAllSessions();
    }

    public class SessionTracker : ISessionTracker
    {
        private readonly ConcurrentDictionary<string, SessionInfo> _sessions = new();

        public void AddOrUpdateSession(string userId, DateTimeOffset expiresAt)
        {
            _sessions.AddOrUpdate(userId, 
                _ => new SessionInfo { UserId = userId, ExpiresAt = expiresAt },
                (_, existing) => { existing.ExpiresAt = expiresAt; existing.WarningSent = false; return existing; });
        }

        public void RemoveSession(string userId)
        {
            _sessions.TryRemove(userId, out _);
        }

        public SessionInfo? GetSession(string userId)
        {
            _sessions.TryGetValue(userId, out var session);
            return session;
        }

        public ConcurrentDictionary<string, SessionInfo> GetAllSessions()
        {
            return _sessions;
        }
    }
}
