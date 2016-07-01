//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using UIKit;

namespace Xamarin.iOS.Conference.WebRTC
{
    [Register("SessionViewController")]
    partial class SessionViewController
    {
        [Outlet]
        UITextField _createSession { get; set; }
        [Outlet]
        UITextField _joinSession { get; set; }
        [Outlet]
        UIButton _createButton { get; set; }
        [Outlet]
        UIButton _joinButton { get; set; }

        void ReleaseDesignerOutlets()
        {
            if (_createSession != null)
            {
                _createSession.Dispose();
                _createSession = null;
            }

            if (_joinSession != null)
            {
                _joinSession.Dispose();
                _joinSession = null;
            }

            if (_createButton != null)
            {
                _createButton.Dispose();
                _createButton = null;
            }

            if (_joinButton != null)
            {
                _joinButton.Dispose();
                _joinButton = null;
            }
        }
    }
}