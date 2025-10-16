using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Enum
{
    public enum EventStatus
    {
        Active,     // Đang hoạt động
        Inactive,   // Không hoạt động
        Cancelled,  // Đã hủy
        Completed   // Đã hoàn thành
    }
}
