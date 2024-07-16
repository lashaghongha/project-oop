using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace ATM_Console
{
    public interface IAccountOperations
    {
        void CheckBalance();
        void WithdrawAmount(decimal amount);
        void DepositAmount(decimal amount);
        void ChangePIN(string newPin);
        void ChangeCurrencyDisplay(int currencyChoice, decimal amountToConvert);
    }

    public interface IUserRepository
    {
        User LoadUserData();
        void SaveUserData(User user);
    }

    public class UserRepository : IUserRepository
    {
        private readonly string _jsonFilePath;

        public UserRepository(string path)
        {
            _jsonFilePath = path;
        }

        public User LoadUserData()
        {
            try
            {
                var jsonString = File.ReadAllText(_jsonFilePath);
                return JsonConvert.DeserializeObject<User>(jsonString);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Error: User data file not found: " + _jsonFilePath);
            }
            catch (JsonException)
            {
                Console.WriteLine("Error: User data is not in the correct format: " + _jsonFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
            return null;
        }

        public void SaveUserData(User user)
        {
            try
            {
                string updatedJson = JsonConvert.SerializeObject(user, Formatting.Indented);
                File.WriteAllText(_jsonFilePath, updatedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while saving data: {ex.Message}");
            }
        }
    }

    public class CardDetails
    {
        public string CardNumber { get; set; }
        public string ExpirationDate { get; set; }
        public string CVC { get; set; }
        public decimal Balance { get; set; }
    }

    public class Transaction
    {
        public string TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountUSD { get; set; }
        public decimal AmountEUR { get; set; }
    }

    public class User : IAccountOperations
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public CardDetails CardDetails { get; set; }
        public string PinCode { get; set; }
        public decimal Balance { get; set; }
        public decimal BalanceUSD { get; set; }
        public decimal BalanceEUR { get; set; }
        public List<Transaction> TransactionHistory { get; set; } = new List<Transaction>();

        public void CheckBalance()
        {
            Console.WriteLine($"Balance: {Balance}");
            Console.WriteLine($"BalanceUSD: {BalanceUSD}");
            Console.WriteLine($"BalanceEUR: {BalanceEUR}");
            AddTransaction("Balance Inquiry", 0);
        }

        public void WithdrawAmount(decimal amount)
        {
            if (amount > Balance)
            {
                Console.WriteLine("Insufficient balance.");
            }
            else
            {
                Balance -= amount;
                Console.WriteLine($"Withdrawal successful. New balance: {Balance}");
                AddTransaction("Withdrawal", amount);
            }
        }

        public void DepositAmount(decimal amount)
        {
            Balance += amount;
            Console.WriteLine($"Deposit successful. New balance: {Balance}");
            AddTransaction("Deposit", amount);
        }

        public void ChangePIN(string newPin)
        {
            PinCode = newPin;
            Console.WriteLine("PIN changed successfully.");
            AddTransaction("Change PIN", 0);
        }

        public void ChangeCurrencyDisplay(int currencyChoice, decimal amountToConvert)
        {
            decimal conversionRate = currencyChoice == 1 ? 2.6m : 2.9m;
            decimal convertedAmount = ConvertCurrency(amountToConvert, conversionRate);

            if (currencyChoice == 1)
            {
                BalanceUSD += convertedAmount;
                Balance -= amountToConvert;
                Console.WriteLine($"Converted to USD. New USD balance: {BalanceUSD}");
            }
            else if (currencyChoice == 2)
            {
                BalanceEUR += convertedAmount;
                Balance -= amountToConvert;
                Console.WriteLine($"Converted to EUR. New EUR balance: {BalanceEUR}");
            }

            AddTransaction($"Converted to {(currencyChoice == 1 ? "USD" : "EUR")}", convertedAmount);
        }

        private void AddTransaction(string transactionType, decimal amount)
        {
            TransactionHistory.Add(new Transaction
            {
                TransactionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                TransactionType = transactionType,
                Amount = amount
            });
        }

        private decimal ConvertCurrency(decimal amount, decimal conversionRate)
        {
            return amount / conversionRate;
        }
    }

    internal class Program
    {
        private static readonly string JsonFilePath = @"C:\Users\user\Desktop\projectl-oop\projectl-oop\UserData.json";

        static void Main(string[] args)
        {
            IUserRepository userRepository = new UserRepository(JsonFilePath);
            User user = userRepository.LoadUserData();
            if (user == null) return;

            while (true)
            {
                try
                {
                    Console.WriteLine("Please enter your card number:");
                    string enteredCardNumber = Console.ReadLine();

                    Console.WriteLine("Please enter your card CVC:");
                    string enteredCVC = Console.ReadLine();

                    Console.WriteLine("Please enter your card expiration date (MM/YY):");
                    string enteredExpirationDate = Console.ReadLine();

                    if (ValidateCardDetails(enteredCardNumber, enteredCVC, enteredExpirationDate, user.CardDetails))
                    {
                        Console.WriteLine("Please enter your PIN:");
                        string enteredPin = Console.ReadLine();
                        if (enteredPin == user.PinCode)
                        {
                            ShowMenu(user);
                        }
                        else
                        {
                            Console.WriteLine("Invalid PIN.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid card details.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                }
            }
        }

        private static bool ValidateCardDetails(string enteredCardNumber, string enteredCVC, string enteredExpirationDate, CardDetails cardDetails)
        {
            return CleanNumericInput(enteredCardNumber) == CleanNumericInput(cardDetails.CardNumber) &&
                   CleanNumericInput(enteredExpirationDate) == CleanNumericInput(cardDetails.ExpirationDate) &&
                   CleanNumericInput(enteredCVC) == CleanNumericInput(cardDetails.CVC);
        }

        private static string CleanNumericInput(string input)
        {
            return Regex.Replace(input, "[^0-9]", "");
        }

        private static void ShowMenu(User user)
        {
            bool exitMenu = false;
            while (!exitMenu)
            {
                Console.WriteLine("\nSelect an option:");
                Console.WriteLine("1. Check Balance");
                Console.WriteLine("2. Withdraw Amount");
                Console.WriteLine("3. Deposit Amount");
                Console.WriteLine("4. Change PIN");
                Console.WriteLine("5. Convert Currency");
                Console.WriteLine("6. Exit");

                switch (Console.ReadLine())
                {
                    case "1":
                        user.CheckBalance();
                        break;
                    case "2":
                        user.WithdrawAmount(ValidateAmount("withdraw"));
                        break;
                    case "3":
                        user.DepositAmount(ValidateAmount("deposit"));
                        break;
                    case "4":
                        Console.WriteLine("Enter your new PIN:");
                        user.ChangePIN(Console.ReadLine());
                        break;
                    case "5":
                        ChangeCurrency(user);
                        break;
                    case "6":
                        exitMenu = true;
                        DrawGeorgianFlag();
                        break;
                    default:
                        Console.WriteLine("Invalid option, please try again.");
                        break;
                }

                IUserRepository userRepository = new UserRepository(JsonFilePath);
                userRepository.SaveUserData(user);
            }
        }

        private static decimal ValidateAmount(string action)
        {
            decimal amount;
            while (true)
            {
                Console.WriteLine($"Enter the amount to {action}:");
                if (Decimal.TryParse(Console.ReadLine(), out amount) && amount > 0)
                {
                    return amount;
                }
                else
                {
                    Console.WriteLine("Invalid amount. Please enter a positive number.");
                }
            }
        }

        private static void ChangeCurrency(User user)
        {
            Console.WriteLine("Select currency to convert to:");
            Console.WriteLine("1. Convert to USD");
            Console.WriteLine("2. Convert to EUR");

            if (int.TryParse(Console.ReadLine(), out int currencyChoice) && (currencyChoice == 1 || currencyChoice == 2))
            {
                Console.WriteLine("Enter the amount to convert:");
                if (decimal.TryParse(Console.ReadLine(), out decimal amountToConvert) && amountToConvert > 0 && amountToConvert <= user.Balance)
                {
                    user.ChangeCurrencyDisplay(currencyChoice, amountToConvert);
                }
                else
                {
                    Console.WriteLine("Invalid amount. Please enter a positive number and ensure it does not exceed your current balance.");
                }
            }
            else
            {
                Console.WriteLine("Invalid currency choice. Please try again.");
            }
        }

        private static void DrawGeorgianFlag()
        {
            Console.WriteLine(@"
            _________________ 
            |       |       |
            |   +   |   +   |
            |_______|_______|
            |       |       |
            |   +   |   +   |
            |_______|_______|
            ");
            Console.WriteLine("Georgia will win against Spain 2-1");
        }
    }
}
