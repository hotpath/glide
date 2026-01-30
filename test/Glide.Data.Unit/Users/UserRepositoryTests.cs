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
    public async Task GetAsync_WithNonExistentUser_ReturnsNull()
    {
        // Act
        User? result = await _repository.GetAsync("forgejo", "nonexistent");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Create_WithValidUser_InsertsUser()
    {
        // Arrange
        User user = new()
        {
            Id = Guid.NewGuid().ToString(),
            OAuthProvider = "forgejo",
            OAuthProviderId = "123",
            DisplayName = "Test User",
            Email = "test@example.com"
        };

        // Act
        await _repository.Create(user);

        // Assert
        User? retrieved = await _repository.GetAsync("forgejo", "123");
        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.Id).IsEqualTo(user.Id);
        await Assert.That(retrieved.OAuthProvider).IsEqualTo(user.OAuthProvider);
        await Assert.That(retrieved.OAuthProviderId).IsEqualTo(user.OAuthProviderId);
        await Assert.That(retrieved.DisplayName).IsEqualTo(user.DisplayName);
        await Assert.That(retrieved.Email).IsEqualTo(user.Email);
    }

    [Test]
    public async Task GetAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        User user = new()
        {
            Id = Guid.NewGuid().ToString(),
            OAuthProvider = "forgejo",
            OAuthProviderId = "456",
            DisplayName = "Existing User",
            Email = "existing@example.com"
        };
        await _repository.Create(user);

        // Act
        User? result = await _repository.GetAsync("forgejo", "456");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.DisplayName).IsEqualTo("Existing User");
    }

    [Test]
    public async Task GetAsync_WithDifferentProvider_ReturnsNull()
    {
        // Arrange
        User user = new()
        {
            Id = Guid.NewGuid().ToString(),
            OAuthProvider = "forgejo",
            OAuthProviderId = "789",
            DisplayName = "User",
            Email = "user@example.com"
        };
        await _repository.Create(user);

        // Act
        User? result = await _repository.GetAsync("github", "789");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task UpdateAsync_WithExistingUser_UpdatesFields()
    {
        // Arrange
        User user = new()
        {
            Id = Guid.NewGuid().ToString(),
            OAuthProvider = "forgejo",
            OAuthProviderId = "update123",
            DisplayName = "Original Name",
            Email = "original@example.com"
        };
        await _repository.Create(user);

        // Act
        User updated = user with { DisplayName = "Updated Name", Email = "updated@example.com" };
        await _repository.UpdateAsync(updated);

        // Assert
        User? retrieved = await _repository.GetAsync("forgejo", "update123");
        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.DisplayName).IsEqualTo("Updated Name");
        await Assert.That(retrieved.Email).IsEqualTo("updated@example.com");
    }

    [Test]
    public async Task CreateOrUpdateFromOAuthAsync_WithNewUser_CreatesUser()
    {
        // Act
        User user = await _repository.CreateOrUpdateFromOAuthAsync(
            "forgejo",
            "oauth123",
            "OAuth User",
            "oauth@example.com"
        );

        // Assert
        await Assert.That(user).IsNotNull();
        await Assert.That(user.OAuthProvider).IsEqualTo("forgejo");
        await Assert.That(user.OAuthProviderId).IsEqualTo("oauth123");
        await Assert.That(user.DisplayName).IsEqualTo("OAuth User");
        await Assert.That(user.Email).IsEqualTo("oauth@example.com");

        // Verify it's in the database
        User? retrieved = await _repository.GetAsync("forgejo", "oauth123");
        await Assert.That(retrieved).IsNotNull();
    }

    [Test]
    public async Task CreateOrUpdateFromOAuthAsync_WithExistingUser_UpdatesUser()
    {
        // Arrange
        User existingUser = await _repository.CreateOrUpdateFromOAuthAsync(
            "forgejo",
            "oauth456",
            "Original Name",
            "original@example.com"
        );

        // Act
        User updatedUser = await _repository.CreateOrUpdateFromOAuthAsync(
            "forgejo",
            "oauth456",
            "Updated Name",
            "updated@example.com"
        );

        // Assert
        await Assert.That(updatedUser.Id).IsEqualTo(existingUser.Id);
        await Assert.That(updatedUser.DisplayName).IsEqualTo("Updated Name");
        await Assert.That(updatedUser.Email).IsEqualTo("updated@example.com");

        // Verify only one user exists
        IEnumerable<User> allUsers = await QueryAsync<User>("SELECT * FROM users WHERE oauth_provider_id = @ProviderId",
            new { ProviderId = "oauth456" });
        await Assert.That(allUsers.Count()).IsEqualTo(1);
    }

    [Test]
    public async Task GetAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        CancellationTokenSource cts = new();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.That(async () => await _repository.GetAsync("forgejo", "test", cts.Token))
            .ThrowsException();
    }

    [Test]
    public async Task Create_WithMultipleUsers_AllStoredCorrectly()
    {
        // Arrange
        User user1 = new()
        {
            Id = Guid.NewGuid().ToString(),
            OAuthProvider = "forgejo",
            OAuthProviderId = "multi1",
            DisplayName = "User 1",
            Email = "user1@example.com"
        };

        User user2 = new()
        {
            Id = Guid.NewGuid().ToString(),
            OAuthProvider = "forgejo",
            OAuthProviderId = "multi2",
            DisplayName = "User 2",
            Email = "user2@example.com"
        };

        // Act
        await _repository.Create(user1);
        await _repository.Create(user2);

        // Assert
        User? retrieved1 = await _repository.GetAsync("forgejo", "multi1");
        User? retrieved2 = await _repository.GetAsync("forgejo", "multi2");

        await Assert.That(retrieved1).IsNotNull();
        await Assert.That(retrieved2).IsNotNull();
        await Assert.That(retrieved1!.DisplayName).IsEqualTo("User 1");
        await Assert.That(retrieved2!.DisplayName).IsEqualTo("User 2");
    }

    [Test]
    public async Task CreateOrUpdateFromOAuthAsync_WithEmptyEmail_HandlesCorrectly()
    {
        // Act
        User user = await _repository.CreateOrUpdateFromOAuthAsync(
            "forgejo",
            "noemail",
            "No Email User",
            ""
        );

        // Assert
        await Assert.That(user.Email).IsEqualTo("");
        User? retrieved = await _repository.GetAsync("forgejo", "noemail");
        await Assert.That(retrieved).IsNotNull();
    }
}