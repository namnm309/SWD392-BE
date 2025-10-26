using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Enum
{
    /// <summary>
    /// Trạng thái của người dùng
    /// </summary>
    public enum UserStatus
    {
        /// <summary>
        /// Chưa kích hoạt (0)
        /// </summary>
        Inactive = 0,
        
        /// <summary>
        /// Đã kích hoạt (1)
        /// </summary>
        Active = 1,
        
        /// <summary>
        /// Bị khóa (2)
        /// </summary>
        Locked = 2
    }
}
