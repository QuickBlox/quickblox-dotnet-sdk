using System;
using SQLite.Net;

namespace XamainForms.Qmunicate
{
	public interface ISqlite
	{
		SQLiteConnection GetConnection();
	}
}

