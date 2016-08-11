using System;
using Xamarin.Forms;
using AudioPlayEx;
using AudioPlayEx.iOS;
using System.IO;
using Foundation;
using AVFoundation;

[assembly: Dependency (typeof (AudioService))]
namespace AudioPlayEx.iOS
{
	public class AudioService : NSObject, IAudio, IAVAudioPlayerDelegate 
	{
		public AudioService ()
		{
		}

		public void PlayAudioFile(string fileName)
		{

			NSError error = null;
			AVAudioSession.SharedInstance ().SetCategory (AVAudioSession.CategoryPlayback, out error);

			string sFilePath = NSBundle.MainBundle.PathForResource(Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));
			var url = NSUrl.FromString (sFilePath);
			var _player = AVAudioPlayer.FromUrl(url);
			_player.Delegate = this;
			_player.Volume =100f;
			_player.PrepareToPlay();
			_player.FinishedPlaying += (object sender, AVStatusEventArgs e) => {
				_player = null;
			};
			_player.Play(); 

		}

		public void Dispose ()
		{

		}
	}
}

