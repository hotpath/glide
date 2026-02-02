using Glide.Data.Sessions;
using Glide.Data.Users;

namespace Glide.Data.Unit.Sessions;

public class SessionRepositoryTests : RepositoryTestBase
{
    private readonly ISessionRepository _repository;
    private readonly IUserRepository _userRepository;

    public SessionRepositoryTests()
    {
        _repository = new SessionRepository(ConnectionFactory);
        _userRepository = new UserRepository(ConnectionFactory);
    }

    [Test]
    public async Task CreateAsync_WithValidData_CreatesSession()
    {
        // Arrange
        User user = await CreateTestUser("session-user-1");

        // Act
        Session session = await _repository.CreateAsync(user.Id, 86400);

        // Assert
        await Assert.That(session).IsNotNull();
        await Assert.That(session.Id).IsNotEmpty();
        await Assert.That(session.UserId).IsEqualTo(user.Id);
        await Assert.That(session.ExpiresAt).IsGreaterThan(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    [Test]
    public async Task CreateAsync_WithDuration_SetsCorrectExpiry()
    {
        // Arrange
        User user = await CreateTestUser("session-user-2");
        long duration = 3600L; // 1 hour in seconds
        long beforeCreate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        Session session = await _repository.CreateAsync(user.Id, duration);

        // Assert
        long afterCreate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long expectedExpiryMin = beforeCreate + (duration * 1000); // Convert duration to milliseconds
        long expectedExpiryMax = afterCreate + (duration * 1000);
        await Assert.That(session.ExpiresAt).IsGreaterThanOrEqualTo(expectedExpiryMin);
        await Assert.That(session.ExpiresAt).IsLessThanOrEqualTo(expectedExpiryMax);
    }

    [Test]
    public async Task GetAsync_WithValidSessionId_ReturnsSessionUser()
    {
        // Arrange
        User user = await CreateTestUser("session-user-3");
        Session session = await _repository.CreateAsync(user.Id, 86400);

        // Act
        SessionUser? result = await _repository.GetAsync(session.Id);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.UserId).IsEqualTo(user.Id);
        await Assert.That(result.DisplayName).IsEqualTo(user.DisplayName);
        await Assert.That(result.Email).IsEqualTo(user.Email);
        await Assert.That(result.ExpiresAt).IsEqualTo(session.ExpiresAt);
    }

    [Test]
    public async Task GetAsync_WithNonExistentSessionId_ReturnsNull()
    {
        // Act
        SessionUser? result = await _repository.GetAsync("nonexistent-session-id");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetAsync_WithExpiredSession_ReturnsNull()
    {
        // Arrange
        User user = await CreateTestUser("session-user-expired");
        Session session = await _repository.CreateAsync(user.Id, -3600); // Expired 1 hour ago

        // Act
        SessionUser? result = await _repository.GetAsync(session.Id);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task DeleteAsync_WithExistingSession_RemovesSession()
    {
        // Arrange
        User user = await CreateTestUser("session-user-4");
        Session session = await _repository.CreateAsync(user.Id, 86400);

        // Verify it exists
        SessionUser? beforeDelete = await _repository.GetAsync(session.Id);
        await Assert.That(beforeDelete).IsNotNull();

        // Act
        await _repository.DeleteAsync(session.Id);

        // Assert
        SessionUser? afterDelete = await _repository.GetAsync(session.Id);
        await Assert.That(afterDelete).IsNull();
    }

    [Test]
    public async Task DeleteAsync_WithNonExistentSession_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _repository.DeleteAsync("nonexistent-session");
    }

    [Test]
    public async Task CreateAsync_WithMultipleSessions_EachGetsUniqueId()
    {
        // Arrange
        User user = await CreateTestUser("session-user-5");

        // Act
        Session session1 = await _repository.CreateAsync(user.Id, 86400);
        Session session2 = await _repository.CreateAsync(user.Id, 86400);
        Session session3 = await _repository.CreateAsync(user.Id, 86400);

        // Assert
        await Assert.That(session1.Id).IsNotEqualTo(session2.Id);
        await Assert.That(session1.Id).IsNotEqualTo(session3.Id);
        await Assert.That(session2.Id).IsNotEqualTo(session3.Id);

        // All should be retrievable
        SessionUser? retrieved1 = await _repository.GetAsync(session1.Id);
        SessionUser? retrieved2 = await _repository.GetAsync(session2.Id);
        SessionUser? retrieved3 = await _repository.GetAsync(session3.Id);

        await Assert.That(retrieved1).IsNotNull();
        await Assert.That(retrieved2).IsNotNull();
        await Assert.That(retrieved3).IsNotNull();
    }

    [Test]
    public async Task GetAsync_ReturnsCorrectUserDetails()
    {
        // Arrange
        User user = await CreateTestUser("detailed-user", "Detailed User", "detailed@example.com");
        Session session = await _repository.CreateAsync(user.Id, 86400);

        // Act
        SessionUser? result = await _repository.GetAsync(session.Id);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.DisplayName).IsEqualTo("Detailed User");
        await Assert.That(result.Email).IsEqualTo("detailed@example.com");
    }

    [Test]
    public async Task CreateAsync_WithZeroDuration_CreatesExpiredSession()
    {
        // Arrange
        User user = await CreateTestUser("session-user-zero");

        // Act
        Session session = await _repository.CreateAsync(user.Id, 0);

        // Assert
        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await Assert.That(session.ExpiresAt).IsLessThanOrEqualTo(currentTime);

        // Should not be retrievable due to expiry
        SessionUser? result = await _repository.GetAsync(session.Id);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task DeleteAsync_OnlyDeletesSpecifiedSession()
    {
        // Arrange
        User user = await CreateTestUser("session-user-6");
        Session session1 = await _repository.CreateAsync(user.Id, 86400);
        Session session2 = await _repository.CreateAsync(user.Id, 86400);

        // Act
        await _repository.DeleteAsync(session1.Id);

        // Assert
        SessionUser? deleted = await _repository.GetAsync(session1.Id);
        SessionUser? remaining = await _repository.GetAsync(session2.Id);

        await Assert.That(deleted).IsNull();
        await Assert.That(remaining).IsNotNull();
    }

    private async Task<User> CreateTestUser(string providerId, string displayName = "Test User",
        string? email = null)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        User user = new()
        {
            Id = Guid.CreateVersion7().ToString(),
            DisplayName = displayName,
            Email = email ?? $"{providerId}@example.com",
            CreatedAt = now,
            UpdatedAt = now
        };
        await _userRepository.CreateAsync(user);
        return user;
    }
}