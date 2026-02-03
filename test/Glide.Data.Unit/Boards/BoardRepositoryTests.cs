using Glide.Data.Boards;
using Glide.Data.Users;

namespace Glide.Data.Unit.Boards;

public class BoardRepositoryTests : RepositoryTestBase
{
    private readonly IBoardRepository _repository;
    private readonly IUserRepository _userRepository;

    public BoardRepositoryTests()
    {
        _repository = new BoardRepository(ConnectionFactory);
        _userRepository = new UserRepository(ConnectionFactory);
    }

    [Test]
    public async Task CreateAsync_WithValidData_CreatesBoard()
    {
        // Arrange
        User user = await CreateTestUser("board-user-1");

        // Act
        Board board = await _repository.CreateAsync("Test Board", user.Id);

        // Assert
        await Assert.That(board).IsNotNull();
        await Assert.That(board.Id).IsNotEmpty();
        await Assert.That(board.Name).IsEqualTo("Test Board");
        await Assert.That(board.BoardUsers).Contains(x => x.UserId == user.Id);
    }

    [Test]
    public async Task GetByIdAsync_WithExistingBoard_ReturnsBoard()
    {
        // Arrange
        User user = await CreateTestUser("board-user-2");
        Board created = await _repository.CreateAsync("Find Me Board", user.Id);

        // Act
        Board? result = await _repository.GetByIdAsync(created.Id);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(created.Id);
        await Assert.That(result.Name).IsEqualTo("Find Me Board");
        await Assert.That(result.BoardUsers).Contains(x => x.UserId == user.Id);
    }

    [Test]
    public async Task GetByIdAsync_WithNonExistentBoard_ReturnsNull()
    {
        // Act
        Board? result = await _repository.GetByIdAsync("nonexistent-board-id");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetByUserIdAsync_WithNoBoards_ReturnsEmpty()
    {
        // Arrange
        User user = await CreateTestUser("board-user-empty");

        // Act
        IEnumerable<Board> result = (await _repository.GetByUserIdAsync(user.Id)).ToList();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task GetByUserIdAsync_WithMultipleBoards_ReturnsAllUserBoards()
    {
        // Arrange
        User user = await CreateTestUser("board-user-3");
        Board board1 = await _repository.CreateAsync("Board 1", user.Id);
        Board board2 = await _repository.CreateAsync("Board 2", user.Id);
        Board board3 = await _repository.CreateAsync("Board 3", user.Id);

        // Act
        IEnumerable<Board> result = await _repository.GetByUserIdAsync(user.Id);

        // Assert
        List<Board> boards = result.ToList();
        await Assert.That(boards.Count).IsEqualTo(3);
        await Assert.That(boards.Any(b => b.Id == board1.Id)).IsTrue();
        await Assert.That(boards.Any(b => b.Id == board2.Id)).IsTrue();
        await Assert.That(boards.Any(b => b.Id == board3.Id)).IsTrue();
    }

    [Test]
    public async Task GetByUserIdAsync_OnlyReturnsUserBoards()
    {
        // Arrange
        User user1 = await CreateTestUser("board-user-4");
        User user2 = await CreateTestUser("board-user-5");

        Board user1Board1 = await _repository.CreateAsync("User 1 Board 1", user1.Id);
        Board user1Board2 = await _repository.CreateAsync("User 1 Board 2", user1.Id);
        Board user2Board = await _repository.CreateAsync("User 2 Board", user2.Id);

        // Act
        IEnumerable<Board> user1Boards = await _repository.GetByUserIdAsync(user1.Id);
        IEnumerable<Board> user2Boards = await _repository.GetByUserIdAsync(user2.Id);

        // Assert
        List<Board> user1List = user1Boards.ToList();
        List<Board> user2List = user2Boards.ToList();

        await Assert.That(user1List.Count).IsEqualTo(2);
        await Assert.That(user2List.Count).IsEqualTo(1);

        await Assert.That(user1List.Any(b => b.Id == user1Board1.Id)).IsTrue();
        await Assert.That(user1List.Any(b => b.Id == user1Board2.Id)).IsTrue();
        await Assert.That(user1List.Any(b => b.Id == user2Board.Id)).IsFalse();

        await Assert.That(user2List.Any(b => b.Id == user2Board.Id)).IsTrue();
    }

    [Test]
    public async Task UpdateAsync_WithExistingBoard_UpdatesName()
    {
        // Arrange
        User user = await CreateTestUser("board-user-6");
        Board board = await _repository.CreateAsync("Original Name", user.Id);

        // Act
        Board? updated = await _repository.UpdateAsync(board.Id, "Updated Name");

        // Assert
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.Name).IsEqualTo("Updated Name");
        await Assert.That(updated.Id).IsEqualTo(board.Id);
        await Assert.That(updated.BoardUsers).Contains(x => x.UserId == user.Id);

        // Verify in database
        Board? retrieved = await _repository.GetByIdAsync(board.Id);
        await Assert.That(retrieved!.Name).IsEqualTo("Updated Name");
    }

    [Test]
    public async Task UpdateAsync_WithNonExistentBoard_ReturnsNull()
    {
        // Act
        Board? result = await _repository.UpdateAsync("nonexistent-id", "New Name");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task DeleteAsync_WithExistingBoard_RemovesBoard()
    {
        // Arrange
        User user = await CreateTestUser("board-user-7");
        Board board = await _repository.CreateAsync("Board To Delete", user.Id);

        // Verify it exists
        Board? beforeDelete = await _repository.GetByIdAsync(board.Id);
        await Assert.That(beforeDelete).IsNotNull();

        // Act
        await _repository.DeleteAsync(board.Id);

        // Assert
        Board? afterDelete = await _repository.GetByIdAsync(board.Id);
        await Assert.That(afterDelete).IsNull();

        // Verify not in user's boards
        IEnumerable<Board> userBoards = await _repository.GetByUserIdAsync(user.Id);
        await Assert.That(userBoards.Any(b => b.Id == board.Id)).IsFalse();
    }

    [Test]
    public async Task DeleteAsync_WithNonExistentBoard_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _repository.DeleteAsync("nonexistent-board");
    }

    [Test]
    public async Task DeleteAsync_OnlyDeletesSpecifiedBoard()
    {
        // Arrange
        User user = await CreateTestUser("board-user-8");
        Board board1 = await _repository.CreateAsync("Keep This", user.Id);
        Board board2 = await _repository.CreateAsync("Delete This", user.Id);

        // Act
        await _repository.DeleteAsync(board2.Id);

        // Assert
        Board? remaining = await _repository.GetByIdAsync(board1.Id);
        Board? deleted = await _repository.GetByIdAsync(board2.Id);

        await Assert.That(remaining).IsNotNull();
        await Assert.That(deleted).IsNull();
    }

    [Test]
    public async Task CreateAsync_WithEmptyName_CreatesBoard()
    {
        // Arrange
        User user = await CreateTestUser("board-user-9");

        // Act
        Board board = await _repository.CreateAsync("", user.Id);

        // Assert
        await Assert.That(board).IsNotNull();
        await Assert.That(board.Name).IsEqualTo("");
    }

    [Test]
    public async Task CreateAsync_WithLongName_CreatesBoard()
    {
        // Arrange
        User user = await CreateTestUser("board-user-10");
        string longName = new('A', 500);

        // Act
        Board board = await _repository.CreateAsync(longName, user.Id);

        // Assert
        await Assert.That(board).IsNotNull();
        await Assert.That(board.Name).IsEqualTo(longName);
    }

    [Test]
    public async Task CreateAsync_WithMultipleBoardsSameName_AllCreatedSuccessfully()
    {
        // Arrange
        User user = await CreateTestUser("board-user-11");
        string name = "Duplicate Name";

        // Act
        Board board1 = await _repository.CreateAsync(name, user.Id);
        Board board2 = await _repository.CreateAsync(name, user.Id);
        Board board3 = await _repository.CreateAsync(name, user.Id);

        // Assert
        await Assert.That(board1.Id).IsNotEqualTo(board2.Id);
        await Assert.That(board1.Id).IsNotEqualTo(board3.Id);
        await Assert.That(board2.Id).IsNotEqualTo(board3.Id);

        IEnumerable<Board> userBoards = await _repository.GetByUserIdAsync(user.Id);
        await Assert.That(userBoards.Count(b => b.Name == name)).IsEqualTo(3);
    }

    [Test]
    public async Task GetBoardUsersAsync_WithMultipleUsers_ReturnsAllUsers()
    {
        // Arrange
        User owner = await CreateTestUser("board-owner");
        User user1 = await CreateTestUser("board-user-12");
        User user2 = await CreateTestUser("board-user-13");

        Board board = await _repository.CreateAsync("Multi-User Board", owner.Id);
        await _repository.AddUserToBoardAsync(board.Id, user1.Id, false);
        await _repository.AddUserToBoardAsync(board.Id, user2.Id, true);

        // Act
        IEnumerable<BoardUser> users = await _repository.GetBoardUsersAsync(board.Id);

        // Assert
        List<BoardUser> userList = users.ToList();
        await Assert.That(userList.Count).IsEqualTo(3);
        await Assert.That(userList.Any(u => u.UserId == owner.Id && u.IsOwner)).IsTrue();
        await Assert.That(userList.Any(u => u.UserId == user1.Id && !u.IsOwner)).IsTrue();
        await Assert.That(userList.Any(u => u.UserId == user2.Id && u.IsOwner)).IsTrue();
    }

    [Test]
    public async Task GetBoardUsersAsync_WithNoUsers_ReturnsEmpty()
    {
        // Act
        IEnumerable<BoardUser> users = await _repository.GetBoardUsersAsync("nonexistent-board");

        // Assert
        await Assert.That(users).IsEmpty();
    }

    [Test]
    public async Task AddUserToBoardAsync_WithNewUser_AddsUserAsNonOwner()
    {
        // Arrange
        User owner = await CreateTestUser("board-owner-2");
        User newUser = await CreateTestUser("board-user-14");
        Board board = await _repository.CreateAsync("Add User Board", owner.Id);

        // Act
        await _repository.AddUserToBoardAsync(board.Id, newUser.Id, false);

        // Assert
        Board? updated = await _repository.GetByIdAsync(board.Id);
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.BoardUsers.Any(u => u.UserId == newUser.Id && !u.IsOwner)).IsTrue();
    }

    [Test]
    public async Task AddUserToBoardAsync_WithNewUser_AddsUserAsOwner()
    {
        // Arrange
        User owner = await CreateTestUser("board-owner-3");
        User newOwner = await CreateTestUser("board-user-15");
        Board board = await _repository.CreateAsync("Add Owner Board", owner.Id);

        // Act
        await _repository.AddUserToBoardAsync(board.Id, newOwner.Id, true);

        // Assert
        Board? updated = await _repository.GetByIdAsync(board.Id);
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.BoardUsers.Any(u => u.UserId == newOwner.Id && u.IsOwner)).IsTrue();
    }

    [Test]
    public async Task UpdateUserRoleAsync_TogglesOwnershipStatus()
    {
        // Arrange
        User owner = await CreateTestUser("board-owner-4");
        User user = await CreateTestUser("board-user-16");
        Board board = await _repository.CreateAsync("Update Role Board", owner.Id);
        await _repository.AddUserToBoardAsync(board.Id, user.Id, false);

        // Act - Promote to owner
        await _repository.UpdateUserRoleAsync(board.Id, user.Id, true);

        // Assert
        Board? afterPromote = await _repository.GetByIdAsync(board.Id);
        await Assert.That(afterPromote!.BoardUsers.First(u => u.UserId == user.Id).IsOwner).IsTrue();

        // Act - Demote to non-owner
        await _repository.UpdateUserRoleAsync(board.Id, user.Id, false);

        // Assert
        Board? afterDemote = await _repository.GetByIdAsync(board.Id);
        await Assert.That(afterDemote!.BoardUsers.First(u => u.UserId == user.Id).IsOwner).IsFalse();
    }

    [Test]
    public async Task RemoveUserFromBoardAsync_WithExistingUser_RemovesUser()
    {
        // Arrange
        User owner = await CreateTestUser("board-owner-5");
        User user = await CreateTestUser("board-user-17");
        Board board = await _repository.CreateAsync("Remove User Board", owner.Id);
        await _repository.AddUserToBoardAsync(board.Id, user.Id, false);

        // Verify user exists
        Board? beforeRemove = await _repository.GetByIdAsync(board.Id);
        await Assert.That(beforeRemove!.BoardUsers.Any(u => u.UserId == user.Id)).IsTrue();

        // Act
        await _repository.RemoveUserFromBoardAsync(board.Id, user.Id);

        // Assert
        Board? afterRemove = await _repository.GetByIdAsync(board.Id);
        await Assert.That(afterRemove!.BoardUsers.Any(u => u.UserId == user.Id)).IsFalse();
        await Assert.That(afterRemove.BoardUsers.Count()).IsEqualTo(1);
    }

    [Test]
    public async Task RemoveUserFromBoardAsync_WithNonExistentUser_DoesNotThrow()
    {
        // Arrange
        User owner = await CreateTestUser("board-owner-6");
        Board board = await _repository.CreateAsync("Remove Nonexistent Board", owner.Id);

        // Act & Assert - Should not throw
        await _repository.RemoveUserFromBoardAsync(board.Id, "nonexistent-user");
    }

    [Test]
    public async Task AddUserToBoardAsync_AllowsDuplicateAddition()
    {
        // Note: The database has a unique constraint, so the second insert should fail silently or throw.
        // This test documents the current behavior - adjust based on desired behavior.
        // Arrange
        User owner = await CreateTestUser("board-owner-7");
        User user = await CreateTestUser("board-user-18");
        Board board = await _repository.CreateAsync("Duplicate Add Board", owner.Id);

        // Act & Assert
        // First add should succeed
        await _repository.AddUserToBoardAsync(board.Id, user.Id, false);

        // Second add should throw due to unique constraint
        await Assert.ThrowsAsync<Exception>(
            async () => await _repository.AddUserToBoardAsync(board.Id, user.Id, true)
        );
    }

    private async Task<User> CreateTestUser(string providerId)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        User user = new()
        {
            Id = Guid.CreateVersion7().ToString(),
            DisplayName = "Test User",
            Email = $"{providerId}@example.com",
            CreatedAt = now,
            UpdatedAt = now
        };
        await _userRepository.CreateAsync(user);
        return user;
    }
}