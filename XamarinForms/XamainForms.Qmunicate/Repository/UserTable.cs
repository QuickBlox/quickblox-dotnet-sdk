using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XamarinForms.Qmunicate.Repository
{
    public class UserTable
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; internal set; }
        public string UserId { get; internal set; }
    }
}
