using AQ.Common.Application.Extensions;
using AQ.Common.Application.Services;
using AQ.Common.Application.Specifications.Builders;
using AQ.Common.Application.Specifications.Interfaces;

namespace AQ.Common.Application.Examples;

/// <summary>
/// Examples showing how to use the IApplicationDbContext wrapper with specifications
/// This file demonstrates the usage patterns and can be deleted after understanding the concepts
/// </summary>
public class ApplicationDbContextUsageExamples
{
    private readonly IApplicationDbContext _context;

    public ApplicationDbContextUsageExamples(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Example: Get user by ID with related data using string includes
    /// </summary>
    public async Task<User?> GetUserWithProfileAsync(int userId)
    {
        return await _context.GetByIdAsync<User>(userId, "Profile");
    }

    /// <summary>
    /// Example: Get user by ID with related data using expression includes
    /// </summary>
    public async Task<User?> GetUserWithOrdersAsync(int userId)
    {
        return await _context.GetByIdAsync(userId, (User u) => u.Orders);
    }

    /// <summary>
    /// Example: Get user by ID with multiple includes using fluent builder
    /// </summary>
    public async Task<User?> GetUserWithMultipleIncludesAsync(int userId)
    {
        return await _context.GetByIdWithIncludesAsync<User>(userId, builder => builder
            .Include(u => u.Profile)
            .Include(u => u.Orders)
            .Include("Roles"));
    }

    /// <summary>
    /// Example: Simple query with filtering
    /// </summary>
    public async Task<List<User>> GetActiveUsersAsync()
    {
        return await _context.GetListAsync<User>(builder => builder
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name));
    }

    /// <summary>
    /// Example: Complex query with filtering, includes, and pagination
    /// </summary>
    public async Task<List<User>> GetActiveUsersWithOrdersAsync(int page, int pageSize)
    {
        return await _context.GetListAsync<User>(builder => builder
            .Where(u => u.IsActive)
            .Where(u => u.Orders.Any())
            .Include(u => u.Orders)
            .Include(u => u.Profile)
            .OrderByDescending(u => u.CreatedAt)
            .Paginate(page, pageSize));
    }

    /// <summary>
    /// Example: Count with filtering
    /// </summary>
    public async Task<int> CountActiveUsersAsync()
    {
        return await _context.CountAsync<User>(builder => builder
            .Where(u => u.IsActive));
    }

    /// <summary>
    /// Example: Check existence
    /// </summary>
    public async Task<bool> HasActiveUsersAsync()
    {
        return await _context.AnyAsync<User>(builder => builder
            .Where(u => u.IsActive));
    }

    /// <summary>
    /// Example: Using traditional specification approach
    /// </summary>
    public async Task<List<User>> GetUsersWithTraditionalSpecAsync()
    {
        var spec = SpecificationBuilder<User>.Create()
            .Where(u => u.IsActive)
            .Include(u => u.Profile)
            .OrderBy(u => u.Name)
            .Build();

        return await _context.GetListAsync(spec);
    }

    /// <summary>
    /// Example: Creating reusable specifications
    /// </summary>
    public IQuerySpecification<User> CreateActiveUsersSpecification()
    {
        return SpecificationBuilder<User>.Create()
            .Where(u => u.IsActive)
            .Include(u => u.Profile)
            .OrderBy(u => u.Name)
            .Build();
    }

    /// <summary>
    /// Example: Using the reusable specification
    /// </summary>
    public async Task<List<User>> GetActiveUsersWithReusableSpecAsync()
    {
        var spec = CreateActiveUsersSpecification();
        return await _context.GetListAsync(spec);
    }
}

/// <summary>
/// Sample entity classes for the examples
/// These are just for demonstration purposes
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Profile? Profile { get; set; }
    public List<Order> Orders { get; set; } = new();
    public List<Role> Roles { get; set; } = new();
}

public class Profile
{
    public int Id { get; set; }
    public string Bio { get; set; } = string.Empty;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public class Order
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<User> Users { get; set; } = new();
}
