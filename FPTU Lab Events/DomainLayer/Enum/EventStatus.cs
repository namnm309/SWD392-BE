using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Enum
{
    public enum EventStatus
    {
        Pending,    // Chờ duyệt (Lecturer tạo event)
        Active,     // Đã duyệt và đang hoạt động
        Inactive,   // Không hoạt động
        Cancelled,  // Đã hủy
        Completed,  // Đã hoàn thành
        Rejected    // Bị từ chối bởi Staff
    }
}
