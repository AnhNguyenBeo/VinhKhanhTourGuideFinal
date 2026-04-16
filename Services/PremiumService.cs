using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinhKhanhTourGuide.Services
{
    public class PremiumService
    {
        private const string KEY = "IS_PREMIUM";

        public bool IsPremium()
        {
            return Preferences.Default.Get(KEY, false);
        }

        public void Unlock()
        {
            Preferences.Default.Set(KEY, true);
        }
    }
}
