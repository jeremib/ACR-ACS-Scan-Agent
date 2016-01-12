using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AcsAcr122UScanAgent
{
    using System.Web.Script.Serialization;

    using AcsAcr122UScanAgent.ACR122U;
    using AcsAcr122UScanAgent.Code;

    using SuperSocket.SocketBase;

    using SuperWebSocket;
    /// <summary>
    /// Main Form
    /// </summary>
    public partial class Home : Form
    {
        /// <summary>
        /// Intitialzie websocket srver 
        /// Implemented technology "SuperWebSocket"
        /// Link to package home pahe:https://github.com/kerryjiang/SuperWebSocket 
        /// </summary>
        WebSocketServer appServer = new WebSocketServer();

        /// <summary>
        /// Monitor pluggedin/out devices, Catch acs acr122u plugin time and initialize listening 
        /// </summary>
        USBControl control = new USBControl();

        public Home()
        {
            InitializeComponent();
            appServer.NewMessageReceived += new SessionHandler<WebSocketSession, string>(appServer_NewMessageReceived);
            WebSocketServerContainer.currentserver = appServer;
            appServer.Setup(2525);
            StartServer();
        }
        #region system tray region
        private void Form1_Load(object sender, EventArgs e)
        {
            notifyIcon1.BalloonTipText = "Application Minimized.";
            notifyIcon1.BalloonTipTitle = "Acs Acr122U scan agent.";
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                ShowInTaskbar = false;
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(1000);
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowInTaskbar = true;
            notifyIcon1.Visible = false;
            WindowState = FormWindowState.Normal;
        }
        #endregion

        private void Form1_Closing(object sender, EventArgs e)
        {
            control.Dispose();
            appServer.Stop();
            SmartcardManager.GetManager().Dispose();
        }

        private void Minimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        
        /// <summary>
        /// Start websocket server
        /// </summary>
        private void StartServer()
        {
            //Initialize listening for card input
            SmartcardManager.GetManager();
            appServer.Start();
            this.label_status.Text = "RUNNING";
            btn_control_server.Text = "STOP";
        }
        /// <summary>
        /// Stop websocket server 
        /// </summary>
        private void StopServer()
        {
            appServer.Stop();
            SmartcardManager.GetManager().Dispose();
            this.label_status.Text = "STOPPED";
            btn_control_server.Text = "START";
        }

        /// <summary>
        /// Stop  or start websocket server depending in current state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Stop_Click(object sender, EventArgs e)
        {
            ServerState state = this.appServer.State;
            if (state == ServerState.NotInitialized || state == ServerState.NotStarted)
            {
                this.StartServer();
                return;
            }

            if (state == ServerState.Running)
            {
                this.StopServer();
                return;
            }
        }

        /// <summary>
        /// handle received mesages/actions from websocket server consumer
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        static void appServer_NewMessageReceived(WebSocketSession session, string message)
        {
            try
            {
                var deserializer = new JavaScriptSerializer();

                //deserialize incoming json object to WebSocketObjectWorker
                var request = deserializer.Deserialize<WebSocketObjectWorker>(message);

                switch (request.Action)
                {
                    //Incoming command is to read card
                       case "read":
                        {
                            var result = new WebSocketObjectWorker();
                            result.Action = "read";
                            if (!AcsAcrWrapper.IsdevicePlugedIn())
                            {
                                result.ErrorCode = -1;
                                result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                                foreach (var item in WebSocketServerContainer.currentserver.GetAllSessions())
                                {
                                    item.Send(new JavaScriptSerializer().Serialize(result));
                                }
                                break;
                            }
                            //read inserted card
                            NdefHelpers.ReadMifareUltralightcard();
                            break;
                        }
                   //Incoming command is to write card
                    case "write":
                        {
                            var result = new WebSocketObjectWorker();
                            result.Action = request.Action;

                            if (!AcsAcrWrapper.IsdevicePlugedIn())
                            {
                                result.ErrorCode = -1;
                                result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                                foreach (var item in WebSocketServerContainer.currentserver.GetAllSessions())
                                {
                                    item.Send(new JavaScriptSerializer().Serialize(result));
                                }
                                break;
                            }

                            if (string.IsNullOrEmpty(request.Data))
                            {
                                result.ErrorCode = 4;
                                result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                                session.Send(new JavaScriptSerializer().Serialize(result));
                                break;
                            }
                            NdefHelpers.WriteDataToCard(request.Data);
                            break;
                        }

                    //Incoming command is to clear card   
                    case "clear":
                        {
                            var result = new WebSocketObjectWorker();
                            result.Action = request.Action;

                            if (!AcsAcrWrapper.IsdevicePlugedIn())
                            {
                                result.ErrorCode = -1;
                                result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                                foreach (var item in WebSocketServerContainer.currentserver.GetAllSessions())
                                {
                                    item.Send(new JavaScriptSerializer().Serialize(result));
                                }
                                break;
                            }
                            NdefHelpers.ClearCard();
                            break;
                        }
                    //Incoming command is to check if device is connected
                    case "checkDevice":
                        {
                            var result = new WebSocketObjectWorker();
                            result.Action = request.Action;
                            if (AcsAcrWrapper.IsdevicePlugedIn())
                            {
                                result.ErrorCode = 0;
                                result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                            }
                            else
                            {
                                result.ErrorCode = -1;
                                result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                            }
                            session.Send(new JavaScriptSerializer().Serialize(result));
                            break;
                        }
                    //Incoming command is Unrecognized
                    default:
                        {
                            var result = new WebSocketObjectWorker();
                            result.Action = request.Action;
                            result.ErrorCode = 5;
                            result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                            session.Send(new JavaScriptSerializer().Serialize(result));
                            //incoming action not recognized
                            break;
                        }
                }
            }
            catch (Exception exception)
            {
                var result = new WebSocketObjectWorker();
                result.ErrorCode = 6;
                result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode) + exception.Message;

                foreach (var item in WebSocketServerContainer.currentserver.GetAllSessions())
                {
                    item.Send(new JavaScriptSerializer().Serialize(result));
                }
            }
        }

    }
}
