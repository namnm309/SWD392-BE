using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainLayer.Enum;

namespace DomainLayer.Entities
{
    [Table("tbl_users")]
    public class Users : BaseEntity
    {
        //Đã có Id, CreatedAt, UpdatedAt kế thừa từ BaseEntity
        public string Username { get; set; }

        public string Fullname { get; set; }

        public string Password { get; set; }

        public string Email { get; set; }

        public string? MSSV { get; set; }

        public UserStatus status { get; set; } = UserStatus.Active;

        public ICollection<Roles> Roles { get; set; } = new List<Roles>();







    }
}
