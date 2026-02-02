using Glide.Data.Boards;
using Glide.Data.Cards;
using Glide.Data.Columns;
using Glide.Data.Users;

namespace Glide.Data.Unit.Cards;

public class CardRepositoryTests : RepositoryTestBase
{
    private readonly IBoardRepository _boardRepository;
    private readonly IColumnRepository _columnRepository;
    private readonly ICardRepository _repository;
    private readonly IUserRepository _userRepository;

    public CardRepositoryTests()
    {
        _repository = new CardRepository(ConnectionFactory);
        _columnRepository = new ColumnRepository(ConnectionFactory);
        _boardRepository = new BoardRepository(ConnectionFactory);
        _userRepository = new UserRepository(ConnectionFactory);
    }

    [Test]
    public async Task CreateAsync_WithValidData_CreatesCard()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();

        // Act
        Card card = await _repository.CreateAsync("Test Card", board.Id, column.Id);

        // Assert
        await Assert.That(card).IsNotNull();
        await Assert.That(card.Id).IsNotEmpty();
        await Assert.That(card.Title).IsEqualTo("Test Card");
        await Assert.That(card.BoardId).IsEqualTo(board.Id);
        await Assert.That(card.ColumnId).IsEqualTo(column.Id);
    }

    [Test]
    public async Task GetByIdAsync_WithExistingCard_ReturnsCard()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card created = await _repository.CreateAsync("Find Me", board.Id, column.Id);

        // Act
        Card? result = await _repository.GetByIdAsync(created.Id);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(created.Id);
        await Assert.That(result.Title).IsEqualTo("Find Me");
        await Assert.That(result.ColumnId).IsEqualTo(column.Id);
    }

    [Test]
    public async Task GetByIdAsync_WithNonExistentCard_ReturnsNull()
    {
        // Act
        Card? result = await _repository.GetByIdAsync("nonexistent-card-id");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task UpdateAsync_WithTitle_UpdatesTitle()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _repository.CreateAsync("Original Title", board.Id, column.Id);

        // Act
        await _repository.UpdateAsync(card.Id, "Updated Title", null);

        // Assert
        Card? updated = await _repository.GetByIdAsync(card.Id);
        await Assert.That(updated!.Title).IsEqualTo("Updated Title");
    }

    [Test]
    public async Task UpdateAsync_WithDescription_UpdatesDescription()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _repository.CreateAsync("Card", board.Id, column.Id);

        // Act
        await _repository.UpdateAsync(card.Id, "Card", "This is a description");

        // Assert
        Card? updated = await _repository.GetByIdAsync(card.Id);
        await Assert.That(updated!.Description).IsEqualTo("This is a description");
    }

    [Test]
    public async Task UpdateAsync_WithNullDescription_ClearsDescription()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _repository.CreateAsync("Card", board.Id, column.Id);
        await _repository.UpdateAsync(card.Id, "Card", "Initial description");

        // Act
        await _repository.UpdateAsync(card.Id, "Card", null);

        // Assert
        Card? updated = await _repository.GetByIdAsync(card.Id);
        await Assert.That(updated!.Description).IsNull();
    }

    [Test]
    public async Task UpdateAsync_WithBothTitleAndDescription_UpdatesBoth()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _repository.CreateAsync("Old Title", board.Id, column.Id);

        // Act
        await _repository.UpdateAsync(card.Id, "New Title", "New Description");

        // Assert
        Card? updated = await _repository.GetByIdAsync(card.Id);
        await Assert.That(updated!.Title).IsEqualTo("New Title");
        await Assert.That(updated.Description).IsEqualTo("New Description");
    }

    [Test]
    public async Task MoveToColumnAsync_WithoutPosition_MovesCard()
    {
        // Arrange
        (Board board, Column column1) = await CreateTestBoardAndColumn();
        Column column2 = await _columnRepository.CreateAsync("Column 2", board.Id, 1);
        Card card = await _repository.CreateAsync("Movable Card", board.Id, column1.Id);

        // Act
        await _repository.MoveToColumnAsync(card.Id, column2.Id);

        // Assert
        Card? moved = await _repository.GetByIdAsync(card.Id);
        await Assert.That(moved!.ColumnId).IsEqualTo(column2.Id);
    }

    [Test]
    public async Task MoveToColumnAsync_WithPosition_MovesCardAndSetsPosition()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card3 = await _repository.CreateAsync("Card 3", board.Id, column.Id);

        // Act - Move card3 to position 0
        await _repository.MoveToColumnAsync(card3.Id, column.Id, 0);

        // Assert
        Card? moved = await _repository.GetByIdAsync(card3.Id);
        await Assert.That(moved!.Position).IsEqualTo(0);
    }

    [Test]
    public async Task MoveToColumnAsync_ToSameColumn_ChangesPosition()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _repository.CreateAsync("Card", board.Id, column.Id);

        // Act
        await _repository.MoveToColumnAsync(card.Id, column.Id, 5);

        // Assert
        Card? moved = await _repository.GetByIdAsync(card.Id);
        await Assert.That(moved!.ColumnId).IsEqualTo(column.Id);
        await Assert.That(moved.Position).IsEqualTo(5);
    }

    [Test]
    public async Task DeleteAsync_WithExistingCard_RemovesCard()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _repository.CreateAsync("Card To Delete", board.Id, column.Id);

        // Verify it exists
        Card? beforeDelete = await _repository.GetByIdAsync(card.Id);
        await Assert.That(beforeDelete).IsNotNull();

        // Act
        await _repository.DeleteAsync(card.Id);

        // Assert
        Card? afterDelete = await _repository.GetByIdAsync(card.Id);
        await Assert.That(afterDelete).IsNull();
    }

    [Test]
    public async Task DeleteAsync_WithNonExistentCard_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _repository.DeleteAsync("nonexistent-card");
    }

    [Test]
    public async Task CreateAsync_WithMultipleCards_AllCreatedSuccessfully()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();

        // Act
        Card card1 = await _repository.CreateAsync("Card 1", board.Id, column.Id);
        Card card2 = await _repository.CreateAsync("Card 2", board.Id, column.Id);
        Card card3 = await _repository.CreateAsync("Card 3", board.Id, column.Id);

        // Assert
        await Assert.That(card1.Id).IsNotEqualTo(card2.Id);
        await Assert.That(card1.Id).IsNotEqualTo(card3.Id);
        await Assert.That(card2.Id).IsNotEqualTo(card3.Id);

        Card? retrieved1 = await _repository.GetByIdAsync(card1.Id);
        Card? retrieved2 = await _repository.GetByIdAsync(card2.Id);
        Card? retrieved3 = await _repository.GetByIdAsync(card3.Id);

        await Assert.That(retrieved1).IsNotNull();
        await Assert.That(retrieved2).IsNotNull();
        await Assert.That(retrieved3).IsNotNull();
    }

    [Test]
    public async Task CreateAsync_WithEmptyTitle_CreatesCard()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();

        // Act
        Card card = await _repository.CreateAsync("", board.Id, column.Id);

        // Assert
        await Assert.That(card).IsNotNull();
        await Assert.That(card.Title).IsEqualTo("");
    }

    [Test]
    public async Task CreateAsync_WithLongTitle_CreatesCard()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        string longTitle = new('A', 1000);

        // Act
        Card card = await _repository.CreateAsync(longTitle, board.Id, column.Id);

        // Assert
        await Assert.That(card).IsNotNull();
        await Assert.That(card.Title).IsEqualTo(longTitle);
    }

    [Test]
    public async Task UpdateAsync_WithLongDescription_UpdatesDescription()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _repository.CreateAsync("Card", board.Id, column.Id);
        string longDescription = new('B', 10000);

        // Act
        await _repository.UpdateAsync(card.Id, "Card", longDescription);

        // Assert
        Card? updated = await _repository.GetByIdAsync(card.Id);
        await Assert.That(updated!.Description).IsEqualTo(longDescription);
    }

    [Test]
    public async Task MoveToColumnAsync_WithNegativePosition_SetsNegativePosition()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _repository.CreateAsync("Card", board.Id, column.Id);

        // Act
        await _repository.MoveToColumnAsync(card.Id, column.Id, -5);

        // Assert
        Card? moved = await _repository.GetByIdAsync(card.Id);
        await Assert.That(moved!.Position).IsEqualTo(-5);
    }

    [Test]
    public async Task GetByIdAsync_ReturnsCardWithAllFields()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _repository.CreateAsync("Full Card", board.Id, column.Id);
        await _repository.UpdateAsync(card.Id, "Full Card", "Complete description");
        await _repository.MoveToColumnAsync(card.Id, column.Id, 10);

        // Act
        Card? result = await _repository.GetByIdAsync(card.Id);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(card.Id);
        await Assert.That(result.Title).IsEqualTo("Full Card");
        await Assert.That(result.Description).IsEqualTo("Complete description");
        await Assert.That(result.BoardId).IsEqualTo(board.Id);
        await Assert.That(result.ColumnId).IsEqualTo(column.Id);
        await Assert.That(result.Position).IsEqualTo(10);
    }

    [Test]
    public async Task MoveToColumnAsync_MultipleTimes_KeepsLatestPosition()
    {
        // Arrange
        (Board board, Column column1) = await CreateTestBoardAndColumn();
        Column column2 = await _columnRepository.CreateAsync("Column 2", board.Id, 1);
        Card card = await _repository.CreateAsync("Moving Card", board.Id, column1.Id);

        // Act
        await _repository.MoveToColumnAsync(card.Id, column2.Id, 0);
        await _repository.MoveToColumnAsync(card.Id, column1.Id, 5);
        await _repository.MoveToColumnAsync(card.Id, column2.Id, 3);

        // Assert
        Card? final = await _repository.GetByIdAsync(card.Id);
        await Assert.That(final!.ColumnId).IsEqualTo(column2.Id);
        await Assert.That(final.Position).IsEqualTo(3);
    }

    [Test]
    public async Task DeleteAsync_DoesNotAffectOtherCards()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card1 = await _repository.CreateAsync("Keep 1", board.Id, column.Id);
        Card card2 = await _repository.CreateAsync("Delete", board.Id, column.Id);
        Card card3 = await _repository.CreateAsync("Keep 2", board.Id, column.Id);

        // Act
        await _repository.DeleteAsync(card2.Id);

        // Assert
        Card? kept1 = await _repository.GetByIdAsync(card1.Id);
        Card? deleted = await _repository.GetByIdAsync(card2.Id);
        Card? kept2 = await _repository.GetByIdAsync(card3.Id);

        await Assert.That(kept1).IsNotNull();
        await Assert.That(deleted).IsNull();
        await Assert.That(kept2).IsNotNull();
    }

    private async Task<(Board board, Column column)> CreateTestBoardAndColumn()
    {
        User user = await CreateTestUser();
        Board board = await _boardRepository.CreateAsync("Test Board", user.Id);
        Column column = await _columnRepository.CreateAsync("Test Column", board.Id, 0);
        return (board, column);
    }

    private async Task<User> CreateTestUser()
    {
        string providerId = Guid.NewGuid().ToString();
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