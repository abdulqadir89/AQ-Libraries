using System.Text.Json;
using System.Text.Json.Serialization;

namespace AQ.ValueObjects;

/// <summary>
/// Represents a country value object with ISO 3166-1 alpha-2 code.
/// Only the ISO code is stored; name and currency are computed from the hardcoded dictionary.
/// </summary>
public sealed class Country : ValueObject
{
    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g., "US", "PK", "GB").
    /// This is the only property stored in the database.
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// Display name of the country (computed from ISO code).
    /// </summary>
    public string Name => Countries.TryGetValue(Value, out var data) ? data.Name : Value;

    /// <summary>
    /// Default currency for this country (ISO 4217 code, computed from ISO code).
    /// </summary>
    public string Currency => Countries.TryGetValue(Value, out var data) ? data.Currency : "USD";

    // Parameterless constructor for EF Core
    private Country()
    {
        Value = default!;
    }

    private Country(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a Country instance from an ISO 3166-1 alpha-2 code.
    /// </summary>
    /// <param name="code">The 2-letter ISO country code.</param>
    /// <returns>A Country instance if the code is valid.</returns>
    /// <exception cref="ArgumentException">Thrown when the country code is invalid.</exception>
    public static Country FromCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Country code cannot be null or empty.", nameof(code));

        var normalizedCode = code.ToUpperInvariant();

        if (!Countries.ContainsKey(normalizedCode))
            throw new ArgumentException($"Invalid country code: {code}. Must be a valid ISO 3166-1 alpha-2 code.", nameof(code));

        return new Country(normalizedCode);
    }

    /// <summary>
    /// Attempts to create a Country instance without throwing an exception.
    /// </summary>
    /// <param name="code">The 2-letter ISO country code.</param>
    /// <param name="result">The created Country instance if successful.</param>
    /// <returns>True if the country code is valid, false otherwise.</returns>
    public static bool TryFromCode(string code, out Country? result)
    {
        result = null;

        try
        {
            result = FromCode(code);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets all available countries.
    /// </summary>
    /// <returns>Collection of all Country instances.</returns>
    public static IEnumerable<Country> GetAll()
    {
        return Countries.Keys.Select(code => new Country(code));
    }

    /// <summary>
    /// Validates if a country code is valid.
    /// </summary>
    /// <param name="code">The country code to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 2)
            return false;

        return Countries.ContainsKey(code.ToUpperInvariant());
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => $"{Name} ({Value})";

    public override Country Clone()
    {
        return new Country(Value);
    }

    /// <summary>
    /// Complete list of ISO 3166-1 alpha-2 countries with their default currencies
    /// </summary>
    private static readonly Dictionary<string, (string Name, string Currency)> Countries = new()
    {
        // A
        { "AD", ("Andorra", "EUR") },
        { "AE", ("United Arab Emirates", "AED") },
        { "AF", ("Afghanistan", "AFN") },
        { "AG", ("Antigua and Barbuda", "XCD") },
        { "AI", ("Anguilla", "XCD") },
        { "AL", ("Albania", "ALL") },
        { "AM", ("Armenia", "AMD") },
        { "AO", ("Angola", "AOA") },
        { "AQ", ("Antarctica", "USD") },
        { "AR", ("Argentina", "ARS") },
        { "AS", ("American Samoa", "USD") },
        { "AT", ("Austria", "EUR") },
        { "AU", ("Australia", "AUD") },
        { "AW", ("Aruba", "AWG") },
        { "AX", ("Åland Islands", "EUR") },
        { "AZ", ("Azerbaijan", "AZN") },

        // B
        { "BA", ("Bosnia and Herzegovina", "BAM") },
        { "BB", ("Barbados", "BBD") },
        { "BD", ("Bangladesh", "BDT") },
        { "BE", ("Belgium", "EUR") },
        { "BF", ("Burkina Faso", "XOF") },
        { "BG", ("Bulgaria", "BGN") },
        { "BH", ("Bahrain", "BHD") },
        { "BI", ("Burundi", "BIF") },
        { "BJ", ("Benin", "XOF") },
        { "BL", ("Saint Barthélemy", "EUR") },
        { "BM", ("Bermuda", "BMD") },
        { "BN", ("Brunei", "BND") },
        { "BO", ("Bolivia", "BOB") },
        { "BQ", ("Caribbean Netherlands", "USD") },
        { "BR", ("Brazil", "BRL") },
        { "BS", ("Bahamas", "BSD") },
        { "BT", ("Bhutan", "BTN") },
        { "BV", ("Bouvet Island", "NOK") },
        { "BW", ("Botswana", "BWP") },
        { "BY", ("Belarus", "BYN") },
        { "BZ", ("Belize", "BZD") },

        // C
        { "CA", ("Canada", "CAD") },
        { "CC", ("Cocos (Keeling) Islands", "AUD") },
        { "CD", ("Democratic Republic of the Congo", "CDF") },
        { "CF", ("Central African Republic", "XAF") },
        { "CG", ("Republic of the Congo", "XAF") },
        { "CH", ("Switzerland", "CHF") },
        { "CI", ("Côte d'Ivoire", "XOF") },
        { "CK", ("Cook Islands", "NZD") },
        { "CL", ("Chile", "CLP") },
        { "CM", ("Cameroon", "XAF") },
        { "CN", ("China", "CNY") },
        { "CO", ("Colombia", "COP") },
        { "CR", ("Costa Rica", "CRC") },
        { "CU", ("Cuba", "CUP") },
        { "CV", ("Cabo Verde", "CVE") },
        { "CW", ("Curaçao", "ANG") },
        { "CX", ("Christmas Island", "AUD") },
        { "CY", ("Cyprus", "EUR") },
        { "CZ", ("Czech Republic", "CZK") },

        // D
        { "DE", ("Germany", "EUR") },
        { "DJ", ("Djibouti", "DJF") },
        { "DK", ("Denmark", "DKK") },
        { "DM", ("Dominica", "XCD") },
        { "DO", ("Dominican Republic", "DOP") },
        { "DZ", ("Algeria", "DZD") },

        // E
        { "EC", ("Ecuador", "USD") },
        { "EE", ("Estonia", "EUR") },
        { "EG", ("Egypt", "EGP") },
        { "EH", ("Western Sahara", "MAD") },
        { "ER", ("Eritrea", "ERN") },
        { "ES", ("Spain", "EUR") },
        { "ET", ("Ethiopia", "ETB") },

        // F
        { "FI", ("Finland", "EUR") },
        { "FJ", ("Fiji", "FJD") },
        { "FK", ("Falkland Islands", "FKP") },
        { "FM", ("Micronesia", "USD") },
        { "FO", ("Faroe Islands", "DKK") },
        { "FR", ("France", "EUR") },

        // G
        { "GA", ("Gabon", "XAF") },
        { "GB", ("United Kingdom", "GBP") },
        { "GD", ("Grenada", "XCD") },
        { "GE", ("Georgia", "GEL") },
        { "GF", ("French Guiana", "EUR") },
        { "GG", ("Guernsey", "GBP") },
        { "GH", ("Ghana", "GHS") },
        { "GI", ("Gibraltar", "GIP") },
        { "GL", ("Greenland", "DKK") },
        { "GM", ("Gambia", "GMD") },
        { "GN", ("Guinea", "GNF") },
        { "GP", ("Guadeloupe", "EUR") },
        { "GQ", ("Equatorial Guinea", "XAF") },
        { "GR", ("Greece", "EUR") },
        { "GS", ("South Georgia and the South Sandwich Islands", "GBP") },
        { "GT", ("Guatemala", "GTQ") },
        { "GU", ("Guam", "USD") },
        { "GW", ("Guinea-Bissau", "XOF") },
        { "GY", ("Guyana", "GYD") },

        // H
        { "HK", ("Hong Kong", "HKD") },
        { "HM", ("Heard Island and McDonald Islands", "AUD") },
        { "HN", ("Honduras", "HNL") },
        { "HR", ("Croatia", "EUR") },
        { "HT", ("Haiti", "HTG") },
        { "HU", ("Hungary", "HUF") },

        // I
        { "ID", ("Indonesia", "IDR") },
        { "IE", ("Ireland", "EUR") },
        { "IL", ("Israel", "ILS") },
        { "IM", ("Isle of Man", "GBP") },
        { "IN", ("India", "INR") },
        { "IO", ("British Indian Ocean Territory", "USD") },
        { "IQ", ("Iraq", "IQD") },
        { "IR", ("Iran", "IRR") },
        { "IS", ("Iceland", "ISK") },
        { "IT", ("Italy", "EUR") },

        // J
        { "JE", ("Jersey", "GBP") },
        { "JM", ("Jamaica", "JMD") },
        { "JO", ("Jordan", "JOD") },
        { "JP", ("Japan", "JPY") },

        // K
        { "KE", ("Kenya", "KES") },
        { "KG", ("Kyrgyzstan", "KGS") },
        { "KH", ("Cambodia", "KHR") },
        { "KI", ("Kiribati", "AUD") },
        { "KM", ("Comoros", "KMF") },
        { "KN", ("Saint Kitts and Nevis", "XCD") },
        { "KP", ("North Korea", "KPW") },
        { "KR", ("South Korea", "KRW") },
        { "KW", ("Kuwait", "KWD") },
        { "KY", ("Cayman Islands", "KYD") },
        { "KZ", ("Kazakhstan", "KZT") },

        // L
        { "LA", ("Laos", "LAK") },
        { "LB", ("Lebanon", "LBP") },
        { "LC", ("Saint Lucia", "XCD") },
        { "LI", ("Liechtenstein", "CHF") },
        { "LK", ("Sri Lanka", "LKR") },
        { "LR", ("Liberia", "LRD") },
        { "LS", ("Lesotho", "LSL") },
        { "LT", ("Lithuania", "EUR") },
        { "LU", ("Luxembourg", "EUR") },
        { "LV", ("Latvia", "EUR") },
        { "LY", ("Libya", "LYD") },

        // M
        { "MA", ("Morocco", "MAD") },
        { "MC", ("Monaco", "EUR") },
        { "MD", ("Moldova", "MDL") },
        { "ME", ("Montenegro", "EUR") },
        { "MF", ("Saint Martin", "EUR") },
        { "MG", ("Madagascar", "MGA") },
        { "MH", ("Marshall Islands", "USD") },
        { "MK", ("North Macedonia", "MKD") },
        { "ML", ("Mali", "XOF") },
        { "MM", ("Myanmar", "MMK") },
        { "MN", ("Mongolia", "MNT") },
        { "MO", ("Macao", "MOP") },
        { "MP", ("Northern Mariana Islands", "USD") },
        { "MQ", ("Martinique", "EUR") },
        { "MR", ("Mauritania", "MRU") },
        { "MS", ("Montserrat", "XCD") },
        { "MT", ("Malta", "EUR") },
        { "MU", ("Mauritius", "MUR") },
        { "MV", ("Maldives", "MVR") },
        { "MW", ("Malawi", "MWK") },
        { "MX", ("Mexico", "MXN") },
        { "MY", ("Malaysia", "MYR") },
        { "MZ", ("Mozambique", "MZN") },

        // N
        { "NA", ("Namibia", "NAD") },
        { "NC", ("New Caledonia", "XPF") },
        { "NE", ("Niger", "XOF") },
        { "NF", ("Norfolk Island", "AUD") },
        { "NG", ("Nigeria", "NGN") },
        { "NI", ("Nicaragua", "NIO") },
        { "NL", ("Netherlands", "EUR") },
        { "NO", ("Norway", "NOK") },
        { "NP", ("Nepal", "NPR") },
        { "NR", ("Nauru", "AUD") },
        { "NU", ("Niue", "NZD") },
        { "NZ", ("New Zealand", "NZD") },

        // O
        { "OM", ("Oman", "OMR") },

        // P
        { "PA", ("Panama", "PAB") },
        { "PE", ("Peru", "PEN") },
        { "PF", ("French Polynesia", "XPF") },
        { "PG", ("Papua New Guinea", "PGK") },
        { "PH", ("Philippines", "PHP") },
        { "PK", ("Pakistan", "PKR") },
        { "PL", ("Poland", "PLN") },
        { "PM", ("Saint Pierre and Miquelon", "EUR") },
        { "PN", ("Pitcairn Islands", "NZD") },
        { "PR", ("Puerto Rico", "USD") },
        { "PS", ("Palestine", "ILS") },
        { "PT", ("Portugal", "EUR") },
        { "PW", ("Palau", "USD") },
        { "PY", ("Paraguay", "PYG") },

        // Q
        { "QA", ("Qatar", "QAR") },

        // R
        { "RE", ("Réunion", "EUR") },
        { "RO", ("Romania", "RON") },
        { "RS", ("Serbia", "RSD") },
        { "RU", ("Russia", "RUB") },
        { "RW", ("Rwanda", "RWF") },

        // S
        { "SA", ("Saudi Arabia", "SAR") },
        { "SB", ("Solomon Islands", "SBD") },
        { "SC", ("Seychelles", "SCR") },
        { "SD", ("Sudan", "SDG") },
        { "SE", ("Sweden", "SEK") },
        { "SG", ("Singapore", "SGD") },
        { "SH", ("Saint Helena", "SHP") },
        { "SI", ("Slovenia", "EUR") },
        { "SJ", ("Svalbard and Jan Mayen", "NOK") },
        { "SK", ("Slovakia", "EUR") },
        { "SL", ("Sierra Leone", "SLL") },
        { "SM", ("San Marino", "EUR") },
        { "SN", ("Senegal", "XOF") },
        { "SO", ("Somalia", "SOS") },
        { "SR", ("Suriname", "SRD") },
        { "SS", ("South Sudan", "SSP") },
        { "ST", ("São Tomé and Príncipe", "STN") },
        { "SV", ("El Salvador", "USD") },
        { "SX", ("Sint Maarten", "ANG") },
        { "SY", ("Syria", "SYP") },
        { "SZ", ("Eswatini", "SZL") },

        // T
        { "TC", ("Turks and Caicos Islands", "USD") },
        { "TD", ("Chad", "XAF") },
        { "TF", ("French Southern Territories", "EUR") },
        { "TG", ("Togo", "XOF") },
        { "TH", ("Thailand", "THB") },
        { "TJ", ("Tajikistan", "TJS") },
        { "TK", ("Tokelau", "NZD") },
        { "TL", ("Timor-Leste", "USD") },
        { "TM", ("Turkmenistan", "TMT") },
        { "TN", ("Tunisia", "TND") },
        { "TO", ("Tonga", "TOP") },
        { "TR", ("Turkey", "TRY") },
        { "TT", ("Trinidad and Tobago", "TTD") },
        { "TV", ("Tuvalu", "AUD") },
        { "TW", ("Taiwan", "TWD") },
        { "TZ", ("Tanzania", "TZS") },

        // U
        { "UA", ("Ukraine", "UAH") },
        { "UG", ("Uganda", "UGX") },
        { "UM", ("United States Minor Outlying Islands", "USD") },
        { "US", ("United States", "USD") },
        { "UY", ("Uruguay", "UYU") },
        { "UZ", ("Uzbekistan", "UZS") },

        // V
        { "VA", ("Vatican City", "EUR") },
        { "VC", ("Saint Vincent and the Grenadines", "XCD") },
        { "VE", ("Venezuela", "VES") },
        { "VG", ("British Virgin Islands", "USD") },
        { "VI", ("U.S. Virgin Islands", "USD") },
        { "VN", ("Vietnam", "VND") },
        { "VU", ("Vanuatu", "VUV") },

        // W
        { "WF", ("Wallis and Futuna", "XPF") },
        { "WS", ("Samoa", "WST") },

        // Y
        { "YE", ("Yemen", "YER") },
        { "YT", ("Mayotte", "EUR") },

        // Z
        { "ZA", ("South Africa", "ZAR") },
        { "ZM", ("Zambia", "ZMW") },
        { "ZW", ("Zimbabwe", "ZWL") }
    };
}

/// <summary>
/// JSON converter for Country value object to enable serialization/deserialization
/// including support for dictionary keys.
/// </summary>
public class CountryJsonConverter : JsonConverter<Country>
{
    public override Country? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var code = reader.GetString();
        return string.IsNullOrEmpty(code) ? null : Country.FromCode(code);
    }

    public override void Write(Utf8JsonWriter writer, Country value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }

    public override Country ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var code = reader.GetString();
        return Country.FromCode(code!);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, Country value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.Value);
    }
}
