namespace QuickShare.Services
{
    public class OnlineCountService
    {
        private readonly Dictionary<string, DateTime> _visitors = new();
        private readonly object _lock = new();
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(15);

        public void UpdateVisitorActivity(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
                throw new ArgumentException("Uuid is required", nameof(uuid));

            lock (_lock)
            {
                _visitors[uuid] = DateTime.UtcNow;
            }
        }

        public int GetOnlineCount()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var active = _visitors.Where(kvp => (now - kvp.Value) < _timeout).ToList();
                return active.Count;
            }
        }

        public List<string> GetOnlineIps()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                return _visitors
                    .Where(kvp => (now - kvp.Value) < _timeout)
                    .Select(kvp => kvp.Key)
                    .ToList();
            }
        }
    }
}
