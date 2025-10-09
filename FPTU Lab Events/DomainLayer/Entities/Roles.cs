using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;


namespace DomainLayer.Entities
{
    [Table("tbl_roles")]
    public class Roles : BaseEntity
    {
        public string name { get; set; }

        public string description { get; set; }

        public ICollection<Users> Users { get; set; } = new List<Users>();

    }
}
