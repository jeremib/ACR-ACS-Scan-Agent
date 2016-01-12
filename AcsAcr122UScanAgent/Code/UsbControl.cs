namespace AcsAcr122UScanAgent.Code
{
    using System;
    using System.Linq;
    using System.Management;
    using System.Web.Script.Serialization;

    using AcsAcr122UScanAgent.ACR122U;

    class USBControl : IDisposable
    {
        // used for monitoring plugging and unplugging of USB devices.
        private ManagementEventWatcher watcherAttach;
        private ManagementEventWatcher watcherDetach;
        public USBControl()
        {
            // Add USB plugged event watching
            this.watcherAttach = new ManagementEventWatcher();
            this.watcherAttach.EventArrived += Attaching;
            this.watcherAttach.Query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
            this.watcherAttach.Start();

            // Add USB unplugged event watching
            this.watcherDetach = new ManagementEventWatcher();
            this.watcherDetach.EventArrived += Detaching;
            this.watcherDetach.Query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");
            this.watcherDetach.Start();
        }

        public void Dispose()
        {
            this.watcherAttach.Stop();
            this.watcherDetach.Stop();
            this.watcherAttach.Dispose();
            this.watcherDetach.Dispose();
        }

        void Attaching(object sender, EventArrivedEventArgs e)
        {

            var server = WebSocketServerContainer.currentserver;
            if (server != null)
            {
                var allConnections = server.GetAllSessions();
                foreach (var item in allConnections)
                {
                    var result = new WebSocketObjectWorker();
                    result.Action = "checkDevice";
                    result.ErrorCode = -1;
                    result.ErrorMessage = "New device is added";
                    item.Send(new JavaScriptSerializer().Serialize(result));
                }
            }
            if (sender != this.watcherAttach) return;
            var elem = SmartcardManager.GetManager().ListReaders();
            int counter = 150;
            while (!elem.Any() && counter > 0)
            {
                SmartcardManager.GetManager().Dispose();
                elem = SmartcardManager.GetManager().ListReaders();
                counter--;
            }
        }

        void Detaching(object sender, EventArrivedEventArgs e)
        {
            if (sender != this.watcherDetach) return;
            var readers = SmartcardManager.GetManager().ListReaders();
            if (!readers.Any())
            {
                SmartcardManager.GetManager().Dispose();
            }
            //e.Dump("Detaching");
        }

        ~USBControl()
        {
            this.Dispose();// for ease of readability I left out the complete Dispose pattern
        }
    }
}
