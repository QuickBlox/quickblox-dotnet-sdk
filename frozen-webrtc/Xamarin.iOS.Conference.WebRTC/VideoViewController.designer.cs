//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using UIKit;

namespace Xamarin.iOS.Conference.WebRTC
{
    [Register("VideoViewController")]
    partial class VideoViewController
    {
        [Outlet]
        UIBarButtonItem _sessionID { get; set; }
        [Outlet]
        UIBarButtonItem _leaveButton { get; set; }
        [Outlet]
        UIToolbar _toolBar { get; set; }
        [Outlet]
        public UIView _videoView { get; set; }
        
        void ReleaseDesignerOutlets()
        {
            if (_sessionID != null)
            {
                _sessionID.Dispose();
                _sessionID = null;
            }

            if (_leaveButton != null)
            {
                _leaveButton.Dispose();
                _leaveButton = null;
            }

            if (_toolBar != null)
            {
                _toolBar.Dispose();
                _toolBar = null;
            }

            if (_videoView != null)
            {
                _videoView.Dispose();
                _videoView = null;
            }
        }
    }
}