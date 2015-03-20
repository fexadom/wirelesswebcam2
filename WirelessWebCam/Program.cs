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
    // A delegate type for hooking up network up notifications.
    public delegate void NetworkUpEventHandler(WifiNetwork network);

    // A delegate type for hooking up network down notifications.
    public delegate void NetworkDownEventHandler(String msg);

    // A delegate type for hooking up network attempt notifications.
    public delegate void NetworkAttemptEventHandler();

    public partial class Program
    {
        private static GHI.Glide.Display.Window wifiWindow;
        private static GHI.Glide.Display.Window mainWindow;
        private static GHI.Glide.Display.Window cameraWindow;
        private static GHI.Glide.Display.Window qrWindow;
        private static CalibrationWindow calibrationWindow;
        private WiFiConfiguration wifi;
        private List wifiNetworksList;
        private bool isStreaming;
        private bool isWebCamOn;
        private bool toggleTakePicture;
        private GT.Timer captureTimer;
        Bitmap lastBitmap;

        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            /*******************************************************************************************
            Modules added in the Program.gadgeteer designer view are used by typing 
            their name followed by a period, e.g.  button.  or  camera.
            
            Many modules generate useful events. Type +=<tab><tab> to add a handler to an event, e.g.:
                button.ButtonPressed +=<tab><tab>
            
            If you want to do something periodically, use a GT.Timer and handle its Tick event, e.g.:
                GT.Timer timer = new GT.Timer(1000); // every second (1000ms)
                timer.Tick +=<tab><tab>
                timer.Start();
            *******************************************************************************************/


            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.
            Debug.Print("Program Started");

            isStreaming = false;
            isWebCamOn = false;
            toggleTakePicture = true;

            //Main window stuff
            mainWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.mainWindow));
            initializeMainWindow();

            // Wifi Stuff
            wifiWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.wifiWindow));
            wifi = new WiFiConfiguration(wifiRS21);
            wifi.NetworkUp += wifi_NetworkUp;
            wifi.NetworkDown += wifi_NetworkDown;
            wifi.NetworkAttempt += wifi_NetworkAttempt;
            initializeWifiWindow();

            //QR stuf
            qrWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.qrWindow));
            qrWindow.TapEvent += qrWindow_TapEvent;

            //Calibration stuff
            calibrationWindow = new CalibrationWindow(false, false);
            calibrationWindow.CloseEvent += OnCloseCalibrar;

            //Camera stuff
            cameraWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.cameraViewWindow));
            cameraWindow.TapEvent += cameraWindow_TapEvent;
            camera.BitmapStreamed += camera_BitmapStreamed;
            captureTimer = new GT.Timer(5000, GT.Timer.BehaviorType.RunContinuously);
            captureTimer.Tick += captureTimer_Tick;

            //Battery stuff
            ucBattery4xAA.DebugPrintEnabled = true;

            GlideTouch.Initialize();

            Glide.MainWindow = mainWindow;

            
        }

        void captureTimer_Tick(GT.Timer timer)
        {
            Debug.Print("Tick...");

            if (toggleTakePicture)
                camera.StartStreaming();
            else
            {
                sendBitmapToCloud();
            }

            toggleTakePicture = !toggleTakePicture;
        }

        void cameraWindow_TapEvent(object sender)
        {
            isStreaming = false;
            camera.StopStreaming();
            displayTE35.SimpleGraphics.Clear();
            Glide.MainWindow = mainWindow;
        }

        void qrWindow_TapEvent(object sender)
        {
            if (isWebCamOn)
            {
                Debug.Print("Deshabilitando webcam");
                captureTimer.Stop();
                isWebCamOn = false;
                TextBlock webcamstatus = (TextBlock)mainWindow.GetChildByName("webcamstatus");
                webcamstatus.Text = "Webcam OFF";
                mainWindow.FillRect(webcamstatus.Rect);
            }

            Glide.MainWindow = mainWindow;
        }

        void camera_BitmapStreamed(Camera sender, Bitmap e)
        {
            if (isStreaming)
                displayTE35.SimpleGraphics.DisplayImage(e, 0, 0);

            if (isWebCamOn)
            {
                Debug.Print("Saving bitmap");
                camera.StopStreaming();
                lastBitmap = e;
            }
        }

        private void OnCloseCalibrar(object sender)
        {
            Glide.MainWindow = mainWindow;
        }

        void wifi_NetworkDown(String msg)
        {
            TextBlock connectedTo = (TextBlock)wifiWindow.GetChildByName("connectedto");
            connectedTo.Text = msg;
            connectedTo.FontColor = GHI.Glide.Colors.Red;

            if (Glide.MainWindow.Equals(wifiWindow))
            {
                wifiWindow.FillRect(connectedTo.Rect);
                connectedTo.Invalidate();
            }

        }

        void wifi_NetworkUp(WifiNetwork network)
        {
            TextBlock connectedTo = (TextBlock)wifiWindow.GetChildByName("connectedto");
            connectedTo.Text = "Conectado a: " + network.getLabel();
            connectedTo.FontColor = GHI.Glide.Colors.Green;

            if (Glide.MainWindow.Equals(wifiWindow))
            {
                wifiWindow.FillRect(connectedTo.Rect);
                connectedTo.Invalidate();
            }
        }

        void wifi_NetworkAttempt()
        {
            TextBlock connectedTo = (TextBlock)wifiWindow.GetChildByName("connectedto");
            connectedTo.Text = "Conectando...";
            connectedTo.FontColor = GHI.Glide.Colors.White;

            if (Glide.MainWindow.Equals(wifiWindow))
            {
                wifiWindow.FillRect(connectedTo.Rect);
                connectedTo.Invalidate();
            }
        }

        private void sendBitmapToCloud()
        {
            try
            {
                POSTContent content = POSTContent.CreateBinaryBasedContent(lastBitmap.GetBitmap());
                HttpRequest request = HttpHelper.CreateHttpPostRequest("http://200.126.23.246/uploadImage", content, "multipart/form-data");

                request.SendRequest();
                request.ResponseReceived += request_ResponseReceived;

                lastBitmap.Dispose();
                Debug.Print("Imagen enviada");
            }
            catch (System.ObjectDisposedException oe)
            {
                Debug.Print("Error in sendBitmapToCloud(): " + oe.Message);
            }
            
        }

        void request_ResponseReceived(HttpRequest sender, HttpResponse response)
        {
            Debug.Print("Response: "+response.StatusCode);
        }

        public void showQR()
        {
            Glide.MainWindow = qrWindow;
        
            var qrrequest = WebClient.GetFromWeb("http://200.126.23.246/showqr");
            qrrequest.ResponseReceived += qrrequest_ResponseReceived;
            qrrequest.SendRequest();
        }

        void qrrequest_ResponseReceived(HttpRequest sender, HttpResponse response)
        {
            if (response.StatusCode == "200")
            {
                Debug.Print("Response OK");
                //This only works if the bitmap is the
                //same size as the screen it's flushing to

                if (isWebCamOn)
                {
                    displayTE35.SimpleGraphics.DisplayImage(response.Picture.MakeBitmap(),0,0);
                    displayTE35.SimpleGraphics.DisplayText("Webcam ON", Resources.GetFont(Resources.FontResources.NinaB), GT.Color.Black, 120, 220);
                }
            }
            else
            {
                Debug.Print("QR Request failed with status code " + response.StatusCode);
            }
        }
    }
}
