namespace QuickShare.Services
{
    public class SendToService(
        SqliteService sqliteService,
        ShareWindowManage shareWindowManage)
    {
        public void ProcessSendToFiles(string[] filePaths)
        {
            var id = sqliteService.AddShareHistory(filePaths);
            var history = sqliteService.ReadShareHistory(id);
            shareWindowManage.CreateShareWindow(history);
        }
    }
}
