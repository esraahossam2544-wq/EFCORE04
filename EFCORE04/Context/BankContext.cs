using EFCORE04.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EFCORE04.Context
{
    public class BankContext:DbContext 
    {
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<Manager> Managers => Set<Manager>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<CustomerAccount> CustomerAccounts => Set<CustomerAccount>();
        public DbSet<Transaction> Transactions => Set<Transaction>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=.;Database=Bank System;Trusted_Connection=true;TrustServerCertificate=true");

        }
        protected override void OnModelCreating(ModelBuilder b)
        {
            // Manager <-> Branch : 1:1 (FK on Branch)
            b.Entity<Branch>(e =>
            {
                e.HasKey(x => x.Code);
                e.Property(x => x.Code).HasMaxLength(10);
                e.Property(x => x.Name).HasMaxLength(100).IsRequired();
                e.Property(x => x.Address).HasMaxLength(200).IsRequired();
                e.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
                e.HasOne(x => x.Manager).WithOne(m => m.Branch)
                 .HasForeignKey<Branch>(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
            });
            b.Entity<Manager>(e =>
            {
                e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
                e.Property(x => x.Email).HasMaxLength(100).IsRequired();
                e.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
            });

            b.Entity<Customer>(e =>
            {
                e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
                e.Property(x => x.NationalId).HasMaxLength(14).IsRequired();
                e.HasIndex(x => x.NationalId).IsUnique();
                e.Property(x => x.Email).HasMaxLength(100).IsRequired();
                e.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
                e.Property(x => x.Address).HasMaxLength(200).IsRequired();
                e.Property(x => x.CustomerType).HasConversion<string>().HasMaxLength(20);
            });

            // Branch 1 - M Account
            b.Entity<Account>(e =>
            {
                e.HasKey(x => x.AccountNumber);
                e.Property(x => x.AccountNumber).HasMaxLength(20);
                e.Property(x => x.CurrentBalance).HasPrecision(18, 2);
                e.Property(x => x.AccountType).HasConversion<string>().HasMaxLength(20);
                e.HasOne(x => x.Branch).WithMany(br => br.Accounts)
                 .HasForeignKey(x => x.BranchCode).OnDelete(DeleteBehavior.Restrict);
            });

            // Customer M - M Account via CustomerAccount (composite key)
            b.Entity<CustomerAccount>(e =>
            {
                e.HasKey(x => new { x.CustomerId, x.AccountNumber });
                e.Property(x => x.OwnershipType).HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.AccountStatus).HasConversion<string>().HasMaxLength(20);
                e.HasOne(x => x.Customer).WithMany(c => c.CustomerAccounts)
                 .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Account).WithMany(a => a.CustomerAccounts)
                 .HasForeignKey(x => x.AccountNumber).OnDelete(DeleteBehavior.Cascade);
            });

            // Account 1 - M Transaction
            b.Entity<Transaction>(e =>
            {
                e.HasKey(x => x.TransactionNumber);
                e.Property(x => x.Amount).HasPrecision(18, 2);
                e.Property(x => x.Note).HasMaxLength(250);
                e.Property(x => x.TransactionType).HasConversion<string>().HasMaxLength(20);
                e.HasOne(x => x.Account).WithMany(a => a.Transactions)
                 .HasForeignKey(x => x.AccountNumber).OnDelete(DeleteBehavior.Cascade);
            });

            // ---- Seeding (Manager first, then Branch) ----
            b.Entity<Manager>().HasData(
                new Manager { Id = 1, FullName = "Khaled Hassan", Email = "khaled@nbg.com", PhoneNumber = "01011111111", HireDate = new DateTime(2015, 3, 1) },
                new Manager { Id = 2, FullName = "Sara Mahmoud", Email = "sara@nbg.com", PhoneNumber = "01022222222", HireDate = new DateTime(2018, 9, 15) });

            b.Entity<Branch>().HasData(
                new Branch { Code = "CAI-01", Name = "Cairo Main Branch", Address = "Downtown, Cairo", PhoneNumber = "0225555555", ManagerId = 1 },
                new Branch { Code = "ALX-01", Name = "Alexandria Branch", Address = "Corniche, Alexandria", PhoneNumber = "0332222222", ManagerId = 2 });
        }
    }






