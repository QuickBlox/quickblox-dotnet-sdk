using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

using Foundation;
using UIKit;
using CoreGraphics;

namespace Xamarin.iOS.Conference.WebRTC
{
    public static class UIView_Toast
    {
        // general appearance
        const float CSToastMaxWidth = 0.8f;      // 80% of parent view width
        const float CSToastMaxHeight = 0.8f;      // 80% of parent view height
        const float CSToastHorizontalPadding = 10.0f;
        const float CSToastVerticalPadding = 10.0f;
        const float CSToastCornerRadius = 10.0f;
        const float CSToastOpacity = 0.8f;
        const float CSToastFontSize = 16.0f;
        const float CSToastMaxTitleLines = 0f;
        const float CSToastMaxMessageLines = 0f;
        const double CSToastFadeDuration = 0.2;

        // shadow appearance
        const float CSToastShadowOpacity = 0.8f;
        const float CSToastShadowRadius = 6.0f;
        static System.Drawing.SizeF CSToastShadowOffset = new System.Drawing.SizeF(4.0f, 4.0f);
        const bool CSToastDisplayShadow = true;

        // display duration and position
        const string CSToastDefaultPosition = "bottom";
        const double CSToastDefaultDuration = 3.0;
        const double CSToastDefaultDelay = 3.0;

        // image view size
        const float CSToastImageViewWidth = 80.0f;
        const float CSToastImageViewHeight = 80.0f;

        // activity
        const float CSToastActivityWidth = 100.0f;
        const float CSToastActivityHeight = 100.0f;
        const string CSToastActivityDefaultPosition = "center";

        // interaction
        const bool CSToastHidesOnTap = true;     // excludes activity views

        // associative reference keys
        const string CSToastTimerKey = "CSToastTimerKey";
        const string CSToastActivityViewKey = "CSToastActivityViewKey";

        public static void Toast(this UIView view, NSString message)
        {
            MakeToast(view, message, CSToastDefaultDuration, CSToastDefaultPosition);
        }

        public static void MakeToast(this UIView view, NSString message, double duration, string position)
        {
            UIView toast = ViewForMessage(view, message, null, null);
            ShowToast(view, toast, duration, position);
        }

        public static void ShowToast(this UIView view, UIView toast, double duration, string point)
        {
            toast.Center = CenterPointForPosition(view, point, toast);
            toast.Alpha = 0.0f;
    
            //if (CSToastHidesOnTap) {
            //    UITapGestureRecognizer *recognizer = [[UITapGestureRecognizer alloc] initWithTarget:toast action:@selector(handleToastTapped:)];
            //    [toast addGestureRecognizer:recognizer];
            //    toast.userInteractionEnabled = YES;
            //    toast.exclusiveTouch = YES;
            //}
    
            view.AddSubview(toast);
    
            UIView.Animate(CSToastFadeDuration, 0.0, (UIViewAnimationOptions.CurveEaseOut | UIViewAnimationOptions.AllowUserInteraction), () =>
            {
                toast.Alpha = 1.0f;
            }, 
            () => 
            {
                //using animate and not changing anything as our timer. to hide the toast.
                UIView.Animate(CSToastFadeDuration, duration, (UIViewAnimationOptions.CurveEaseIn | UIViewAnimationOptions.BeginFromCurrentState), () => { toast.Alpha = 0.0f; }, () => { HideToast(view, toast); });
            });
    
        }

        public static void HideToast(this UIView view, UIView toast)
        {
            UIView.Animate(CSToastFadeDuration, 0.0, (UIViewAnimationOptions.CurveEaseIn | UIViewAnimationOptions.BeginFromCurrentState), () => 
            {
                toast.Alpha = 0.0f;
            },
            () =>  {
                toast.RemoveFromSuperview();
            });
        }

        public static CGPoint CenterPointForPosition(this UIView view, string point, UIView toast)
        {
            if(!string.IsNullOrEmpty(point)) {
                // convert string literals @"top", @"bottom", @"center", or any point wrapped in an NSValue object into a CGPoint
                if(point.ToLower().Equals("top"))
                {
                    return new CGPoint(view.Bounds.Size.Width/2, (toast.Frame.Size.Height / 2) + CSToastVerticalPadding);
                } 
                else if(point.ToLower().Equals("bottom")) 
                {
                    return new CGPoint(view.Bounds.Size.Width/2, (view.Bounds.Size.Height - (toast.Frame.Size.Height / 2)) - CSToastVerticalPadding);
                } 
                else if(point.ToLower().Equals("center"))
                {
                    return new CGPoint(view.Bounds.Size.Width / 2, view.Bounds.Size.Height / 2);
                }
            } 
            //else if ([point isKindOfClass:[NSValue class]]) {
            //    return [point CGPointValue];
            //}
    
            //NSLog(@"Warning: Invalid position for toast.");
            return CenterPointForPosition(view, CSToastDefaultPosition, toast);
        }

        public static CGSize SizeForString(this UIView view, NSString str, UIFont font, CGSize constrainedSize, UILineBreakMode lineBreakMode)
        {
            //if ([string respondsToSelector:@selector(boundingRectWithSize:options:attributes:context:)]) {
            //    NSMutableParagraphStyle *paragraphStyle = [[NSMutableParagraphStyle alloc] init];
            //    paragraphStyle.lineBreakMode = lineBreakMode;
            //    NSDictionary *attributes = @{NSFontAttributeName:font, NSParagraphStyleAttributeName:paragraphStyle};
            //    CGRect boundingRect = [string boundingRectWithSize:constrainedSize options:NSStringDrawingUsesLineFragmentOrigin attributes:attributes context:nil];
            //    return CGSizeMake(ceilf(boundingRect.size.width), ceilf(boundingRect.size.height));
            //}

            return str.StringSize(font, constrainedSize, lineBreakMode);
        }

        public static UIView ViewForMessage(this UIView view, NSString message, NSString title, UIImage image)
        {
            // sanity
            if((message == null) && (title == null) && (image == null)) return null;

            // dynamically build a toast view with any combination of message, title, & image.
            UILabel messageLabel = null;
            UILabel titleLabel = null;
            UIImageView imageView = null;
    
            // create the parent view
            UIView wrapperView = new UIView();
            wrapperView.AutoresizingMask = (UIViewAutoresizing.FlexibleLeftMargin | UIViewAutoresizing.FlexibleRightMargin | UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleBottomMargin);
            wrapperView.Layer.CornerRadius = CSToastCornerRadius;
    
            if (CSToastDisplayShadow) {
                wrapperView.Layer.ShadowColor = UIColor.Black.CGColor;
                wrapperView.Layer.ShadowOpacity = CSToastShadowOpacity;
                wrapperView.Layer.ShadowRadius = CSToastShadowRadius;
                wrapperView.Layer.ShadowOffset = CSToastShadowOffset;
            }

            wrapperView.BackgroundColor = UIColor.Black.ColorWithAlpha(CSToastOpacity);
    
            if(image != null) {
                imageView = new UIImageView(image);
                imageView.ContentMode = UIViewContentMode.ScaleAspectFit;
                imageView.Frame = new System.Drawing.RectangleF(CSToastHorizontalPadding, CSToastVerticalPadding, CSToastImageViewWidth, CSToastImageViewHeight);
            }
    
            float imageWidth, imageHeight, imageLeft;
    
            // the imageView frame values will be used to size & position the other views
            if(imageView != null) {
				imageWidth = (float)imageView.Bounds.Size.Width;
				imageHeight = (float)imageView.Bounds.Size.Height;
                imageLeft = CSToastHorizontalPadding;
            } else {
                imageWidth = imageHeight = imageLeft = 0.0f;
            }
    
            if (title != null) {
                titleLabel = new UILabel();
                titleLabel.Lines = (int)CSToastMaxTitleLines;
                titleLabel.Font = UIFont.BoldSystemFontOfSize(CSToastFontSize);
                titleLabel.TextAlignment = UITextAlignment.Left;
                titleLabel.LineBreakMode = UILineBreakMode.WordWrap;
                titleLabel.TextColor = UIColor.White;
                titleLabel.BackgroundColor = UIColor.Clear;
                titleLabel.Alpha = 1.0f;
                titleLabel.Text = title;
        
                // size the title label according to the length of the text
				CGSize maxSizeTitle = new CGSize((view.Bounds.Size.Width * CSToastMaxWidth) - imageWidth, view.Bounds.Size.Height * CSToastMaxHeight);
				CGSize expectedSizeTitle = SizeForString(view, title, titleLabel.Font, maxSizeTitle, titleLabel.LineBreakMode);
                titleLabel.Frame = new CGRect(0.0f, 0.0f, expectedSizeTitle.Width, expectedSizeTitle.Height);
            }
    
            if (message != null) {
                messageLabel = new UILabel();
                messageLabel.Lines = (int)CSToastMaxMessageLines;
                messageLabel.Font = UIFont.SystemFontOfSize(CSToastFontSize);
                messageLabel.LineBreakMode = UILineBreakMode.WordWrap;
                messageLabel.TextColor = UIColor.White;
                messageLabel.BackgroundColor = UIColor.Clear;
                messageLabel.Alpha = 1.0f;
                messageLabel.Text = message;
        
                // size the message label according to the length of the text
				CGSize maxSizeMessage = new CGSize((view.Bounds.Size.Width * CSToastMaxWidth) - imageWidth, view.Bounds.Size.Height * CSToastMaxHeight);
				CGSize expectedSizeMessage = SizeForString(view, message, messageLabel.Font, maxSizeMessage, messageLabel.LineBreakMode);
				messageLabel.Frame = new CGRect(0.0f, 0.0f, expectedSizeMessage.Width, expectedSizeMessage.Height);
            }
    
            // titleLabel frame values
            float titleWidth, titleHeight, titleTop, titleLeft;
    
            if(titleLabel != null) {
				titleWidth = (float)titleLabel.Bounds.Size.Width;
				titleHeight = (float)titleLabel.Bounds.Size.Height;
                titleTop = CSToastVerticalPadding;
                titleLeft = imageLeft + imageWidth + CSToastHorizontalPadding;
            } else {
                titleWidth = titleHeight = titleTop = titleLeft = 0.0f;
            }
    
            // messageLabel frame values
            float messageWidth, messageHeight, messageLeft, messageTop;

            if(messageLabel != null) {
				messageWidth = (float)messageLabel.Bounds.Size.Width;
				messageHeight = (float)messageLabel.Bounds.Size.Height;
                messageLeft = imageLeft + imageWidth + CSToastHorizontalPadding;
                messageTop = titleTop + titleHeight + CSToastVerticalPadding;
            } else {
                messageWidth = messageHeight = messageLeft = messageTop = 0.0f;
            }

            float longerWidth = Math.Max(titleWidth, messageWidth);
            float longerLeft = Math.Max(titleLeft, messageLeft);
    
            // wrapper width uses the longerWidth or the image width, whatever is larger. same logic applies to the wrapper height
            float wrapperWidth = Math.Max((imageWidth + (CSToastHorizontalPadding * 2)), (longerLeft + longerWidth + CSToastHorizontalPadding));    
            float wrapperHeight = Math.Max((messageTop + messageHeight + CSToastVerticalPadding), (imageHeight + (CSToastVerticalPadding * 2)));
                         
			wrapperView.Frame = new CGRect(0.0f, 0.0f, wrapperWidth, wrapperHeight);
    
            if(titleLabel != null) {
				titleLabel.Frame = new CGRect(titleLeft, titleTop, titleWidth, titleHeight);
                wrapperView.AddSubview(titleLabel);
            }
    
            if(messageLabel != null) {
				messageLabel.Frame = new CGRect(messageLeft, messageTop, messageWidth, messageHeight);
                wrapperView.AddSubview(messageLabel);
            }
    
            if(imageView != null) {
                wrapperView.AddSubview(imageView);
            }
        
            return wrapperView;
        }
    }
}