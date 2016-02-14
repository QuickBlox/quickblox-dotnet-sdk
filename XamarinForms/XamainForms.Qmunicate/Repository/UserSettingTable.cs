using SQLite.Net.Attributes;

namespace XamainForms.Qmunicate.Repository
{
    public class UserSettingTable
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }

        public string Login { get; set; }
        public string Password { get; set; }
    }
}
