using System;

namespace Avira.VPN.Core
{
    public interface IUserManagementController
    {
        Action RunAfterTrialActivation { get; set; }

        Action RunAfterUserProfileChanged { get; set; }

        void ActivateTrial();
    }
}