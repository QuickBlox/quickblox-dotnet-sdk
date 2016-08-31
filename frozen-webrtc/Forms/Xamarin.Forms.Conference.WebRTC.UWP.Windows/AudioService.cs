using AudioPlayEx.WinRT;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioService))]
namespace AudioPlayEx.WinRT
{
    public class AudioService : IAudio
    {
        public AudioService()
        {
        }

        public void PlayAudioFile(string fileName)
        {
         
        }

        public void Dispose()
        {

        }
    }
}

