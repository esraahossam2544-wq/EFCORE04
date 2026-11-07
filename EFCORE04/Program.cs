using EFCORE04.Context;
using EFCORE04.Models;
using System.Net.Mail;

using var db = new BankContext();
  

while (true)
{
    Console.Clear();
    Console.WriteLine("========================================");
    Console.WriteLine("    National Bank - Management");
    Console.WriteLine("========================================");
    Console.WriteLine("  1) Add a new Customer");
    Console.WriteLine("  2) Open a new Account for a Customer");
    Console.WriteLine("  3) Update Account Status (Active / Closed)");
    Console.WriteLine("  4) Remove an Account from a Customer");
    Console.WriteLine("  5) List all Customers (with accounts)");
    Console.WriteLine("  0) Exit");
    Console.WriteLine("----------------------------------------");
    Console.Write("  Enter choice: ");

    var choice = Console.ReadLine()?.Trim();
    if (choice == "0") break;

    try
    {
        switch (choice)
        {
            case "1": AddCustomer(); break;
            case "2": OpenAccount(); break;
            case "3": UpdateStatus(); break;
            case "4": RemoveAccount(); break;
            case "5": ListCustomers(); break;
            default: Print("Invalid choice, try again.", ConsoleColor.Red); break;
        }
    }
    catch (Exception ex)
    {
        Print($"Unexpected error: {ex.Message}", ConsoleColor.Red);
    }

    Console.WriteLine("\nPress any key to return to the menu...");
    Console.ReadKey(true);
}
db.Dispose();   // close the connection
Console.WriteLine("Goodbye!");

// ===================== Operations =====================
void AddCustomer()
{
    Console.WriteLine("\n--- Add New Customer ---");
    var name = ReadString("Full Name      ", s => s.Length >= 3, "Name must be at least 3 characters.");
    var nid = ReadString("National ID    ", s => s.Length == 14 && s.All(char.IsDigit), "National ID must be exactly 14 digits.");
    if (db.Customers.Any(c => c.NationalId == nid)) { Print("A customer with this National ID already exists.", ConsoleColor.Red); return; }
    var dob = ReadDate("Date of Birth  (yyyy-MM-dd)");
    var email = ReadString("Email          ", IsValidEmail, "Invalid email format.");
    var phone = ReadString("Phone          ", s => s.Length is >= 10 and <= 15 && s.All(char.IsDigit), "Phone must be 10-15 digits.");
    var address = ReadString("Address        ", s => s.Length >= 5, "Address is too short.");
    var type = (CustomerType)ReadChoice("Customer Type:", "Individual", "Business");

    var customer = new Customer
    {
        FullName = name,
        NationalId = nid,
        DateOfBirth = dob,
        Email = email,
        PhoneNumber = phone,
        Address = address,
        CustomerType = type
    };
    db.Customers.Add(customer);
    db.SaveChanges();
    Print($"\nCustomer created successfully. CustomerId = {customer.Id}", ConsoleColor.Green);
}

void OpenAccount()
{
    Console.WriteLine("\n--- Open New Account ---");
    var accNo = ReadString("Account Number ", s => s.Length is >= 4 and <= 20 && s.All(char.IsLetterOrDigit), "Account number must be 4-20 letters/digits.");
    var accType = (AccountType)ReadChoice("Account Type:", "Savings", "Current", "Business");
    var branchCode = ReadString("Branch Code    ", s => s.Length > 0, "Branch code is required.").ToUpper();
    var customerId = ReadInt("Customer Id     ");
    var role = (OwnershipType)ReadChoice("Ownership Role:", "Primary", "CoHolder");

    Print($"Validating branch '{branchCode}' and customer #{customerId}...", ConsoleColor.Cyan);

    if (!db.Branches.Any(b => b.Code == branchCode)) { Print("Branch not found.", ConsoleColor.Red); return; }
    if (!db.Customers.Any(c => c.Id == customerId)) { Print("Customer not found.", ConsoleColor.Red); return; }

    var account = db.Accounts.Include(a => a.CustomerAccounts).FirstOrDefault(a => a.AccountNumber == accNo);
    if (account == null)
    {
        account = new Account { AccountNumber = accNo, AccountType = accType, BranchCode = branchCode, OpeningDate = DateTime.Today, CurrentBalance = 0 };
        db.Accounts.Add(account);
    }
    else
    {
        if (account.CustomerAccounts.Any(ca => ca.CustomerId == customerId))
        { Print("This customer is already linked to this account.", ConsoleColor.Red); return; }
        if (role == OwnershipType.Primary && account.CustomerAccounts.Any(ca => ca.OwnershipType == OwnershipType.Primary))
        { Print("This account already has a Primary owner.", ConsoleColor.Red); return; }
    }

    db.CustomerAccounts.Add(new CustomerAccount
    {
        CustomerId = customerId,
        Account = account,
        OwnershipType = role,
        OwnershipStartDate = DateTime.Today,
        AccountStatus = AccountStatus.Active
    });
    db.SaveChanges();
    Print($"Account '{accNo}' created and linked to customer {customerId} as {role} owner.", ConsoleColor.Green);
}

void UpdateStatus()
{
    Console.WriteLine("\n--- Update Account Status ---");
    var accNo = ReadString("Account Number ", s => s.Length > 0, "Required.");
    var customerId = ReadInt("Customer Id     ");
    var link = db.CustomerAccounts.FirstOrDefault(ca => ca.AccountNumber == accNo && ca.CustomerId == customerId);
    if (link == null) { Print("No such account/customer link.", ConsoleColor.Red); return; }

    var status = (AccountStatus)ReadChoice("New Status:", "Active", "Closed");
    link.AccountStatus = status;
    db.SaveChanges();
    Print($"Status updated to {status}.", ConsoleColor.Green);
}

void RemoveAccount()
{
    Console.WriteLine("\n--- Remove Account From Customer ---");
    var accNo = ReadString("Account Number ", s => s.Length > 0, "Required.");
    var customerId = ReadInt("Customer Id     ");

    var link = db.CustomerAccounts
        .Include(ca => ca.Account).ThenInclude(a => a.CustomerAccounts)
        .FirstOrDefault(ca => ca.AccountNumber == accNo && ca.CustomerId == customerId);
    if (link == null) { Print("No such account/customer link.", ConsoleColor.Red); return; }

    var account = link.Account;
    bool lastOwner = account.CustomerAccounts.Count == 1;

    db.CustomerAccounts.Remove(link);
    if (lastOwner) db.Accounts.Remove(account);   // transactions are cascade-deleted
    db.SaveChanges();

    Print("\n  Ownership link deleted.", ConsoleColor.Green);
    if (lastOwner) Print($"      That was the last owner - account '{accNo}' was also removed.", ConsoleColor.Cyan);
}

void ListCustomers()
{
    Console.WriteLine("\n--- All Customers ---\n");
    // Eager loading: Customer -> CustomerAccounts -> Account -> Branch
    var customers = db.Customers
        .Include(c => c.CustomerAccounts).ThenInclude(ca => ca.Account).ThenInclude(a => a.Branch)
        .AsNoTracking().OrderBy(c => c.Id).ToList();

    if (customers.Count == 0) { Console.WriteLine("  (no customers)"); return; }

    foreach (var c in customers)
    {
        Console.WriteLine($"  #{c.Id} {c.FullName} ({c.CustomerType})");
        if (c.CustomerAccounts.Count == 0) { Console.WriteLine("      (no accounts)"); continue; }
        foreach (var ca in c.CustomerAccounts)
        {
            var a = ca.Account;
            Console.WriteLine($"      {a.AccountNumber,-10} {a.AccountType,-9} Balance: {a.CurrentBalance,12:N2}   {ca.OwnershipType,-9} {ca.AccountStatus,-7} @ {a.Branch.Name}");
        }
    }
}

// ===================== Input helpers =====================
static void Print(string msg, ConsoleColor color)
{
    Console.ForegroundColor = color;
    Console.WriteLine(msg);
    Console.ResetColor();
}

static string ReadString(string label, Func<string, bool> valid, string error)
{
    while (true)
    {
        Console.Write($"{label}: ");
        var s = Console.ReadLine()?.Trim() ?? "";
        if (valid(s)) return s;
        Print(error, ConsoleColor.Red);
    }
}

static int ReadInt(string label)
{
    while (true)
    {
        Console.Write($"{label}: ");
        if (int.TryParse(Console.ReadLine(), out var n) && n > 0) return n;
        Print("Please enter a valid positive number.", ConsoleColor.Red);
    }
}

static DateTime ReadDate(string label)
{
    while (true)
    {
        Console.Write($"{label}: ");
        if (DateTime.TryParseExact(Console.ReadLine()?.Trim(), "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d)
            && d < DateTime.Today && d > DateTime.Today.AddYears(-120))
            return d;
        Print("Invalid date. Use yyyy-MM-dd and a past date.", ConsoleColor.Red);
    }
}

static int ReadChoice(string title, params string[] options)
{
    Console.WriteLine(title);
    for (int i = 0; i < options.Length; i++) Console.WriteLine($"    {i + 1}) {options[i]}");
    while (true)
    {
        Console.Write("  Choice: ");
        if (int.TryParse(Console.ReadLine(), out var n) && n >= 1 && n <= options.Length) return n;
        Print($"Please choose a number between 1 and {options.Length}.", ConsoleColor.Red);
    }
}

static bool IsValidEmail(string s)
{
    try { return new MailAddress(s).Address == s; } catch { return false; }
}



