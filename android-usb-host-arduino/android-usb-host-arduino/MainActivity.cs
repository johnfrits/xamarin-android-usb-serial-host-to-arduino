using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Hardware.Usb;
using Java.Nio;
using System.Threading;
using Java.Lang;

namespace android_usb_host_arduino
{
    [Activity(Label = "android_usb_host_arduino", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private char CMD_LED_OFF = '1';
        private char CMD_LED_ON = '2';

        SeekBar bar;
        ToggleButton buttonLed;

        private UsbManager usbManager;
        private UsbDevice deviceFound;
        private UsbDeviceConnection usbDeviceConnection;
        private UsbInterface usbInterfaceFound = null;
        private UsbEndpoint endpointOut = null;
        private UsbEndpoint endpointIn = null;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            buttonLed = FindViewById<ToggleButton>(Resource.Id.toggleButton1);
            buttonLed.CheckedChange += ButtonLed_CheckedChange;
        }

        private void ButtonLed_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (e.IsChecked)
            {
                sendCommand(CMD_LED_ON);
            }
            else
            {
                sendCommand(CMD_LED_OFF);
            }

            usbManager = (UsbManager)GetSystemService(Context.UsbService);
        }
        public void onResume()
        {
            base.OnResume();

            Intent intent = new Intent();
            string action = intent.Action;

            UsbDevice device = (UsbDevice)intent.GetParcelableExtra(UsbManager.ExtraDevice);
            if (UsbManager.ActionUsbAccessoryAttached.Equals(action))
            {
                setDevice(device);
            }
            else if (UsbManager.ActionUsbAccessoryDetached.Equals(action))
            {
                if (deviceFound != null && deviceFound.Equals(device))
                {
                    setDevice(null);
                }
            }
        }
      
         
        private void setDevice(UsbDevice device)
        {
            usbInterfaceFound = null;
            endpointOut = null;
            endpointIn = null;
            const int UsbEndpointXferBulk = 2;
            const int UsbDirOut = 0;
            const int UsbDirIn = 128;
       
            for (int i = 0; i < device.InterfaceCount; i++)
            {
                UsbInterface usbif = device.GetInterface(i);

                UsbEndpoint tOut = null;
                UsbEndpoint tIn = null;

                int tEndpointCnt = usbif.EndpointCount;
                if (tEndpointCnt >= 2)
                {
                    for (int j = 0; j < tEndpointCnt; j++)
                    {
                        if (usbif.GetEndpoint(j).GetType == UsbEndpointXferBulk)
                        {
                            if (usbif.GetEndpoint(j).getDirection() == UsbDirOut)
                            {
                                tOut = usbif.GetEndpoint(j);
                            }
                            else if (usbif.GetEndpoint(j).getDirection() == UsbDirIn)
                            {
                                tIn = usbif.GetEndpoint(j);
                            }
                        }
                    }

                    if (tOut != null && tIn != null)
                    {
                        // This interface have both USB_DIR_OUT
                        // and USB_DIR_IN of USB_ENDPOINT_XFER_BULK
                        usbInterfaceFound = usbif;
                        endpointOut = tOut;
                        endpointIn = tIn;
                    }
                }

            }

            if (usbInterfaceFound == null)
            {
                return;
            }

            deviceFound = device;

            if (device != null)
            {
                UsbDeviceConnection connection =
                        usbManager.OpenDevice(device);
                if (connection != null &&
                        connection.ClaimInterface(usbInterfaceFound, true))
                {
                    usbDeviceConnection = connection;
                    System.Threading.Thread thread = new System.Threading.Thread(this);
                    thread.Start();

                }
                else
                {
                    usbDeviceConnection = null;
                }
            }
        }

        private void sendCommand(int control)
        {
            Synchronized(this) {

                if (usbDeviceConnection != null)
                {
                    byte[] message = new byte[1];
                    message[0] = (byte)control;
                    usbDeviceConnection.BulkTransfer(endpointOut,
                            message, message.Length, 0);
                }
            }
        }
        public void run()
        {
            ByteBuffer buffer = ByteBuffer.Allocate(1);
            UsbRequest request = new UsbRequest();
            request.Initialize(usbDeviceConnection, endpointIn);
            while (true)
            {
                request.Queue(buffer, 1);
                if (usbDeviceConnection.RequestWait() == request)
                {
                    byte rxCmd = buffer.Get(0);
                    if (rxCmd != 0)
                    {
                        bar.setp((int)rxCmd);
                    }

                    try
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                    catch (InterruptedException e)
                    {
                       
                    }
                }
                else
                {
                    break;
                }
            }

        }
    }
}

