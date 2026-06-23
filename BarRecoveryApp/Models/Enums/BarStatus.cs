using System;
using System.Collections.Generic;
using System.Text;

namespace BarRecoveryApp.Models.Enums
{
    public enum BarStatus
    {
        Created = 1,
        InRecovery = 2,
        PendingQuality = 3,
        Approved = 4,
        Rejected = 5,
        ReadyToShip = 6,
        Shipped = 7,
        Disposed = 8
    }
}
