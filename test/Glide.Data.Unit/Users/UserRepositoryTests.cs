using Glide.Data.Users;

namespace Glide.Data.Unit.Users;

public class UserRepositoryTests : RepositoryTestBase
{
    private readonly IUserRepository _repository;

    public UserRepositoryTests()
    {
        _repository = new UserRepository(ConnectionFactory);
    }

    [Test]
    public async Task GetByIdAsync_WithNonExistentUser_ReturnsNull()
    {
        // Act
        User? result = await _repository.GetByIdAsync("nonexistent");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task CreateAsync_WithValidUser_InsertsUser()
    {
        // Arrange
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        User user = new()
        {
            Id = Guid.CreateVersion7().ToString(),
            DisplayName = "Test User",
            Email = "test@example.com",
            CreatedAt = now,
            UpdatedAt = now
        };

        // Act
        await _repository.CreateAsync(user);

        // Assert
        User? retrieved = await _repository.GetByIdAsync(user.Id);
        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.Id).IsEqualTo(user.Id);
        await Assert.That(retrieved.DisplayName).IsEqualTo(user.DisplayName);
        await Assert.That(retrieved.Email).IsEqualTo(user.Email);
    }

    [Test]
    public async Task GetByIdAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        User user = new()
        {
            Id = Guid.CreateVersion7().ToString(),
            DisplayName = "Existing User",
            Email = "existing@example.com",
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repository.CreateAsync(user);

        // Act
        User? result = await _repository.GetByIdAsync(user.Id);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.DisplayName).IsEqualTo("Existing User");
    }

    [Test]
    public async Task GetByEmailAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        User user = new()
        {
            Id = Guid.CreateVersion7().ToString(),
            DisplayName = "Email User",
            Email = "email@example.com",
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repository.CreateAsync(user);

        // Act
        User? result = await _repository.GetByEmailAsync("email@example.com");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(user.Id);
    }

    [Test]
    public async Task GetByEmailAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Act
        User? result = await _repository.GetByEmailAsync("nonexistent@example.com");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task UpdateAsync_WithExistingUser_UpdatesFields()
    {
        // Arrange
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        User user = new()
        {
            Id = Guid.CreateVersion7().ToString(),
            DisplayName = "Original Name",
            Email = "original@example.com",
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repository.CreateAsync(user);

        // Act
        User updated = user with { DisplayName = "Updated Name", Email = "updated@example.com" };
        await _repository.UpdateAsync(updated);

        // Assert
        User? retrieved = await _repository.GetByIdAsync(user.Id);
        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.DisplayName).IsEqualTo("Updated Name");
        await Assert.That(retrieved.Email).IsEqualTo("updated@example.com");
    }

    [Test]
    public async Task CreateAsync_WithMultipleUsers_AllStoredCorrectly()
    {
        // Arrange
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        User user1 = new()
        {
            Id = Guid.CreateVersion7().ToString(),
            DisplayName = "User 1",
            Email = "user1@example.com",
            CreatedAt = now,
            UpdatedAt = now
        };

        User user2 = new()
        {
            Id = Guid.CreateVersion7().ToString(),
            DisplayName = "User 2",
            Email = "user2@example.com",
            CreatedAt = now,
            UpdatedAt = now
        };

        // Act
        await _repository.CreateAsync(user1);
        await _repository.CreateAsync(user2);

        // Assert
        User? retrieved1 = await _repository.GetByIdAsync(user1.Id);
        User? retrieved2 = await _repository.GetByIdAsync(user2.Id);

        await Assert.That(retrieved1).IsNotNull();
        await Assert.That(retrieved2).IsNotNull();
        await Assert.That(retrieved1!.DisplayName).IsEqualTo("User 1");
        await Assert.That(retrieved2!.DisplayName).IsEqualTo("User 2");
    }

    [Test]
    public async Task SearchByEmailAsync_WithPartialMatch_ReturnsMatchingUsers()
    {
        // Arrange
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        User user1 = new()
        {
            Id = Guid.CreateVersion7().ToString(),
            DisplayName = "Alice",
            Email = "alice.smith@company.com",
            CreatedAt = now,
            UpdatedAt = now
        };

        User user2 = new()
        {
            Id = Guid.CreateVersion7().ToString(),
            DisplayName = "Bob",
            Email = "bob.smith@company.com",
            CreatedAt = now,
            UpdatedAt = now
        };

        User user3 = new()
        {
            Id = Guid.CreateVersion7().ToString(),
            DisplayName = "Charlie",
            Email = "charlie@other.com",
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.CreateAsync(user1);
        await _repository.CreateAsync(user2);
        await _repository.CreateAsync(user3);

        // Act
        IEnumerable<User> results = await _repository.SearchByEmailAsync("smith");

        // Assert
        List<User> resultList = results.ToList();
        await Assert.That(resultList.Count).IsEqualTo(2);
        await Assert.That(resultList.Any(u => u.Email == "alice.smith@company.com")).IsTrue();
        await Assert.That(resultList.Any(u => u.Email == "bob.smith@company.com")).IsTrue();
    }

    [Test]
    public async Task SearchByEmailAsync_WithNoMatches_ReturnsEmpty()
    {
        // Act
        IEnumerable<User> results = await _repository.SearchByEmailAsync("nonexistent");

        // Assert
        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task SearchByEmailAsync_WithExactMatch_ReturnsUser()
    {
        // Arrange
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        User user = new()
        {
            Id = Guid.CreateVersion7().ToString(),
            DisplayName = "Search Test",
            Email = "search@test.com",
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repository.CreateAsync(user);

        // Act
        IEnumerable<User> results = await _repository.SearchByEmailAsync("search@test.com");

        // Assert
        List<User> resultList = results.ToList();
        await Assert.That(resultList.Count).IsEqualTo(1);
        await Assert.That(resultList[0].Email).IsEqualTo("search@test.com");
    }

    [Test]
    public async Task SearchByEmailAsync_IsCaseInsensitive()
    {
        // Arrange
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        User user = new()
        {
            Id = Guid.CreateVersion7().ToString(),
            DisplayName = "Case Test",
            Email = "CaseTest@EXAMPLE.com",
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repository.CreateAsync(user);

        // Act
        IEnumerable<User> results = await _repository.SearchByEmailAsync("casetest");

        // Assert
        List<User> resultList = results.ToList();
        await Assert.That(resultList.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(resultList.Any(u => u.Email.Contains("CaseTest", StringComparison.OrdinalIgnoreCase)))
            .IsTrue();
    }
}