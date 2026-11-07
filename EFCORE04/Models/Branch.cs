using System;
using System.Collections.Generic;
using System.Text;

namespace EFCORE04.Models
{
    public class Branch
    {
        public string Code { get; set; } = null!;          // PK
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public int ManagerId { get; set; }                 // FK (unique)
        public Manager Manager { get; set; } = null!;
        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}
