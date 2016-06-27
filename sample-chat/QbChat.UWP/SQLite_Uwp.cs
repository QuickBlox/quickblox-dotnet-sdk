using SQLite.Net;
using System.IO;
using Windows.Storage;
using QbChat.Pcl;

namespace QbChat.UWP
{
    public class SQLite_Uwp : ISqlite
    {
        public SQLite_Uwp() { }
        public SQLiteConnection GetConnection()
        {
            var sqliteFilename = "TodoSQLite.db3";
            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, sqliteFilename);
            // Create the connection
            var platform = new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT();

            SQLite.Net.SQLiteConnection conn = new SQLite.Net.SQLiteConnection(platform, path);
            // Return the database connection
            return conn;
        }
    }
}
