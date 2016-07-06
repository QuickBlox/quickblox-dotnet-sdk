using System;
using System.IO;
using SQLite.Net;
using Xamarin.Forms;
using XamarinForms.QbChat.iOS;
using QbChat.Pcl;

[assembly: Dependency(typeof(SqliteIOS))]
namespace XamarinForms.QbChat.iOS
{
	public class SqliteIOS : ISqlite
	{
		public SQLiteConnection GetConnection ()
		{
			var sqliteFilename = "qmunicate.db3";
			string documentsPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal); 
			string libraryPath = Path.Combine (documentsPath, "..", "Library"); 
			var path = Path.Combine(libraryPath, sqliteFilename);
			var plat = new SQLite.Net.Platform.XamarinIOS.SQLitePlatformIOS();
			var conn = new SQLite.Net.SQLiteConnection(plat, path);
			return conn;
		}
	}
}

