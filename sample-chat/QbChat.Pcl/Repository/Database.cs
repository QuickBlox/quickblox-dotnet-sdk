using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QbChat.Pcl.Repository
{
    public class Database
    {
        // the database
        private SQLiteConnection database;

        static object locker = new object();
        static Database instance;

        public static Database Instance()
        {
            if (instance == null)
            {
                instance = new Database();
            }
            return instance;
        }

        public Action dialogObserver = delegate { };
        public Action messageObserver = delegate { };

        private Database()
        {
        }

        public void SubscribeForDialogs(Action dialogsCallback)
        {
            dialogObserver += dialogsCallback;
        }

        public void UnSubscribeForDialogs(Action dialogsCallback)
        {
            dialogObserver -= dialogsCallback;
        }

        public void SubscribeForMessages(Action messagesCallback)
        {
			messageObserver += messagesCallback;
        }

        public void UnSubscribeForMessages(Action messagesCallback)
        {
			messageObserver -= messagesCallback;
        }

        public void Init(SQLiteConnection connection)
        {
            this.database = connection;
            this.database.CreateTable<MessageTable>();
            this.database.CreateTable<DialogTable>();
            this.database.CreateTable<UserTable>();

			this.database.BeginTransaction();
			this.database.Execute("DELETE FROM MessageTable");
			this.database.Execute("DELETE FROM DialogTable");
			this.database.Execute("DELETE FROM UserTable");
			this.database.Commit();
        }

        public void ResetAll()
        {
            this.database.BeginTransaction();
            this.database.Execute("DELETE FROM MessageTable");
            this.database.Execute("DELETE FROM DialogTable");
            this.database.Execute("DELETE FROM UserTable");
            this.database.Commit();

			dialogObserver = delegate {};
			messageObserver = delegate {};
        }

        public IList<MessageTable> GetMessages(String dialogId)
        {
            lock (locker)
            {
                return this.database.Table<MessageTable>().Where(x => x.DialogId == dialogId).ToList();
            }
        }

        public int SaveMessage(MessageTable item)
        {
            int retVal = 0;
            lock (locker)
            {
				var checkIfPresence = this.database.Table<MessageTable>().FirstOrDefault(x => x.ID == item.ID);
                if (checkIfPresence != null)
                {
                    retVal = item.ID = checkIfPresence.ID;
                    this.database.Update(item);
                }
                else
                {
                    this.database.Insert(item);
                    retVal = item.ID;
                }
            }

			messageObserver.Invoke ();

            return retVal;
        }

        public int SaveAllMessages(String dialogId, IEnumerable<MessageTable> messages)
        {
            int count = 0;
            lock (locker)
            {
                this.database.BeginTransaction();
                this.database.Execute("DELETE FROM MessageTable WHERE DialogId='" + dialogId + "'");
                this.database.Commit();
                count = this.database.InsertAll(messages);

            }

			messageObserver.Invoke ();

            return count;
        }

        public UserTable GetUser(int userId)
        {
            lock (locker)
            {
                return this.database.Table<UserTable>().FirstOrDefault(x => x.UserId == userId);
            }
        }

        public int SaveUser(UserTable item)
        {
            int retVal = 0;
            lock (locker)
            {
                var checkIfPresence = this.database.Table<UserTable>().FirstOrDefault(x => x.UserId == item.UserId);
                if (checkIfPresence != null)
                {
                    retVal = item.ID = checkIfPresence.ID;
                    this.database.Update(item);
                }
                else
                {
                    this.database.Insert(item);
                    retVal = item.ID;
                }
            }

            return retVal;
        }

        public List<DialogTable> GetDialogs()
        {
            lock (locker)
            {
                return this.database.Table<DialogTable>().ToList();
            }
        }

        public DialogTable GetDialog(string dialogId)
        {
            lock (locker)
            {
                return this.database.Table<DialogTable>().FirstOrDefault(d => d.DialogId == dialogId);
            }
        }

		public int SaveDialog(DialogTable item, bool isNotify = false)
        {
            int retVal = 0;
            lock (locker)
            {
                var checkIfPresence = this.database.Table<DialogTable>().FirstOrDefault(x => x.DialogId == item.DialogId);
                if (checkIfPresence != null)
                {
                    retVal = item.ID = checkIfPresence.ID;
                    this.database.Update(item);
                }
                else
                {
                    this.database.Insert(item);
                    retVal = item.ID;
                }
            }

			if (isNotify)
				dialogObserver.Invoke ();
            return retVal;
        }

        public int SaveAllDialogs(IEnumerable<DialogTable> dialogs)
        {
            int count = 0;
            lock (locker)
            {
                this.database.BeginTransaction();
                this.database.Execute("DELETE FROM DialogTable");
                this.database.Commit();
                count = this.database.InsertAll(dialogs);
            }

			dialogObserver.Invoke ();
            return count;
        }

		public void DeleteDialog (string dialogId)
		{
			lock (locker) {
				this.database.BeginTransaction();
				this.database.Execute("DELETE FROM DialogTable WHERE DialogId='" + dialogId + "'");
				this.database.Commit();
			}

			dialogObserver.Invoke ();
		}
    }
}
