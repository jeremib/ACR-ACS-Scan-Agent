namespace AcsAcr122UScanAgent.ACR122U.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using AcsAcr122UScanAgent.ACR122U.SystemExceptions;

    public class MagneticCardReader : IDisposable
    {
        private int retCode;
        private int hCard;
        private int hContext;
        private int Protocol;
        public bool connActive = false;
        private string readername = "ACS ACR122 0"; // change depending on reader
        public byte[] SendBuff = new byte[263];
        public byte[] RecvBuff = new byte[263];
        public int SendLen, RecvLen, nBytesRet, reqType, Aprotocol, dwProtocol, cbPciLength;
        public Card.SCARD_READERSTATE RdrState;
        public Card.SCARD_IO_REQUEST pioSendRequest;


        public bool OpenConnection()
        {
            this.connActive = true;

            #region Select device

            List<string> availableReaders = this.ListReaders();

            if (!availableReaders.Any())
            {
                throw new ReaderNotConnectedException();
            }
            this.RdrState = new Card.SCARD_READERSTATE();
            this.readername = availableReaders[0].ToString(); //selecting first device
            this.RdrState.RdrName = this.readername;

            #endregion

            this.retCode = Card.SCardEstablishContext(Card.SCARD_SCOPE_SYSTEM, 0, 0, ref this.hContext);
            if (this.retCode != Card.SCARD_S_SUCCESS)
            {
                this.connActive = false;
                throw new ReaderNotConnectedException();

            }

            this.retCode = Card.SCardConnect(this.hContext, this.readername, Card.SCARD_SHARE_SHARED,
                Card.SCARD_PROTOCOL_T0 | Card.SCARD_PROTOCOL_T1, ref this.hCard, ref this.Protocol);

            if (this.retCode != Card.SCARD_S_SUCCESS)
            {

                this.connActive = false;
                throw new CardNotInsertedException();
            }

            
            return this.connActive;
        }
        

        public List<string> ListReaders()
        {
            int ReaderCount = 0;
            List<string> AvailableReaderList = new List<string>();

            //Make sure a context has been established before 
            //retrieving the list of smartcard readers.
            this.retCode = Card.SCardListReaders(this.hContext, null, null, ref ReaderCount);
            if (this.retCode != Card.SCARD_S_SUCCESS)
            {
                  
            }

            byte[] ReadersList = new byte[ReaderCount];

            //Get the list of reader present again but this time add sReaderGroup, retData as 2rd & 3rd parameter respectively.
            this.retCode = Card.SCardListReaders(this.hContext, null, ReadersList, ref ReaderCount);
            if (this.retCode != Card.SCARD_S_SUCCESS)
            {
              //  MessageBox.Show(Card.GetScardErrMsg(retCode));
            }

            string rName = "";
            int indx = 0;
            if (ReaderCount > 0)
            {
                // Convert reader buffer to string
                while (ReadersList[indx] != 0)
                {

                    while (ReadersList[indx] != 0)
                    {
                        rName = rName + (char) ReadersList[indx];
                        indx = indx + 1;
                    }

                    //Add reader name to list
                    AvailableReaderList.Add(rName);
                    rName = "";
                    indx = indx + 1;

                }
            }
            return AvailableReaderList;

        }
        
        public string DecodeMifareUltraLightdata(string data)
        {
            var result = string.Empty;
            foreach(var item in data.Split(new char[] { '\0' }, int.MaxValue, StringSplitOptions.None))
            {
                result += item;
            }
            return result;
        }

        public string GetCardId()
        {
            string tmpStr = "";
            int indx;
            //Mifire ultralight does not need authenification

            this.ClearBuffers();
            this.SendBuff[0] = 0xFF; // CLA 
            this.SendBuff[1] = 0xB0; // INS
            this.SendBuff[2] = 0x00; // P1
            this.SendBuff[3] = (byte)int.Parse("0");
            this.SendBuff[4] = (byte)int.Parse("16");

            this.SendLen = 5;
            this.RecvLen = this.SendBuff[4] + 2;

            this.retCode = this.SendAPDUandDisplay(2);

            if (this.retCode == -200)
            {
                return "outofrangeexception";
            }

            if (this.retCode == -202)
            {
                return "BytesNotAcceptable";
            }

            if (this.retCode != Card.SCARD_S_SUCCESS)
            {
                return "FailRead";
            }

            // Display data in text format

            for (indx = 0; indx <= 7; indx++)
            {
                if(indx==3)
                {
                    
                }
                else
                {
                    var value = Convert.ToInt32(this.RecvBuff[indx]);
                    tmpStr += value;
                }
                
            }
            return (tmpStr);
        }

        
        public string GetCardUID() //only for mifare 1k cards
        {
            string cardUID = "";
            byte[] receivedUID = new byte[256];
            Card.SCARD_IO_REQUEST request = new Card.SCARD_IO_REQUEST();
            request.dwProtocol = Card.SCARD_PROTOCOL_T1;
            request.cbPciLength = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Card.SCARD_IO_REQUEST));
            byte[] sendBytes = new byte[] { 0xFF, 0xCA, 0x00, 0x00, 0x00 }; //get UID command      for Mifare cards
            int outBytes = receivedUID.Length;
            int status = Card.SCardTransmit(hCard, ref request, ref sendBytes[0], sendBytes.Length, ref request,
                ref receivedUID[0], ref outBytes);

            if (status != Card.SCARD_S_SUCCESS)
            {
                cardUID = "Error";
            }
            else
            {
                cardUID = BitConverter.ToString(receivedUID.ToArray()).Replace("-", string.Empty).ToLower();
            }

            return cardUID;
        }
        public string ReadCardByBlock(String Block)
        {
            return this.DecodeMifareUltraLightdata(this.ReadBlock(Block));
           // string value;
           // value = ;
           //// value = value.Split(new char[] {'\0'}, 2, StringSplitOptions.None)[0];
           // return value;
        }
        public string ReadBlock(String Block)
        {
            string tmpStr = "";
            int indx;
            //Mifire ultralight does not need authenification

            this.ClearBuffers();
            this.SendBuff[0] = 0xFF; // CLA 
            this.SendBuff[1] = 0xB0; // INS
            this.SendBuff[2] = 0x00; // P1
            this.SendBuff[3] = (byte)int.Parse(Block); // P2 : Block No.
            this.SendBuff[4] = (byte)int.Parse("16");

            this.SendLen = 5;
            this.RecvLen = this.SendBuff[4] + 2;

            this.retCode = this.SendAPDUandDisplay(2);

            if (this.retCode == -200)
            {
                return "outofrangeexception";
            }

            if (this.retCode == -202)
            {
                return "BytesNotAcceptable";
            }

            if (this.retCode != Card.SCARD_S_SUCCESS)
            {
                return "FailRead";
            }

            // Display data in text format

            for (indx = 0; indx <= this.RecvLen - 1; indx++)
            {
                tmpStr = tmpStr + Convert.ToChar(this.RecvBuff[indx]);
            }
            
            return (tmpStr);


        }

      public void WritebytesToMifareUltralight(byte[] data, String Block)
      {
          int indx;
          this.ClearBuffers();
          this.SendBuff[0] = 0xFF; // CLA
          this.SendBuff[1] = 0xD6; // INS
          this.SendBuff[2] = 0x00; // P1
          this.SendBuff[3] = (byte)int.Parse(Block); // P2 : Starting Block No.
          //For mifare ultralight data length =4
          this.SendBuff[4] = (byte)int.Parse("4"); // P3 : Data length
          for (indx = 0; indx <= (data).Length - 1; indx++)
          {
              this.SendBuff[indx + 5] = data[indx];
          }
          this.SendLen = this.SendBuff[4] + 5;
          this.RecvLen = 0x02;
          this.retCode = this.SendAPDUandDisplay(2);
          if (this.retCode != Card.SCARD_S_SUCCESS)
          {
              throw new WriteToCardException();
          }
      }

     
        // clear memory buffers
        private void ClearBuffers()
        {
            long indx;

            for (indx = 0; indx <= 262; indx++)
            {
                this.RecvBuff[indx] = 0;
                this.SendBuff[indx] = 0;
            }
        }

        // send application protocol data unit : communication unit between a smart card reader and a smart card
        private int SendAPDUandDisplay(int reqType)
        {
            int indx;
            string tmpStr = "";

            this.pioSendRequest.dwProtocol = this.Aprotocol;
            this.pioSendRequest.cbPciLength = 8;

            //Display Apdu In
            for (indx = 0; indx <= this.SendLen - 1; indx++)
            {
                tmpStr = tmpStr + " " + string.Format("{0:X2}", this.SendBuff[indx]);
            }

            this.retCode = Card.SCardTransmit(this.hCard, ref this.pioSendRequest, ref this.SendBuff[0],
                this.SendLen, ref this.pioSendRequest, ref this.RecvBuff[0], ref this.RecvLen);

            if (this.retCode != Card.SCARD_S_SUCCESS)
            {
                return this.retCode;
            }

            else
            {
                try
                {
                    tmpStr = "";
                    switch (reqType)
                    {
                        case 0:
                            for (indx = (this.RecvLen - 2); indx <= (this.RecvLen - 1); indx++)
                            {
                                tmpStr = tmpStr + " " + string.Format("{0:X2}", this.RecvBuff[indx]);
                            }

                            if ((tmpStr).Trim() != "90 00")
                            {
                                //MessageBox.Show("Return bytes are not acceptable.");
                                return -202;
                            }

                            break;

                        case 1:

                            for (indx = (this.RecvLen - 2); indx <= (this.RecvLen - 1); indx++)
                            {
                                tmpStr = tmpStr + string.Format("{0:X2}", this.RecvBuff[indx]);
                            }

                            if (tmpStr.Trim() != "90 00")
                            {
                                tmpStr = tmpStr + " " + string.Format("{0:X2}", this.RecvBuff[indx]);
                            }

                            else
                            {
                                tmpStr = "ATR : ";
                                for (indx = 0; indx <= (this.RecvLen - 3); indx++)
                                {
                                    tmpStr = tmpStr + " " + string.Format("{0:X2}", this.RecvBuff[indx]);
                                }
                            }

                            break;

                        case 2:

                            for (indx = 0; indx <= (this.RecvLen - 1); indx++)
                            {
                                tmpStr = tmpStr + " " + string.Format("{0:X2}", this.RecvBuff[indx]);
                            }

                            break;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    return -200;
                }
            }
            return this.retCode;
        }

        //disconnect card reader connection
        public void Close()
        {
            if (this.connActive)
            {
                this.retCode = Card.SCardDisconnect(this.hCard, Card.SCARD_UNPOWER_CARD);
            }
            //retCode = Card.SCardReleaseContext(hCard);
        }

       
        public void Dispose()
        {
           this.Close();

        }

    }
}
 
