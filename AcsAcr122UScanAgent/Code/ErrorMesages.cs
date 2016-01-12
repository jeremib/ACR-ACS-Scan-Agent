namespace AcsAcr122UScanAgent.Code
{

    public static class ErrorMesagesExtensions
    {
        public static string GetMessageBycode(int code)
        {
            string errormessage = "No errors";
            switch (code)
            {
                case 0:
                    {
                        errormessage = "Operation was successfully completed!";
                        break;
                    }
                case -1:
                    {
                        errormessage = "Device not plugged in";
                        break;
                    }
                case 1:
                    {
                        errormessage = "Card is not inserted";
                        break;
                    }
                case 2:
                    {
                        errormessage = "Card is not empty";
                        break;
                    }
                case 3:
                    {
                        errormessage = "Card is empty";
                        break;
                    }
                case 4:
                    {
                        errormessage = "Data that needs to be written is empty";
                        break;
                    }
                case 5:
                    {
                        errormessage = "Unrecognized action";
                        break;
                    }
                case 6:
                    {
                        errormessage = "Unexpected error:";
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            return errormessage;
        }
        
    }
 //Result codes
/*
 
 Result Code    Meaning
 0              No Errors
-1              Device not pluged in
1               Card not inserted
2               Card is not empty//deprecated
3               Card is empty
4               Data for write is empty
5               Unrecognized Action
 *
 
 */
}