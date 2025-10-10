using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Enum
{
    public enum RoomStatus
    {
        Available,  // Có sẵn
        Occupied,   // Đang sử dụng
        Maintenance, // Bảo trì
        Unavailable // Không khả dụng
    }
}
