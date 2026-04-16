using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinhKhanhTourGuide.Services
{
    public class PurchaseService
    {
        private readonly PremiumService _premiumService;

        public PurchaseService(PremiumService premiumService)
        {
            _premiumService = premiumService;
        }

        public async Task<bool> PurchaseFullPackageAsync()
        {
            await Task.Delay(1000);

            bool paymentSuccess = true;

            if (paymentSuccess)
            {
                _premiumService.Unlock();
                return true;
            }

            return false;
        }
    }
}