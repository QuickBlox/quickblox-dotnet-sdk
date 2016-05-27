using QbChat.Pcl.Repository;
using Quickblox.Sdk.Modules.UsersModule.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace QbChat.UWP.ViewModels
{
    public class BaseChatViewModel : ViewModel
    {
        private string dialogName;
        private string messageText;

        protected List<User> opponentUsers;
        protected string dialogId;

        private BitmapImage imageSource;

        public BitmapImage ImageSource
        {
            get { return imageSource; }
            set { imageSource = value;
                RaisePropertyChanged();
            }
        }


        public BaseChatViewModel(string dialogId)
        {
            this.dialogId = dialogId;
            this.Messages = new ObservableCollection<MessageTable>();
        }

        public string DialogName
        {
            get { return dialogName; }
            set
            {
                dialogName = value;
                this.RaisePropertyChanged();
            }
        }

        public string MessageText
        {
            get { return messageText; }
            set
            {
                messageText = value;
                this.RaisePropertyChanged();
            }
        }

        public ObservableCollection<MessageTable> Messages { get; private set; }

        public ICommand SendMessageCommand { get; protected set; }

        protected async Task SetRecepientName(MessageTable messageTable)
        {
            if (messageTable.SenderId == App.QbProvider.UserId)
            {
                messageTable.RecepientFullName = "Me";
            }
            else {
                var opponentUser = opponentUsers.FirstOrDefault(u => u.Id == messageTable.SenderId);
                if (opponentUser == null)
                {
                    var userRespose = await App.QbProvider.GetUserAsync(messageTable.SenderId);
                    if (userRespose != null)
                    {
                        this.opponentUsers.Add(userRespose);
                        messageTable.RecepientFullName = userRespose.FullName;
                    }
                }
                else {
                    messageTable.RecepientFullName = opponentUser.FullName;
                }
            }
        }
    }
}
