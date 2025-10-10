using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Enum
{
    public enum BookingStatus
    {
        Pending,        // Chờ duyệt
        Approved,       // Đã duyệt
        Rejected,       // Từ chối
        Cancelled,      // Đã hủy
        Completed       // Hoàn thành
    }
}
