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

    public partial class Program
    {
        private static GHI.Glide.Display.Window wifiWindow;
        private static GHI.Glide.Display.Window mainWindow;
        private static CalibrationWindow calibrationWindow;
        private WiFiConfiguration wifi;
        private List wifiNetworksList;

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

            //Main window stuff
            mainWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.mainWindow));
            initializeMainWindow();

            // Wifi Stuff
            wifiWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.wifiWindow));
            wifi = new WiFiConfiguration(wifiRS21);
            wifi.NetworkUp += wifi_NetworkUp;
            wifi.NetworkDown += wifi_NetworkDown;
            initializeWifiWindow();
            updateWifiWindow();

            //Calibration stuff
            calibrationWindow = new CalibrationWindow(false, false);
            calibrationWindow.CloseEvent += OnCloseCalibrar;

            GlideTouch.Initialize();

            Glide.MainWindow = mainWindow;

            
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

        
    }
}
