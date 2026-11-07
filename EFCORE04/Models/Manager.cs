using System;
using System.Collections.Generic;
using System.Text;

namespace EFCORE04.Models
{
    public class Manager
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public DateTime HireDate { get; set; }
        public Branch? Branch { get; set; }
    }
}
