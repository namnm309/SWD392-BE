using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Enum
{
    public enum ReportStatus
    {
        Open,       // Mở
        InProgress, // Đang xử lý
        Resolved,   // Đã giải quyết
        Closed      // Đã đóng
    }
}
