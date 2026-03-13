namespace QuickShare.Services
{
    public class DownloadTicket
    {
        public long FileId { get; set; }
        public string Ticket { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }

    public class DownloadTicketService
    {
        private List<DownloadTicket> _tickets = new();
        private readonly TimeSpan _ticketValidityDuration = TimeSpan.FromSeconds(60);

        public DownloadTicketService()
        {
            var timer = new System.Timers.Timer(30000); // 每30s清理一次过期的下载票
            timer.Elapsed += (sender, e) =>
            {
                var now = DateTime.Now;
                _tickets.RemoveAll(t => now - t.Expiration >= _ticketValidityDuration);
            };
            timer.AutoReset = true;
            timer.Start();
        }

        public string GenericDownloadTicket(long fileId)
        {
            var ticket = new DownloadTicket
            {
                FileId = fileId,
                Ticket = Guid.NewGuid().ToString(),
                Expiration = DateTime.Now
            };
            _tickets.Add(ticket);
            return ticket.Ticket;
        }

        public bool VerifyDownloadTicket(long fileId, string ticket)
        {
            var existingTicket = _tickets.FirstOrDefault(t => t.Ticket == ticket);
            if (existingTicket == null)
            {
                return false;
            }
            if (existingTicket.FileId == fileId &&
                DateTime.Now - existingTicket.Expiration < _ticketValidityDuration)
            {
                _tickets.Remove(existingTicket);
                return true;
            }
            else
            {
                _tickets.Remove(existingTicket);
                return false;
            }
        }
    }
}
