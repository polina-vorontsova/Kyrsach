using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RDPCOMAPILib;


namespace Common
{
    public partial class frmServer : Form
    {
        private const int PORT = 5000;
        private const string GROUP_NAME = "COURSE";
        private const string STR_AUTH = "951005";
        private const string PASSWORD = "";
        private const int CLIENT_LIMIT = 1;
        private const int BUF_SIZE = 1024;
        private Socket sockServer;
        private Socket socClient;
        private Thread Listening;
        private Thread WaitCommand;
        private RDPSession session = null;
        private delegate void ConnectedClient();
        private delegate DialogResult ShowFolder();

        public frmServer()
        {
            InitializeComponent();
            this.BackColor = Color.FromArgb(195, 148, 244);
            rchLog.BackColor = Color.FromArgb( 255, 255, 255);
            btnClose.BackColor = Color.FromArgb(255, 255, 255);
            btnStart.BackColor = Color.FromArgb(255, 255, 255);
        }
        
        private List<string> GetLocalIP ()
        {
            List<string> IPs = new List<string>();
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    IPs.Add(ip.ToString());
            }
            IPs.Add("127.0.0.1");
            return IPs;
        }

        private void ShowIPs ()
        {
            foreach (var ip in GetLocalIP())
            {
                rchLog.Text += $"{ ip }\n";
            }
        }

        private bool IsCorrectIP (string EnteredIp)
        {
            foreach (string ip in GetLocalIP())
            {
                if (ip == EnteredIp)
                    return true;
            }
            return false;
        }

        private void StartWait(string StrIp)
        {
            IPAddress ip = IPAddress.Parse(StrIp);
            sockServer = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sockServer.Bind(new IPEndPoint(ip, PORT));
            sockServer.Listen(CLIENT_LIMIT);

            rchLog.Text = $"Server started on IP : {ip.ToString()}:{PORT}\n";
            rchLog.Text += "Waiting for client...\n";

            Listening = new Thread(StartListen);
            Listening.IsBackground = true;
            Listening.Start();
        }

        private void StartListen()
        {
            try
            {
                socClient = sockServer.Accept();
                Invoke(new ConnectedClient(NewConnection));
            }
            catch (Exception)
            {
                return;
            }
        }

        private void NewConnection()
        {
            rchLog.Text += $"Client connected : {socClient.RemoteEndPoint.ToString()}\n";
            CreateSession();

            IRDPSRAPIInvitation invitation = session.Invitations.CreateInvitation(STR_AUTH, GROUP_NAME, PASSWORD, CLIENT_LIMIT);
            socClient.Send(Encoding.Unicode.GetBytes(invitation.ConnectionString));

            StartWaitingCommand();
        }

        private void StartWaitingCommand()
        {
            WaitCommand = new Thread(Wait);
            WaitCommand.IsBackground = true;
            WaitCommand.Start();
        }

        private void Wait()
        {
            while (sockServer != null)
            {
                byte[] buffer = new byte[BUF_SIZE];
                int recbytes = socClient.Receive(buffer);
                ParseCommand(Encoding.Unicode.GetString(buffer , 0, recbytes));
            }
        }

        private void ParseCommand(string command)
        {
            string[] args = command.Split('|');
            string firstCommand = args[0];
            switch (firstCommand)
            {
                case "file":
                    AcceptFile(args[1], args[2]);
                    break;
            }
        }

        private string CutName (string Name)
        {
            int ind = Name.LastIndexOf('\\');
            return Name.Substring(ind + 1);
        }

        private void AcceptFile(string Name, string Size)
        {
            DialogResult dialogResult = MessageBox.Show($"Вы хотите принять файл {Name} размером {Size}Б?", "Файл", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                ShowFolder del = new ShowFolder(fbd.ShowDialog);
                if ((DialogResult)Invoke(del) == DialogResult.OK)
                {
                    byte[] buffer = new byte[Convert.ToInt32(Size)];
                    int bytesReceive = socClient.Receive(buffer);
                    File.WriteAllBytes(fbd.SelectedPath + "\\" + CutName(Name), buffer);
                }
            }
        }

        private void CreateSession()
        {
            session = new RDPSession();
            ConnectGuest(session);
        }

        private void CloseSession()
        {
            if (session != null)
                session.Close();
            if (sockServer != null)
                sockServer.Close();
            rchLog.Text += "Server has been stopped\n";
            session = null;
            sockServer = null;
            ShowIPs();
            btnStart.Enabled = true;
            txtIP.Enabled = true;
        }
            
        private void Incoming(object Guest)
        {
            IRDPSRAPIAttendee CurGuest = Guest as IRDPSRAPIAttendee;
            CurGuest.ControlLevel = CTRL_LEVEL.CTRL_LEVEL_INTERACTIVE;
            rchLog.Text += $"Connected attendee info :{CurGuest.RemoteName}\n";
        }

        private void Outcoming(object Guest)
        {
            IRDPSRAPIAttendeeDisconnectInfo pDiscInfo = Guest as IRDPSRAPIAttendeeDisconnectInfo;
            rchLog.Text += $"Disconnected attendee info : {pDiscInfo.Attendee.RemoteName}";
            CloseSession();
        }

        private void ConnectGuest(RDPSession rdpSession)
        {
            rdpSession.OnAttendeeConnected += Incoming;
            rdpSession.OnAttendeeDisconnected += Outcoming;
            rdpSession.Open();
        }
                
        private void frmServer_Load(object sender, EventArgs e)
        {
            ShowIPs();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            string ip = txtIP.Text;
            if (IsCorrectIP(ip))
            {
                txtIP.Enabled = false;
                btnStart.Enabled = false;
                StartWait(txtIP.Text);
            }
            else
            {
                MessageBox.Show("Incorrect IP Adress");
                txtIP.Text = "";
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            CloseSession();
        }
    }
}
