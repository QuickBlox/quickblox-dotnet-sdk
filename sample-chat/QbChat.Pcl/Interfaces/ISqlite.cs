using System;
using SQLite.Net;

namespace QbChat.Pcl
{
	public interface ISqlite
	{
		SQLiteConnection GetConnection();
	}
}

