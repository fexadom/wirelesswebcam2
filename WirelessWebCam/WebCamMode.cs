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
        /// <summary>The time period in ms used by the timer to either take a picture or send it to the remote server.</summary>
        const int WEBCAM_PICTURE_PERIOD = 5000;

        /// <summary>While the wecam is ON, this boolean toggles between taking a picture and sending it to the remote server.</summary>
        private bool toggleTakePicture;

        /// <summary>The timer used to take a picture or send it to the remote server.</summary>
        private GT.Timer captureTimer;

        /// <summary>Initializes all webcam functionality elements.</summary>
        private void initializeWebcamWindow()
        {
            toggleTakePicture = true;

            Button backButton = (Button)webcamWindow.GetChildByName("back");
            backButton.TapEvent += backButtonWebCam_TapEvent;

            Button startButton = (Button)webcamWindow.GetChildByName("startwebcam");
            startButton.TapEvent += startButton_TapEvent;

            Button stopButton = (Button)webcamWindow.GetChildByName("stopwebcam");
            stopButton.TapEvent += stopButton_TapEvent;

            Button showQRButton = (Button)webcamWindow.GetChildByName("viewQR");
            showQRButton.TapEvent += showQRButton_TapEvent;

            Button sleepButton = (Button)webcamWindow.GetChildByName("sleep");
            sleepButton.TapEvent += sleepButtonButton_TapEvent;

            sleepWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.sleepWindow));
            sleepWindow.TapEvent += sleepWindow_TapEvent;

            captureTimer = new GT.Timer(WEBCAM_PICTURE_PERIOD, GT.Timer.BehaviorType.RunContinuously);
            captureTimer.Tick += captureTimer_Tick;
        }

        /// <summary>Sets a black screen</summary>
        private void sleepButtonButton_TapEvent(object sender)
        {
            Glide.MainWindow = sleepWindow;
        }

        /// <summary>Shows the QR code in the display</summary>
        void showQRButton_TapEvent(object sender)
        {
            if (isWebCamOn && wifi.isConnected())
                showQR();
        }

        /// <summary>Stops the webcam functionality by stoping the timer...</summary>
        void stopButton_TapEvent(object sender)
        {
            if (isWebCamOn)
            {
                TextBlock webcamText = (TextBlock)webcamWindow.GetChildByName("webcamlabel");
                isWebCamOn = false;
                webcamText.Text = "WEBCAM OFF";
                webcamWindow.FillRect(webcamText.Rect);
                webcamText.Invalidate();

                //Enables Camera mode button in main window to allow user to use this mode while webcam is off
                Button cameraModeButton = (Button)mainWindow.GetChildByName("cameraMode");
                cameraModeButton.Enabled = true;

                //Stop timer
                captureTimer.Stop();
            }
        }

        /// <summary>Starts the webcam functionality by starting the timer...</summary>
        void startButton_TapEvent(object sender)
        {
            if (!isWebCamOn && wifi.isConnected())
            {
                TextBlock webcamText = (TextBlock)webcamWindow.GetChildByName("webcamlabel");
                isWebCamOn = true;
                webcamText.Text = "WEBCAM ON";
                webcamWindow.FillRect(webcamText.Rect);
                webcamText.Invalidate();

                //Disable Camera mode button in main window to forbid user to use this mode while webcam is on
                Button cameraModeButton = (Button)mainWindow.GetChildByName("cameraMode");
                cameraModeButton.Enabled = false;

                //Start timer    
                captureTimer.Start();
            }
        }

        void sleepWindow_TapEvent(object sender)
        {
            switchState(State.Webcam);
        }

        private void backButtonWebCam_TapEvent(object sender)
        {
            switchState(State.Main);
        }

        /// <summary>Timer tick where the device either takes a picture or sends it to the remote server.</summary>
        void captureTimer_Tick(GT.Timer timer)
        {
            Debug.Print("Tick...");

            if (isWebCamOn)
            {
                //Toggles between taking a picture or sending it to the remote server
                if (toggleTakePicture)
                    camera.StartStreaming(currentBitmap);
                else
                {
                    sendBitmapToCloud();
                }

                toggleTakePicture = !toggleTakePicture;
            }
        }


    }
}