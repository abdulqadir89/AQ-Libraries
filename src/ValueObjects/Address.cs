namespace AQ.ValueObjects;

/// <summary>
/// Represents a physical address value object.
/// </summary>
public sealed class Address : ValueObject
{
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public Country Country { get; init; }

    // Parameterless constructor for EF Core
    private Address()
    {
        Street = null;
        City = null;
        State = null;
        PostalCode = null;
        Country = default!;
    }

    private Address(string? street, string? city, string? state, string? postalCode, Country country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    /// <summary>
    /// Creates a new Address instance with validation.
    /// </summary>
    /// <param name="street">The street address.</param>
    /// <param name="city">The city name.</param>
    /// <param name="state">The state or province.</param>
    /// <param name="postalCode">The postal or ZIP code.</param>
    /// <param name="countryCode">The ISO 3166-1 alpha-2 country code.</param>
    /// <returns>A valid Address instance.</returns>
    /// <exception cref="ArgumentException">Thrown when any required field is invalid.</exception>
    public static Address Create(string? street, string? city, string? state, string? postalCode, string countryCode)
    {
        ArgumentNullException.ThrowIfNull(countryCode);

        var country = Country.FromCode(countryCode);

        if (!string.IsNullOrWhiteSpace(street) && street.Length > 100)
            throw new ArgumentException("Street address cannot exceed 100 characters.", nameof(street));

        if (!string.IsNullOrWhiteSpace(city) && city.Length > 50)
            throw new ArgumentException("City cannot exceed 50 characters.", nameof(city));

        if (!string.IsNullOrWhiteSpace(state) && state.Length > 50)
            throw new ArgumentException("State cannot exceed 50 characters.", nameof(state));

        if (!string.IsNullOrWhiteSpace(postalCode) && postalCode.Length > 20)
            throw new ArgumentException("Postal code cannot exceed 20 characters.", nameof(postalCode));

        return new Address(
            string.IsNullOrWhiteSpace(street) ? null : street.Trim(),
            string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            string.IsNullOrWhiteSpace(state) ? null : state.Trim(),
            string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim(),
            country
        );
    }

    /// <summary>
    /// Attempts to create an Address instance without throwing an exception.
    /// </summary>
    /// <param name="street">The street address.</param>
    /// <param name="city">The city name.</param>
    /// <param name="state">The state or province.</param>
    /// <param name="postalCode">The postal or ZIP code.</param>
    /// <param name="countryCode">The ISO 3166-1 alpha-2 country code.</param>
    /// <param name="result">The created Address instance if successful.</param>
    /// <returns>True if the address is valid and created successfully, false otherwise.</returns>
    public static bool TryCreate(string? street, string? city, string? state, string? postalCode, string countryCode, out Address? result)
    {
        result = null;

        try
        {
            result = Create(street, city, state, postalCode, countryCode);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns the full address as a formatted string.
    /// </summary>
    /// <returns>The formatted address string.</returns>
    public string GetFullAddress()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(Street))
            parts.Add(Street);

        if (!string.IsNullOrWhiteSpace(City))
            parts.Add(City);

        var statePostal = new List<string>();
        if (!string.IsNullOrWhiteSpace(State))
            statePostal.Add(State);
        if (!string.IsNullOrWhiteSpace(PostalCode))
            statePostal.Add(PostalCode);

        if (statePostal.Count > 0)
            parts.Add(string.Join(" ", statePostal));

        parts.Add(Country.Name);

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Creates a new Address with a modified street.
    /// </summary>
    /// <param name="newStreet">The new street address.</param>
    /// <returns>A new Address instance with the modified street.</returns>
    public Address WithStreet(string? newStreet) => Create(newStreet, City, State, PostalCode, Country.Value);

    /// <summary>
    /// Creates a new Address with a modified city.
    /// </summary>
    /// <param name="newCity">The new city name.</param>
    /// <returns>A new Address instance with the modified city.</returns>
    public Address WithCity(string? newCity) => Create(Street, newCity, State, PostalCode, Country.Value);

    /// <summary>
    /// Creates a new Address with a modified state.
    /// </summary>
    /// <param name="newState">The new state or province.</param>
    /// <returns>A new Address instance with the modified state.</returns>
    public Address WithState(string? newState) => Create(Street, City, newState, PostalCode, Country.Value);

    /// <summary>
    /// Creates a new Address with a modified postal code.
    /// </summary>
    /// <param name="newPostalCode">The new postal or ZIP code.</param>
    /// <returns>A new Address instance with the modified postal code.</returns>
    public Address WithPostalCode(string? newPostalCode) => Create(Street, City, State, newPostalCode, Country.Value);

    /// <summary>
    /// Creates a new Address with a modified country.
    /// </summary>
    /// <param name="newCountryCode">The new ISO 3166-1 alpha-2 country code.</param>
    /// <returns>A new Address instance with the modified country.</returns>
    public Address WithCountry(string newCountryCode) => Create(Street, City, State, PostalCode, newCountryCode);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Street ?? string.Empty;
        yield return City ?? string.Empty;
        yield return State ?? string.Empty;
        yield return PostalCode ?? string.Empty;
        yield return Country;
    }

    public override string ToString() => GetFullAddress();

    public override Address Clone()
    {
        return Create(Street, City, State, PostalCode, Country.Value);
    }
}
