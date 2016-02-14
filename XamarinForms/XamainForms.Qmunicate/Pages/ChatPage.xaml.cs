using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace XamainForms.Qmunicate.Pages
{
    public partial class ChatPage : ContentPage
    {
        private string dialogId;

        public ChatPage(string dialogId)
        {
            InitializeComponent();
            this.dialogId = dialogId;
        }
    }
}
