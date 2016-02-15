using System;
using XamainForms.Qmunicate;
using System.IO;
using Xamarin.Forms;
using XamarinForms.Qmunicate.Android;

[assembly: Dependency(typeof(SqliteAndroid))]
namespace XamarinForms.Qmunicate.Android
{
	public class SqliteAndroid : ISqlite
	{
		const string sqliteFilename = "qmunicate.db3";

		public SqliteAndroid ()
		{
		}

		public SQLite.Net.SQLiteConnection GetConnection ()
		{
			string documentsPath = System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal);
			var path = Path.Combine(documentsPath, sqliteFilename);

			var plat = new SQLite.Net.Platform.XamarinAndroid.SQLitePlatformAndroid();
			var conn = new SQLite.Net.SQLiteConnection(plat, path);
			return conn;
		}
	}
}

