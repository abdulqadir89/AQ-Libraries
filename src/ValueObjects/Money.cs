using System.Globalization;

namespace AQ.ValueObjects;

public sealed class Money : ValueObject
{
    private static readonly HashSet<string> ValidIsoCurrencies = new()
    {
        // Major World Currencies
        "USD", "EUR", "JPY", "GBP", "AUD", "CAD", "CHF", "CNY", "SEK", "NZD", "NOK", "DKK",
        
        // Asian Currencies
        "PKR", "INR", "KRW", "SGD", "HKD", "TWD", "THB", "MYR", "IDR", "PHP", "VND", "BDT",
        "LKR", "NPR", "AFN", "MMK", "KHR", "LAK", "BND", "MOP", "BTN", "MVR", "KGS", "UZS",
        "TMT", "TJS", "KZT", "AZN", "GEL", "AMD", "MNT",
        
        // Middle Eastern & West Asian Currencies
        "SAR", "AED", "QAR", "KWD", "BHD", "OMR", "JOD", "ILS", "TRY", "IRR", "IQD", "LBP",
        "SYP", "YER", "LYD", "DZD",
        
        // European Currencies (Non-Euro)
        "PLN", "CZK", "HUF", "RON", "BGN", "HRK", "RSD", "RUB", "UAH", "MKD", "ALL", "BAM",
        "MDL", "BYN", "ISK", "GBP", "CHF",
        
        // African Currencies
        "ZAR", "EGP", "NGN", "KES", "GHS", "MAD", "TND", "ETB", "UGX", "TZS", "ZMW", "BWP",
        "NAD", "SZL", "LSL", "MWK", "MZN", "AOA", "CFA", "XAF", "XOF", "GMD", "GNF", "LRD",
        "SLL", "CVE", "STN", "DJF", "ERN", "RWF", "BIF", "KMF", "SCR", "MUR", "MGA", "CDF",
        "XAG", "XAU", "XPD", "XPT",
        
        // Latin American Currencies
        "BRL", "MXN", "ARS", "CLP", "COP", "PEN", "UYU", "PYG", "BOB", "VES", "VED", "GTQ",
        "HNL", "NIO", "CRC", "PAB", "DOP", "HTG", "JMD", "CUP", "CUC", "BSD", "BBD", "BZD",
        "XCD", "AWG", "ANG", "SRD", "GYD", "FKP",
        
        // North American Currencies
        "USD", "CAD", "MXN",
        
        // Oceania & Pacific
        "AUD", "NZD", "FJD", "PGK", "WST", "TOP", "VUV", "SBD", "NCR", "XPF", "KID", "TVD",
        "NRU", "AUD",
        
        // Central Asian Currencies
        "KZT", "UZS", "KGS", "TJS", "TMT",
        
        // Caribbean Currencies
        "XCD", "JMD", "HTG", "DOP", "CUP", "CUC", "BSD", "BBD", "BZD", "KYD", "AWG", "ANG",
        "TTD", "GYD", "SRD",
        
        // Special & Precious Metals
        "XDR", "XAG", "XAU", "XPD", "XPT",
        
        // Cryptocurrencies (commonly accepted in financial systems)
        "BTC", "ETH", "USDT", "USDC", "BNB", "XRP", "ADA", "SOL", "DOGE", "AVAX", "DOT", "MATIC",
        
        // Historical/Legacy but still valid
        "ZWL", "VEF", "SDD", "CSD", "YUM", "SIT", "SKK", "EEK", "LVL", "LTL", "CYP", "MTL"
    };

    public decimal Value { get; init; }
    public string Currency { get; init; } = default!;

    // Parameterless constructor for EF Core
    private Money() { }
    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be null or empty.", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code (e.g., USD, PKR).", nameof(currency));

        var normalizedCurrency = currency.ToUpperInvariant();

        if (!ValidIsoCurrencies.Contains(normalizedCurrency))
            throw new ArgumentException($"Invalid currency code '{currency}'. Must be a valid ISO 4217 currency code.", nameof(currency));

        Value = amount;
        Currency = normalizedCurrency;
    }

    public static Money Zero(string currency) => new(0, currency);

    public static Money Create(decimal amount, string currency) => new(amount, currency);

    /// <summary>
    /// Validates if the provided currency code is a valid ISO 4217 currency
    /// </summary>
    /// <param name="currency">The currency code to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return false;

        return ValidIsoCurrencies.Contains(currency.ToUpperInvariant());
    }

    /// <summary>
    /// Gets all supported ISO 4217 currency codes
    /// </summary>
    /// <returns>Read-only collection of supported currency codes</returns>
    public static IReadOnlySet<string> GetSupportedCurrencies() => ValidIsoCurrencies;

    /// <summary>
    /// Creates a Money instance with validation, returning null if currency is invalid
    /// </summary>
    /// <param name="amount">The monetary amount</param>
    /// <param name="currency">The currency code</param>
    /// <returns>Money instance if valid currency, null otherwise</returns>
    public static Money? TryCreate(decimal amount, string currency)
    {
        try
        {
            return new Money(amount, currency);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add money with different currencies: {Currency} and {other.Currency}");

        return new Money(Value + other.Value, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract money with different currencies: {Currency} and {other.Currency}");

        return new Money(Value - other.Value, Currency);
    }

    public Money Multiply(decimal multiplier) => new(Value * multiplier, Currency);

    public Money Divide(decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide money by zero.");

        return new Money(Value / divisor, Currency);
    }

    public bool IsZero => Value == 0;

    public bool IsPositive => Value > 0;

    public bool IsNegative => Value < 0;

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static Money operator *(Money money, decimal multiplier) => money.Multiply(multiplier);

    public static Money operator *(decimal multiplier, Money money) => money.Multiply(multiplier);

    public static Money operator /(Money money, decimal divisor) => money.Divide(divisor);

    public static bool operator >(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot compare money with different currencies: {left.Currency} and {right.Currency}");

        return left.Value > right.Value;
    }

    public static bool operator <(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot compare money with different currencies: {left.Currency} and {right.Currency}");

        return left.Value < right.Value;
    }

    public static bool operator >=(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot compare money with different currencies: {left.Currency} and {right.Currency}");

        return left.Value >= right.Value;
    }

    public static bool operator <=(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot compare money with different currencies: {left.Currency} and {right.Currency}");

        return left.Value <= right.Value;
    }

    public override string ToString() => $"{Value:F2} {Currency}";

    public string ToString(string format) => $"{Value.ToString(format, CultureInfo.InvariantCulture)} {Currency}";

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
        yield return Currency;
    }

    // Clone method to create a copy of the Money instance
    public override Money Clone()
    {
        return new Money(Value, Currency);
    }

    // ISO 4217 Currency factory methods
    public static class Currencies
    {
        // Major World Currencies
        public static Money USD(decimal amount) => new(amount, "USD"); // United States Dollar
        public static Money EUR(decimal amount) => new(amount, "EUR"); // Euro
        public static Money JPY(decimal amount) => new(amount, "JPY"); // Japanese Yen
        public static Money GBP(decimal amount) => new(amount, "GBP"); // British Pound Sterling
        public static Money AUD(decimal amount) => new(amount, "AUD"); // Australian Dollar
        public static Money CAD(decimal amount) => new(amount, "CAD"); // Canadian Dollar
        public static Money CHF(decimal amount) => new(amount, "CHF"); // Swiss Franc
        public static Money CNY(decimal amount) => new(amount, "CNY"); // Chinese Yuan
        public static Money SEK(decimal amount) => new(amount, "SEK"); // Swedish Krona
        public static Money NZD(decimal amount) => new(amount, "NZD"); // New Zealand Dollar

        // Asian Currencies
        public static Money PKR(decimal amount) => new(amount, "PKR"); // Pakistani Rupee
        public static Money INR(decimal amount) => new(amount, "INR"); // Indian Rupee
        public static Money KRW(decimal amount) => new(amount, "KRW"); // South Korean Won
        public static Money SGD(decimal amount) => new(amount, "SGD"); // Singapore Dollar
        public static Money HKD(decimal amount) => new(amount, "HKD"); // Hong Kong Dollar
        public static Money TWD(decimal amount) => new(amount, "TWD"); // Taiwan Dollar
        public static Money THB(decimal amount) => new(amount, "THB"); // Thai Baht
        public static Money MYR(decimal amount) => new(amount, "MYR"); // Malaysian Ringgit
        public static Money IDR(decimal amount) => new(amount, "IDR"); // Indonesian Rupiah
        public static Money PHP(decimal amount) => new(amount, "PHP"); // Philippine Peso
        public static Money VND(decimal amount) => new(amount, "VND"); // Vietnamese Dong
        public static Money BDT(decimal amount) => new(amount, "BDT"); // Bangladeshi Taka
        public static Money LKR(decimal amount) => new(amount, "LKR"); // Sri Lankan Rupee
        public static Money NPR(decimal amount) => new(amount, "NPR"); // Nepalese Rupee
        public static Money AFN(decimal amount) => new(amount, "AFN"); // Afghan Afghani

        // Middle Eastern Currencies
        public static Money SAR(decimal amount) => new(amount, "SAR"); // Saudi Riyal
        public static Money AED(decimal amount) => new(amount, "AED"); // UAE Dirham
        public static Money QAR(decimal amount) => new(amount, "QAR"); // Qatari Riyal
        public static Money KWD(decimal amount) => new(amount, "KWD"); // Kuwaiti Dinar
        public static Money BHD(decimal amount) => new(amount, "BHD"); // Bahraini Dinar
        public static Money OMR(decimal amount) => new(amount, "OMR"); // Omani Rial
        public static Money JOD(decimal amount) => new(amount, "JOD"); // Jordanian Dinar
        public static Money ILS(decimal amount) => new(amount, "ILS"); // Israeli Shekel
        public static Money TRY(decimal amount) => new(amount, "TRY"); // Turkish Lira
        public static Money IRR(decimal amount) => new(amount, "IRR"); // Iranian Rial

        // European Currencies (Non-Euro)
        public static Money NOK(decimal amount) => new(amount, "NOK"); // Norwegian Krone
        public static Money DKK(decimal amount) => new(amount, "DKK"); // Danish Krone
        public static Money PLN(decimal amount) => new(amount, "PLN"); // Polish Zloty
        public static Money CZK(decimal amount) => new(amount, "CZK"); // Czech Koruna
        public static Money HUF(decimal amount) => new(amount, "HUF"); // Hungarian Forint
        public static Money RON(decimal amount) => new(amount, "RON"); // Romanian Leu
        public static Money BGN(decimal amount) => new(amount, "BGN"); // Bulgarian Lev
        public static Money HRK(decimal amount) => new(amount, "HRK"); // Croatian Kuna
        public static Money RSD(decimal amount) => new(amount, "RSD"); // Serbian Dinar
        public static Money RUB(decimal amount) => new(amount, "RUB"); // Russian Ruble
        public static Money UAH(decimal amount) => new(amount, "UAH"); // Ukrainian Hryvnia

        // African Currencies
        public static Money ZAR(decimal amount) => new(amount, "ZAR"); // South African Rand
        public static Money EGP(decimal amount) => new(amount, "EGP"); // Egyptian Pound
        public static Money NGN(decimal amount) => new(amount, "NGN"); // Nigerian Naira
        public static Money KES(decimal amount) => new(amount, "KES"); // Kenyan Shilling
        public static Money GHS(decimal amount) => new(amount, "GHS"); // Ghanaian Cedi
        public static Money MAD(decimal amount) => new(amount, "MAD"); // Moroccan Dirham
        public static Money TND(decimal amount) => new(amount, "TND"); // Tunisian Dinar
        public static Money ETB(decimal amount) => new(amount, "ETB"); // Ethiopian Birr
        public static Money UGX(decimal amount) => new(amount, "UGX"); // Ugandan Shilling
        public static Money TZS(decimal amount) => new(amount, "TZS"); // Tanzanian Shilling

        // Latin American Currencies
        public static Money BRL(decimal amount) => new(amount, "BRL"); // Brazilian Real
        public static Money MXN(decimal amount) => new(amount, "MXN"); // Mexican Peso
        public static Money ARS(decimal amount) => new(amount, "ARS"); // Argentine Peso
        public static Money CLP(decimal amount) => new(amount, "CLP"); // Chilean Peso
        public static Money COP(decimal amount) => new(amount, "COP"); // Colombian Peso
        public static Money PEN(decimal amount) => new(amount, "PEN"); // Peruvian Sol
        public static Money UYU(decimal amount) => new(amount, "UYU"); // Uruguayan Peso
        public static Money PYG(decimal amount) => new(amount, "PYG"); // Paraguayan Guarani
        public static Money BOB(decimal amount) => new(amount, "BOB"); // Bolivian Boliviano
        public static Money VES(decimal amount) => new(amount, "VES"); // Venezuelan Bolívar

        // Oceania & Other
        public static Money FJD(decimal amount) => new(amount, "FJD"); // Fijian Dollar
        public static Money PGK(decimal amount) => new(amount, "PGK"); // Papua New Guinea Kina
        public static Money WST(decimal amount) => new(amount, "WST"); // Samoan Tala
        public static Money TOP(decimal amount) => new(amount, "TOP"); // Tongan Paʻanga

        // Cryptocurrencies (commonly used)
        public static Money BTC(decimal amount) => new(amount, "BTC"); // Bitcoin
        public static Money ETH(decimal amount) => new(amount, "ETH"); // Ethereum
        public static Money USDT(decimal amount) => new(amount, "USDT"); // Tether
        public static Money USDC(decimal amount) => new(amount, "USDC"); // USD Coin

        // Special Drawing Rights
        public static Money XDR(decimal amount) => new(amount, "XDR"); // IMF Special Drawing Rights
    }
}
