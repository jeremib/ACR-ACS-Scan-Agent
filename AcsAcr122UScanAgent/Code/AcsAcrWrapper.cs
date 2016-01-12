namespace AcsAcr122UScanAgent.Code
{
    using System.Linq;

    using AcsAcr122UScanAgent.ACR122U;

    public static class AcsAcrWrapper
    {
      public   static bool IsdevicePlugedIn()
        {
            var reader = SmartcardManager.GetManager();
            var listReaders = reader.ListReaders();

            if (!listReaders.Any())
            {
                return false;
            }
            else
            {
                return true;
                var neededReader = reader.ListReaders().FirstOrDefault(c => c == "ACS ACR122 0");
                if (neededReader == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

        }
    }
}
