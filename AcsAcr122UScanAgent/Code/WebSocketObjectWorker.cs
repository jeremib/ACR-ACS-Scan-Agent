namespace AcsAcr122UScanAgent.Code
{
    /// <summary>
    /// Object used to comunicate with websocket server client
    /// </summary>
    public class WebSocketObjectWorker
    {
        /// <summary>
        /// Incoming action methods,
        /// Available actions: write,read,clear,checkDevice 
        /// </summary>
        public string Action { get; set; }
        /// <summary>
        /// Error code int value
        /// Possible error code:
        /// 0 -No errors, Operation was successfully completed!
        ///-1 -Device not plugged in
        /// 1 -Card is not inserted
        /// 2 -Card is not empty
        /// 3 -Card is not empty
        /// 4 -Data that needs to be written is empty
        /// 4 -Data that needs to be written is empty
        /// 5 -Unrecognized action
        /// 6 -Unexpected error
        /// </summary>
        public int ErrorCode { get; set; }

        public string ErrorMessage { get; set; }
        /// <summary>
        /// Used to write and read data, 
        /// in case if action==read , Data=readed data, 
        /// in case if action==write , Data=writed data, 
        /// </summary>
        public string Data { get; set; }

        public string Id { get; set; }

    }
}