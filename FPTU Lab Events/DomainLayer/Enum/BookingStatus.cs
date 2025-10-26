using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Enum
{
    /// <summary>
    /// Trạng thái của booking
    /// </summary>
    public enum BookingStatus
    {
        /// <summary>
        /// Chờ duyệt (0)
        /// </summary>
        Pending = 0,
        
        /// <summary>
        /// Đã duyệt (1)
        /// </summary>
        Approved = 1,
        
        /// <summary>
        /// Từ chối (2)
        /// </summary>
        Rejected = 2,
        
        /// <summary>
        /// Đã hủy (3)
        /// </summary>
        Cancelled = 3,
        
        /// <summary>
        /// Hoàn thành (4)
        /// </summary>
        Completed = 4
    }
}
