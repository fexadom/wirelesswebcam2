using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using GHI.Glide;
using GHI.Glide.Display;
using GHI.Glide.UI;

namespace WirelessWebCam
{
    public partial class Program
    {
        /// <summary>Initializes all camera functionality elements.</summary>
        private void initializeCameraWindow()
        {
            Button backButton = (Button)cameraWindow.GetChildByName("back");
            Button takePictureButton = (Button)cameraWindow.GetChildByName("takepicture");
            backButton.TapEvent += backButtonCamera_TapEvent;
            takePictureButton.TapEvent += takePictureButton_TapEvent;

            cameraBackgroundWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.cameraBackgroundWindow));
            cameraBackgroundWindow.TapEvent += cameraBackgroundWindow_TapEvent;

            pictureWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.pictureWindow));
            Button backPictureButton = (Button)pictureWindow.GetChildByName("back");
            Button showqrButton = (Button)pictureWindow.GetChildByName("showqr");
            showqrButton.TapEvent += showqrButton_TapEvent;
            backPictureButton.TapEvent += backPictureButton_TapEvent;
        }

        void backPictureButton_TapEvent(object sender)
        {
            switchState(State.Camera);
        }

        /// <summary>Shows the QR code in the display</summary>
        void showqrButton_TapEvent(object sender)
        {
            Debug.Print("Showing QR");

            if (wifi.isConnected())
                showQR();
        }

        /// <summary>When tapped, the device stops the streaming and loads a GUI window with the latest bitmap.
        /// Additionally, it sends the bitmap to the remote server.</summary>
        void cameraBackgroundWindow_TapEvent(object sender)
        {
            isStreaming = false;
            camera.StopStreaming();
            GHI.Glide.UI.Image image = (GHI.Glide.UI.Image)pictureWindow.GetChildByName("pictureholder");
            image.Bitmap = currentBitmap;
            Glide.MainWindow = pictureWindow;

            //meanwhile, send picture to server if network is available
            if (wifi.isConnected())
                sendBitmapToCloud();
        }

        /// <summary>Start streaiming to display</summary>
        void takePictureButton_TapEvent(object sender)
        {
            isStreaming = true;
            Glide.MainWindow = cameraBackgroundWindow;
            camera.StartStreaming(currentBitmap);
        }

        private void backButtonCamera_TapEvent(object sender)
        {
            switchState(State.Main);
        }
    }
}