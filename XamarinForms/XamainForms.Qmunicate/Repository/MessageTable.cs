using SQLite.Net.Attributes;
using System;

namespace XamainForms.Qmunicate.Repository
{
    public class MessageTable
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }

        public string MessageId { get; set; }
        public string Text { get; set; }
        public string DialogId { get; set; }
        public DateTime DateSent { get; set; }

        public int RecepientId { get; set; }
        public int SenderId { get; set; }
        public bool IsRead { get; set; }
    }
}
