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
    /*******************************************************************************************
    This class encapsulates all WiFi functionality
     * Created by: Federico Domínguez
    *******************************************************************************************/
    public class WiFiConfiguration
    {
        private bool connected;
        private WifiNetwork network;

        /// <summary>The WiFi RS21 module using socket 9 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.WiFiRS21 wifiRS21;

        // An event that clients can use to be notified whenever the
        // network is up.
        public event NetworkUpEventHandler NetworkUp;

        // An event that clients can use to be notified whenever the
        // network is down.
        public event NetworkDownEventHandler NetworkDown;

        // An event that clients can use to be notified whenever the
        // the wifi is attempting to connect.
        public event NetworkAttemptEventHandler NetworkAttempt;

        public WiFiConfiguration(Gadgeteer.Modules.GHIElectronics.WiFiRS21 wifiRS21)
        {
            connected = false;
            this.wifiRS21 = wifiRS21;

            wifiRS21.NetworkUp += wifiRS21_NetworkUp;
            wifiRS21.NetworkDown += wifiRS21_NetworkDown;

            //Open the interface
            if (wifiRS21.NetworkInterface.Opened)
                wifiRS21.NetworkInterface.Close();
            if (!wifiRS21.NetworkInterface.Opened)
                wifiRS21.NetworkInterface.Open();
        }

        public bool isConnected()
        {
            return connected;
        }

        /// <summary>Returns an array of all scanned networks, encapsulated using the WifiNetwork class.</summary>
        public WifiNetwork[] getAllWiFiNetworks()
        {
            WifiNetwork[] networks;

            GHI.Networking.WiFiRS9110.NetworkParameters[] parameters = wifiRS21.NetworkInterface.Scan();

            if (parameters != null)
            {
                networks = new WifiNetwork[parameters.Length];
                networks[0] = new WifiNetwork();
                for (int i = 0; i < parameters.Length; i++)
                {
                    networks[i] = new WifiNetwork(parameters[i].Ssid, parameters[i].Rssi);
                }
            }
            else
            {
                networks = new WifiNetwork[1];
                networks[0] = new WifiNetwork();
            }


            return networks;
        }

        public void connect(WifiNetwork network)
        {
            //Notify that the module will attempt to connect to a wifi network
            NetworkAttempt();
            
            if (wifiRS21.NetworkInterface.Opened)
                wifiRS21.NetworkInterface.Close();
            if (!wifiRS21.NetworkInterface.Opened)
                wifiRS21.NetworkInterface.Open();
            if (!wifiRS21.NetworkInterface.IsDhcpEnabled)
                wifiRS21.NetworkInterface.EnableDhcp();

            try
            {
                if (network.isKnown())
                    wifiRS21.NetworkInterface.Join(network.Ssid, network.getPassword());
                else
                    wifiRS21.NetworkInterface.Join(network.Ssid);
            }catch(GHI.Networking.WiFiRS9110.JoinException e)
            {
                Debug.Print("Error de coneccion: "+e.Message);
                connected = false;
                NetworkDown("Error de coneccion");
            }
            

            this.network = network;
        }

        public void disconnect()
        {
            if (connected)
            {
                wifiRS21.NetworkInterface.Disconnect();
                connected = false;
                NetworkDown("Desconectado");
            }
            
        }

        void wifiRS21_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Network down");
            connected = false;
            NetworkDown("Desconectado");
        }

        void wifiRS21_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Network up");
            connected = true;
            NetworkUp(network);
        }

        public String connectedTo()
        {
            if (connected)
                return network.getLabel();
            else
                return "No hay red disponible";
        }
    }

    /*******************************************************************************************
    This class encapsulates information about an scanned wifi network
     * Created by: Federico Domínguez
    *******************************************************************************************/
    public class WifiNetwork
    {
        public String Ssid { get; private set; }
        public int Rssi { get; private set; }

        public bool isEmptyNetwork { get; private set; }

        /// <summary>A hashtable with known Wifi network passwords.</summary>
        private Hashtable passwords;

        public WifiNetwork(String Ssid, int Rssi)
        {
            this.Ssid = Ssid;
            this.Rssi = Rssi;
            this.isEmptyNetwork = false;

            //Add passwords for known networks
            passwords = new Hashtable();
            passwords.Add("CTI", "ct1esp0l");
            passwords.Add("CTI_DOMO", "ct1esp0l");
        }

        /// <summary>Creates an empty network, used to represent zero scanned networks.</summary>
        public WifiNetwork()
        {
            this.isEmptyNetwork = true;
        }

        public String getLabel()
        {
            if (isEmptyNetwork)
                return "No hay ninguna red";
            else
                return Ssid + " -" + Rssi + "dB";
        }

        public bool isKnown()
        {
            if (!isEmptyNetwork)
                return passwords.Contains(Ssid);
            else
                return false;
        }

        public String getPassword()
        {
            if (isKnown())
                return (String)passwords[Ssid];
            else
                return "";
        }

        public override String ToString()
        {
            return Ssid;
        }

    }
}