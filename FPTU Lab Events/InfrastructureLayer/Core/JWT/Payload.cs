using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainLayer.Enum;

namespace InfrastructureLayer.Core.JWT
{
    public class Payload
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public Guid SessionId { get; set; }
        public UserStatus Status { get; set; }
        public bool IsAdmin { get; set; }
    }
}
