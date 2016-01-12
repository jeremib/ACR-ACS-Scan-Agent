namespace AcsAcr122UScanAgent.ACR122U.Listener
{
    using System.Web.Script.Serialization;

    using AcsAcr122UScanAgent.Code;

    public static class OnCardStateChange
    {
        public static async void OnCardInsert()
        {
            NdefHelpers.ReadMifareUltralightcard();
            
          /*  var sessions = WebSocketServerContainer.currentserver.GetAllSessions();
            foreach (var session in sessions)
            {
                NdefHelpers.ReadMifareUltralightcard();

             session.Send(new JavaScriptSerializer().Serialize(new WebSocketObjectWorker()
                {
                    Action = "read",
                    ErrorCode = 0,
                    ErrorMessage = "Inserted card was readed",
                    Data = "data from card"
                }));
            }*/
        }
        public static async void CardWasInserted()
        {
         
        }
        public static async void OnCardEject()
        {
         //  ReadCard.CardWasInserted();
        }
        
    }
}
