using System;
using System.Collections.Generic;
using System.Text;

namespace EFCORE04.Models
{
    public class Transaction
    {
        public int TransactionNumber { get; set; }         // PK identity
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public TransactionType TransactionType { get; set; }
        public string? Note { get; set; }
        public string AccountNumber { get; set; } = null!; // FK
        public Account Account { get; set; } = null!;
    }
}
