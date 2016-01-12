﻿
namespace AcsAcr122UScanAgent.ACR122U.SystemExceptions
{
    using AcsAcr122UScanAgent.Localization;

    public class AuthentificationToDeviceFailException:BaseException
    {
        public override string Message
        {
            get { return Resource.Exception_AuthentificationToDeviceFail; }
        }

        public override int ErrorCode
        {
            get { return 1001; }
        }
    }
}
