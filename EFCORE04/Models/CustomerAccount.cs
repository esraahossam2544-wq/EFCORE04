using System;
using System.Collections.Generic;
using System.Text;

namespace EFCORE04.Models
{
    public class CustomerAccount
    {
        public int CustomerId { get; set; }
        public string AccountNumber { get; set; } = null!;
        public OwnershipType OwnershipType { get; set; }
        public DateTime OwnershipStartDate { get; set; }
        public AccountStatus AccountStatus { get; set; }
        public Customer Customer { get; set; } = null!;
        public Account Account { get; set; } = null!;
    }
}
