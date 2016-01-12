namespace AcsAcr122UScanAgent.Code
{
    using System;
    using System.Linq;
    using System.Web.Script.Serialization;

    using AcsAcr122UScanAgent.ACR122U.Core;
    using AcsAcr122UScanAgent.ACR122U.SystemExceptions;

    /// <summary>
    /// Helper used to write / read cards
    /// </summary>
    public static class NdefHelpers
    {
       
        public static byte[] FromHex(string hex, int incomingDatalength)
        {
            byte[] raw = new byte[(hex.Length / 2) + incomingDatalength + 1];
            for (int i = 0; i < hex.Length / 2; i++)
            {
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return raw;
        }

        public static byte[] FromHex(string hex)
        {
            hex = hex.Replace("-", "");
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return raw;
        }
        /// <summary>
        /// Create empty message and convert to  byte[] 
        /// </summary>
        /// <returns></returns>
        public static byte[] GetBytesToclear()
        {
            var page3Hex = "E1101200";
            var TLVLockControlHex = "0103A01044";
            var NdefTLVHex = "03";
            int payloudLength =  5 + 1;//5 is language code //1 additional
            int length = payloudLength + 4;
            NdefTLVHex = NdefTLVHex + length.ToString("X") + "D1" + "01" + payloudLength.ToString("X") + "54" + "05" + "656E2D5553";//Text
            var firstPartHex = page3Hex + TLVLockControlHex + NdefTLVHex;
            var bytes = FromHex(firstPartHex,0);
            int next = firstPartHex.Length / 2;
            var TLLterminatorHex = "FE";
            bytes[next] = FromHex(TLLterminatorHex)[0];
            return bytes;
        }
        /// <summary>
        /// Create NDEF message in  byte[]  format 
        /// </summary>
        /// <param name="IncomingText"></param>
        /// <returns></returns>
        public static byte[] GetBytesForWriting(string IncomingText)
        {
            var page3Hex = "E1101200";
            var TLVLockControlHex = "0103A01044";
            var NdefTLVHex = "03";
            int payloudLength = IncomingText.Length + 5 + 1;//5 is language code //1 additional
            int length = payloudLength + 4;
            NdefTLVHex = NdefTLVHex + length.ToString("X") + "D1" + "01" + payloudLength.ToString("X") + "54" + "05" + "656E2D5553";//Text
            var firstPartHex = page3Hex + TLVLockControlHex + NdefTLVHex;
            var bytes = FromHex(firstPartHex, IncomingText.Length);
            int next = firstPartHex.Length / 2;
            for (int i = 0; i < IncomingText.Length; i++)
            {
                var val = (byte)IncomingText[i];
                bytes[next] = val;
                next++;
            }
            var TLLterminatorHex = "FE";
            bytes[next] = FromHex(TLLterminatorHex)[0];
            return bytes;
      
        }


        /// <summary>
        /// Read card
        /// Possible error codes : -1,1,6,3
        /// if Error code=0-> card was readed succesfuly
        /// </summary>
        public static void ReadMifareUltralightcard()
        {
            var result = new WebSocketObjectWorker();
            result.Action = "read";
            result.Data = String.Empty;
            using (var _cardReader = new MagneticCardReader())
            {
                try
                {
                    //starting sector=8;
                    var isConnected = _cardReader.OpenConnection();
                    if (isConnected)
                    {
                        string wholeResultFrompage3 = String.Empty;
                        //read block by block take 4 bytes

                        try
                        {
                            var currentValue = _cardReader.GetCardId();
                            result.Id = currentValue;
                            //read Id
                        }
                        catch (Exception ex)
                        {
                            
                        }
                        
                        for (int i = 3; i < 39; i += 4)
                        {
                            var currentValue = _cardReader.ReadCardByBlock(i.ToString());
                            wholeResultFrompage3 += currentValue;
                        }



                        //parse json result
                        result.Data = wholeResultFrompage3.Substring(
                            wholeResultFrompage3.IndexOf('{'),
                            wholeResultFrompage3.IndexOf('}') - wholeResultFrompage3.IndexOf('{') + 1);


                        if (!string.IsNullOrEmpty(result.Data))
                        {
                            result.ErrorCode = 0;
                            result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);

                            foreach (var item in WebSocketServerContainer.currentserver.GetAllSessions())
                            {
                                string serializedObject = new JavaScriptSerializer().Serialize(result);
                                item.Send(serializedObject);
                            }
                        }
                        else
                        {
                            result.ErrorCode = 3;
                            result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                            foreach (var item in WebSocketServerContainer.currentserver.GetAllSessions())
                            {
                                item.Send(new JavaScriptSerializer().Serialize(result));
                            }
                        }
                    }
                }
                catch (ReaderNotConnectedException ex)
                {
                    result.ErrorCode = -1;
                    result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                    foreach (var item in WebSocketServerContainer.currentserver.GetAllSessions())
                    {
                        item.Send(new JavaScriptSerializer().Serialize(result));
                    }
                }
                catch (CardNotInsertedException ex)
                {
                    result.ErrorCode = 1;
                    result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                    foreach (var item in WebSocketServerContainer.currentserver.GetAllSessions())
                    {
                        item.Send(new JavaScriptSerializer().Serialize(result));
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorCode = 6;
                    result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode) + ex.Message;
                    foreach (var item in WebSocketServerContainer.currentserver.GetAllSessions())
                    {
                        item.Send(new JavaScriptSerializer().Serialize(result));
                    }
                }
            }
        }


        /// <summary>
        /// Write string message to card
        /// </summary>
        /// <param name="data"></param>
        public static void WriteDataToCard(string data)
        {
            using (var _cardReader = new MagneticCardReader())
            {
                var result = new WebSocketObjectWorker();
                result.Action = "write";
                try
                {

                    bool isOpen = _cardReader.OpenConnection();
                    if (isOpen)
                    {
                        var bytes = NdefHelpers.GetBytesForWriting(data);
                        var bytesDevidedBy4 = bytes.Split(4);
                        int initialPage = 3;
                        for (int i = 0; i < bytesDevidedBy4.Count(); i++)
                        {
                            _cardReader.WritebytesToMifareUltralight(bytesDevidedBy4[i], initialPage.ToString());
                            initialPage++;
                        }
                        result.ErrorCode = 0;
                        result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                        foreach (var item in WebSocketServerContainer.currentserver.GetAllSessions())
                        {
                            item.Send(new JavaScriptSerializer().Serialize(result));
                        }

                    }
                }
                catch (ReaderNotConnectedException ex)
                {
                    result.ErrorCode = -1;
                    result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                    foreach (var session in WebSocketServerContainer.currentserver.GetAllSessions())
                    {
                        session.Send(new JavaScriptSerializer().Serialize(result));
                    }
                }

                catch (CardNotInsertedException ex)
                {
                    result.ErrorCode = 1;
                    result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                    foreach (var session in WebSocketServerContainer.currentserver.GetAllSessions())
                    {
                        session.Send(new JavaScriptSerializer().Serialize(result));
                    }

                }
                catch (WriteToCardException ex)
                {
                    result.ErrorCode = 6;
                    result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode) + ex.Message;
                    foreach (var session in WebSocketServerContainer.currentserver.GetAllSessions())
                    {
                        session.Send(new JavaScriptSerializer().Serialize(result));
                    }

                }
                catch (AuthentificationToDeviceFailException ex)
                {
                    result.ErrorCode = 6;
                    result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode) + ex.Message;
                    foreach (var session in WebSocketServerContainer.currentserver.GetAllSessions())
                    {
                        session.Send(new JavaScriptSerializer().Serialize(result));
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        public static void ClearCard()
        {
            using (var _cardReader = new MagneticCardReader())
            {
                var result = new WebSocketObjectWorker();
                result.Action = "clear";
                try
                {
                    bool isOpen = _cardReader.OpenConnection();
                    if (isOpen)
                    {
                        var bytes = NdefHelpers.GetBytesToclear();
                        var bytesDevidedBy4 = bytes.Split(4);
                        int initialPage = 3;
                        for (int i = 0; i < bytesDevidedBy4.Count(); i++)
                        {
                            _cardReader.WritebytesToMifareUltralight(bytesDevidedBy4[i], initialPage.ToString());
                            initialPage++;
                        }
                        result.ErrorCode = 0;
                        result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                        foreach (var item in WebSocketServerContainer.currentserver.GetAllSessions())
                        {
                            item.Send(new JavaScriptSerializer().Serialize(result));
                        }
                    }
                }
                catch (ReaderNotConnectedException ex)
                {
                    result.ErrorCode = -1;
                    result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                    foreach (var session in WebSocketServerContainer.currentserver.GetAllSessions())
                    {
                        session.Send(new JavaScriptSerializer().Serialize(result));
                    }
                }

                catch (CardNotInsertedException ex)
                {
                    result.ErrorCode = 1;
                    result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode);
                    foreach (var session in WebSocketServerContainer.currentserver.GetAllSessions())
                    {
                        session.Send(new JavaScriptSerializer().Serialize(result));
                    }

                }
                catch (WriteToCardException ex)
                {
                    result.ErrorCode = 6;
                    result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode) + ex.Message;
                    foreach (var session in WebSocketServerContainer.currentserver.GetAllSessions())
                    {
                        session.Send(new JavaScriptSerializer().Serialize(result));
                    }

                }
                catch (AuthentificationToDeviceFailException ex)
                {
                    result.ErrorCode = 6;
                    result.ErrorMessage = ErrorMesagesExtensions.GetMessageBycode(result.ErrorCode) + ex.Message;
                    foreach (var session in WebSocketServerContainer.currentserver.GetAllSessions())
                    {
                        session.Send(new JavaScriptSerializer().Serialize(result));
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }
    }

    public static class ArraySpliter
    {
        public static T[][] Split<T>(this T[] arrayIn, int length)
        {
            bool even = arrayIn.Length % length == 0;
            int totalLength = arrayIn.Length / length;
            if (!even)
                totalLength++;

            T[][] newArray = new T[totalLength][];
            for (int i = 0; i < totalLength; ++i)
            {
                int allocLength = length;
                if (!even && i == totalLength - 1)
                    allocLength = arrayIn.Length % length;

                newArray[i] = new T[allocLength];
                Array.Copy(arrayIn, i * length, newArray[i], 0, allocLength);
            }
            return newArray;
        }
    }
}

