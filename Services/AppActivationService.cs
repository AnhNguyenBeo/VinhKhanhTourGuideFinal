using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinhKhanhTourGuide.Services
{
    public class AppActivationService
    {
        private const string FirstLaunchKey = "FirstLaunchCompleted";

        public bool IsFirstLaunch()
        {
            return !Preferences.Default.Get(FirstLaunchKey, false);
        }

        public void CompleteFirstLaunch()
        {
            Preferences.Default.Set(FirstLaunchKey, true);
        }
    }
}