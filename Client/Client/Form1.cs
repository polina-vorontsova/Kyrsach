using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using AxRDPCOMAPILib;
using System.Threading;

namespace Client
{
    public partial class formClient : Form
    {
        private const int BUF_SIZE = 16384;
        private const int PORT = 5000;
        private const string USER_NAME = "POLINA";
        private const string PASSWORD = "";
        private Socket sockServer = null;
        private Thread WaitCommand;

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
                int recbytes = sockServer.Receive(buffer);
                ParseCommand(Encoding.Unicode.GetString(buffer, 0, recbytes));
            }
        }

        private void ParseCommand(string Command)
        {
            string[] args = Command.Split('|');
            string command = args[0];
            switch (command)
            {
                case "fileYes":
                    break;
            }

        }

        public formClient()
        {
            InitializeComponent();
            rdpViewer.SmartSizing = true;
            this.BackColor = Color.FromArgb(204, 255, 204);
            foreach (var control in MainPanel.Controls)
            {
                Button but = control as Button;
                if (but != null )
                    but.BackColor = Color.FromArgb(255, 255, 255);
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                IPAddress ip = IPAddress.Parse(txtIP.Text);
                sockServer = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sockServer.Connect(ip, PORT);

                byte[] buffer = new byte[BUF_SIZE];
                int recbytes = sockServer.Receive(buffer);
                string invitation = Encoding.Unicode.GetString(buffer, 0, recbytes);

                RDPConnect(invitation, this.rdpViewer, USER_NAME, PASSWORD);

                StartWaitingCommand();
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to connect!");
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            Disconnect(rdpViewer);
        }

        private void RDPConnect(string invitation, AxRDPViewer display, string UserName, string Password)
        {
            try
            {
                display.Connect(invitation, UserName, Password);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect. \nException :  {ex.Message}");
            }
        }

        private void Disconnect(AxRDPViewer display)
        {
            display.Disconnect();
        }


        private void formClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            rdpViewer.Disconnect();
        }

        private void rdpViewer_OnConnectionEstablished(object sender, EventArgs e)
        {
            btnSend.Enabled = true;
            txtIP.Enabled = false;
            btnConnect.Enabled = false;
            MessageBox.Show("Connection Established!");
        }

        private void rdpViewer_OnConnectionFailed(object sender, EventArgs e)
        {
            btnConnect.Enabled = true;
            txtIP.Enabled = true;
            btnSend.Enabled = false;
            MessageBox.Show($"ConnectionError");
        }

        private void rdpViewer_OnConnectionTerminated(object sender, _IRDPSessionEvents_OnConnectionTerminatedEvent e)
        {
            btnConnect.Enabled = true;
            txtIP.Enabled = true;
            btnSend.Enabled = false;
            sockServer = null;
            MessageBox.Show($"Connection Terminated. Reason: {e.discReason}");
        }

        private void rdpViewer_OnError(object sender, _IRDPSessionEvents_OnErrorEvent e)
        {
            int errorCode = (int)e.errorInfo;
            MessageBox.Show($"Error 0x{errorCode}");
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (sockServer != null)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        byte[] buffer = File.ReadAllBytes(dialog.FileName);
                        FileInfo info = new FileInfo(dialog.FileName);
                        sockServer.Send(Encoding.Unicode.GetBytes($"file|{info.FullName}|{buffer.Length}|"));
                        sockServer.Send(buffer);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show($"Exception : {ex.Message}");
                    }
                }
            }
        }

    }
}
