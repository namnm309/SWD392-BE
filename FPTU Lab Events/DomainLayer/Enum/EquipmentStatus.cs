using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Enum
{
    public enum EquipmentStatus
    {
        Available,      // Có sẵn
        InUse,          // Đang sử dụng
        Maintenance,    // Bảo trì
        Broken,         // Hỏng
        Unavailable     // Không khả dụng
    }
}
