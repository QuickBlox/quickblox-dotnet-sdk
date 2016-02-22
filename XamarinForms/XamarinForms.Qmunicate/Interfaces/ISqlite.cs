using System;
using SQLite.Net;

namespace XamainForms.QbChat
{
	public interface ISqlite
	{
		SQLiteConnection GetConnection();
	}
}

