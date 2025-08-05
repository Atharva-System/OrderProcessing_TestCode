using OrderProcessing.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace OrderProcessing.Domain.ValueObjects;

public record CreditCardNumber : IValueObject
{
    private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes("MySecretKey12345"); // In real app, use secure key management

    public string Value { get; }
    public string MaskedValue => MaskCreditCard(Value);

    private CreditCardNumber(string value)
    {
        Value = value;
    }

    public static CreditCardNumber Create(string creditCardNumber)
    {
        if (string.IsNullOrWhiteSpace(creditCardNumber))
            throw new ArgumentException("Credit card number cannot be null or empty", nameof(creditCardNumber));

        var cleaned = creditCardNumber.Replace("-", "").Replace(" ", "");
        
        if (!IsValidCreditCardNumber(cleaned))
            throw new ArgumentException("Invalid credit card number format", nameof(creditCardNumber));

        // In production, encrypt the credit card number
        var encrypted = EncryptCreditCard(creditCardNumber);
        return new CreditCardNumber(encrypted);
    }

    private static string EncryptCreditCard(string creditCard)
    {
        // Simple encryption for demo - use proper encryption in production
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(creditCard));
    }

    private static string MaskCreditCard(string encryptedCard)
    {
        try
        {
            var decrypted = Encoding.UTF8.GetString(Convert.FromBase64String(encryptedCard));
            if (decrypted.Length < 4) return "****";
            return "****-****-****-" + decrypted.Substring(decrypted.Length - 4);
        }
        catch
        {
            return "****-****-****-****";
        }
    }

    private static bool IsValidCreditCardNumber(string number)
    {
        if (string.IsNullOrEmpty(number) || number.Length < 13 || number.Length > 19)
            return false;

        return number.All(char.IsDigit) && IsValidLuhn(number);
    }

    private static bool IsValidLuhn(string number)
    {
        int sum = 0;
        bool alternate = false;
        
        for (int i = number.Length - 1; i >= 0; i--)
        {
            int n = int.Parse(number[i].ToString());
            if (alternate)
            {
                n *= 2;
                if (n > 9) n = (n % 10) + 1;
            }
            sum += n;
            alternate = !alternate;
        }
        
        return sum % 10 == 0;
    }
}