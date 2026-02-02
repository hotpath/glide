using Glide.Data.Boards;
using Glide.Data.Cards;
using Glide.Data.Columns;
using Glide.Data.Labels;
using Glide.Data.Users;

namespace Glide.Data.Unit.Labels;

public class LabelRepositoryTests : RepositoryTestBase
{
    private readonly ILabelRepository _repository;
    private readonly IBoardRepository _boardRepository;
    private readonly IColumnRepository _columnRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IUserRepository _userRepository;

    public LabelRepositoryTests()
    {
        _repository = new LabelRepository(ConnectionFactory);
        _boardRepository = new BoardRepository(ConnectionFactory);
        _columnRepository = new ColumnRepository(ConnectionFactory);
        _cardRepository = new CardRepository(ConnectionFactory);
        _userRepository = new UserRepository(ConnectionFactory);
    }

    [Test]
    public async Task CreateAsync_WithValidData_CreatesLabel()
    {
        // Arrange
        Board board = await CreateTestBoard();

        // Act
        Label label = await _repository.CreateAsync(board.Id, "Bug", "#ff0000", "🐛");

        // Assert
        await Assert.That(label).IsNotNull();
        await Assert.That(label.Id).IsNotEmpty();
        await Assert.That(label.BoardId).IsEqualTo(board.Id);
        await Assert.That(label.Name).IsEqualTo("Bug");
        await Assert.That(label.Color).IsEqualTo("#ff0000");
        await Assert.That(label.Icon).IsEqualTo("🐛");
    }

    [Test]
    public async Task CreateAsync_WithoutIcon_CreatesLabelWithNullIcon()
    {
        // Arrange
        Board board = await CreateTestBoard();

        // Act
        Label label = await _repository.CreateAsync(board.Id, "Feature", "#00ff00", null);

        // Assert
        await Assert.That(label).IsNotNull();
        await Assert.That(label.Name).IsEqualTo("Feature");
        await Assert.That(label.Icon).IsNull();
    }

    [Test]
    public async Task GetByIdAsync_WithExistingLabel_ReturnsLabel()
    {
        // Arrange
        Board board = await CreateTestBoard();
        Label created = await _repository.CreateAsync(board.Id, "Enhancement", "#0000ff", "✨");

        // Act
        Label? result = await _repository.GetByIdAsync(created.Id);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(created.Id);
        await Assert.That(result.Name).IsEqualTo("Enhancement");
        await Assert.That(result.Color).IsEqualTo("#0000ff");
        await Assert.That(result.Icon).IsEqualTo("✨");
    }

    [Test]
    public async Task GetByIdAsync_WithNonExistentLabel_ReturnsNull()
    {
        // Act
        Label? result = await _repository.GetByIdAsync("nonexistent-label-id");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetByBoardIdAsync_WithMultipleLabels_ReturnsAllLabelsForBoard()
    {
        // Arrange
        Board board1 = await CreateTestBoard();
        Board board2 = await CreateTestBoard();

        await _repository.CreateAsync(board1.Id, "Bug", "#ff0000", "🐛");
        await _repository.CreateAsync(board1.Id, "Feature", "#00ff00", "✨");
        await _repository.CreateAsync(board2.Id, "Task", "#0000ff", "📋");

        // Act
        IEnumerable<Label> labels = await _repository.GetByBoardIdAsync(board1.Id);

        // Assert
        List<Label> labelList = labels.ToList();
        await Assert.That(labelList.Count).IsEqualTo(2);
        await Assert.That(labelList.Any(l => l.Name == "Bug")).IsTrue();
        await Assert.That(labelList.Any(l => l.Name == "Feature")).IsTrue();
    }

    [Test]
    public async Task GetByBoardIdAsync_WithNoLabels_ReturnsEmptyList()
    {
        // Arrange
        Board board = await CreateTestBoard();

        // Act
        IEnumerable<Label> labels = await _repository.GetByBoardIdAsync(board.Id);

        // Assert
        await Assert.That(labels.Any()).IsFalse();
    }

    [Test]
    public async Task GetByBoardIdAsync_ReturnsLabelsSortedByName()
    {
        // Arrange
        Board board = await CreateTestBoard();
        await _repository.CreateAsync(board.Id, "Zebra", "#ff0000", null);
        await _repository.CreateAsync(board.Id, "Apple", "#00ff00", null);
        await _repository.CreateAsync(board.Id, "Mango", "#0000ff", null);

        // Act
        IEnumerable<Label> labels = await _repository.GetByBoardIdAsync(board.Id);

        // Assert
        List<Label> labelList = labels.ToList();
        await Assert.That(labelList[0].Name).IsEqualTo("Apple");
        await Assert.That(labelList[1].Name).IsEqualTo("Mango");
        await Assert.That(labelList[2].Name).IsEqualTo("Zebra");
    }

    [Test]
    public async Task UpdateAsync_WithAllFields_UpdatesLabel()
    {
        // Arrange
        Board board = await CreateTestBoard();
        Label label = await _repository.CreateAsync(board.Id, "Old Name", "#111111", "🔧");

        // Act
        await _repository.UpdateAsync(label.Id, "New Name", "#222222", "🎨");

        // Assert
        Label? updated = await _repository.GetByIdAsync(label.Id);
        await Assert.That(updated!.Name).IsEqualTo("New Name");
        await Assert.That(updated.Color).IsEqualTo("#222222");
        await Assert.That(updated.Icon).IsEqualTo("🎨");
    }

    [Test]
    public async Task UpdateAsync_WithNullIcon_SetsIconToNull()
    {
        // Arrange
        Board board = await CreateTestBoard();
        Label label = await _repository.CreateAsync(board.Id, "Label", "#ffffff", "⭐");

        // Act
        await _repository.UpdateAsync(label.Id, "Label", "#ffffff", null);

        // Assert
        Label? updated = await _repository.GetByIdAsync(label.Id);
        await Assert.That(updated!.Icon).IsNull();
    }

    [Test]
    public async Task DeleteAsync_WithExistingLabel_RemovesLabel()
    {
        // Arrange
        Board board = await CreateTestBoard();
        Label label = await _repository.CreateAsync(board.Id, "To Delete", "#000000", null);

        Label? beforeDelete = await _repository.GetByIdAsync(label.Id);
        await Assert.That(beforeDelete).IsNotNull();

        // Act
        await _repository.DeleteAsync(label.Id);

        // Assert
        Label? afterDelete = await _repository.GetByIdAsync(label.Id);
        await Assert.That(afterDelete).IsNull();
    }

    [Test]
    public async Task DeleteAsync_WithNonExistentLabel_DoesNotThrow()
    {
        // Act & Assert
        await _repository.DeleteAsync("nonexistent-label");
    }

    [Test]
    public async Task AddLabelToCardAsync_WithValidIds_CreatesAssociation()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _cardRepository.CreateAsync("Card", board.Id, column.Id);
        Label label = await _repository.CreateAsync(board.Id, "Label", "#ff0000", null);

        // Act
        await _repository.AddLabelToCardAsync(card.Id, label.Id);

        // Assert
        IEnumerable<Label> labels = await _repository.GetLabelsByCardIdAsync(card.Id);
        await Assert.That(labels.Any(l => l.Id == label.Id)).IsTrue();
    }

    [Test]
    public async Task AddLabelToCardAsync_WithDuplicateAssociation_DoesNotThrow()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _cardRepository.CreateAsync("Card", board.Id, column.Id);
        Label label = await _repository.CreateAsync(board.Id, "Label", "#ff0000", null);

        // Act
        await _repository.AddLabelToCardAsync(card.Id, label.Id);
        await _repository.AddLabelToCardAsync(card.Id, label.Id); // Duplicate

        // Assert
        IEnumerable<Label> labels = await _repository.GetLabelsByCardIdAsync(card.Id);
        await Assert.That(labels.Count()).IsEqualTo(1);
    }

    [Test]
    public async Task RemoveLabelFromCardAsync_WithExistingAssociation_RemovesAssociation()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _cardRepository.CreateAsync("Card", board.Id, column.Id);
        Label label = await _repository.CreateAsync(board.Id, "Label", "#ff0000", null);
        await _repository.AddLabelToCardAsync(card.Id, label.Id);

        // Act
        await _repository.RemoveLabelFromCardAsync(card.Id, label.Id);

        // Assert
        IEnumerable<Label> labels = await _repository.GetLabelsByCardIdAsync(card.Id);
        await Assert.That(labels.Any()).IsFalse();
    }

    [Test]
    public async Task RemoveLabelFromCardAsync_WithNonExistentAssociation_DoesNotThrow()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _cardRepository.CreateAsync("Card", board.Id, column.Id);
        Label label = await _repository.CreateAsync(board.Id, "Label", "#ff0000", null);

        // Act & Assert
        await _repository.RemoveLabelFromCardAsync(card.Id, label.Id);
    }

    [Test]
    public async Task GetLabelsByCardIdAsync_WithMultipleLabels_ReturnsAllLabels()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _cardRepository.CreateAsync("Card", board.Id, column.Id);
        Label label1 = await _repository.CreateAsync(board.Id, "Bug", "#ff0000", "🐛");
        Label label2 = await _repository.CreateAsync(board.Id, "Feature", "#00ff00", "✨");
        Label label3 = await _repository.CreateAsync(board.Id, "Task", "#0000ff", "📋");

        await _repository.AddLabelToCardAsync(card.Id, label1.Id);
        await _repository.AddLabelToCardAsync(card.Id, label3.Id);

        // Act
        IEnumerable<Label> labels = await _repository.GetLabelsByCardIdAsync(card.Id);

        // Assert
        List<Label> labelList = labels.ToList();
        await Assert.That(labelList.Count).IsEqualTo(2);
        await Assert.That(labelList.Any(l => l.Id == label1.Id)).IsTrue();
        await Assert.That(labelList.Any(l => l.Id == label3.Id)).IsTrue();
        await Assert.That(labelList.Any(l => l.Id == label2.Id)).IsFalse();
    }

    [Test]
    public async Task GetLabelsByCardIdAsync_WithNoLabels_ReturnsEmptyList()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _cardRepository.CreateAsync("Card", board.Id, column.Id);

        // Act
        IEnumerable<Label> labels = await _repository.GetLabelsByCardIdAsync(card.Id);

        // Assert
        await Assert.That(labels.Any()).IsFalse();
    }

    [Test]
    public async Task GetLabelsByCardIdAsync_ReturnsSortedByName()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _cardRepository.CreateAsync("Card", board.Id, column.Id);
        Label label1 = await _repository.CreateAsync(board.Id, "Zebra", "#ff0000", null);
        Label label2 = await _repository.CreateAsync(board.Id, "Apple", "#00ff00", null);

        await _repository.AddLabelToCardAsync(card.Id, label1.Id);
        await _repository.AddLabelToCardAsync(card.Id, label2.Id);

        // Act
        IEnumerable<Label> labels = await _repository.GetLabelsByCardIdAsync(card.Id);

        // Assert
        List<Label> labelList = labels.ToList();
        await Assert.That(labelList[0].Name).IsEqualTo("Apple");
        await Assert.That(labelList[1].Name).IsEqualTo("Zebra");
    }

    [Test]
    public async Task GetCardIdsByLabelIdAsync_WithMultipleCards_ReturnsAllCardIds()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card1 = await _cardRepository.CreateAsync("Card 1", board.Id, column.Id);
        Card card2 = await _cardRepository.CreateAsync("Card 2", board.Id, column.Id);
        Card card3 = await _cardRepository.CreateAsync("Card 3", board.Id, column.Id);
        Label label = await _repository.CreateAsync(board.Id, "Bug", "#ff0000", "🐛");

        await _repository.AddLabelToCardAsync(card1.Id, label.Id);
        await _repository.AddLabelToCardAsync(card3.Id, label.Id);

        // Act
        IEnumerable<string> cardIds = await _repository.GetCardIdsByLabelIdAsync(label.Id);

        // Assert
        List<string> cardIdList = cardIds.ToList();
        await Assert.That(cardIdList.Count).IsEqualTo(2);
        await Assert.That(cardIdList.Contains(card1.Id)).IsTrue();
        await Assert.That(cardIdList.Contains(card3.Id)).IsTrue();
        await Assert.That(cardIdList.Contains(card2.Id)).IsFalse();
    }

    [Test]
    public async Task GetCardIdsByLabelIdAsync_WithNoCards_ReturnsEmptyList()
    {
        // Arrange
        Board board = await CreateTestBoard();
        Label label = await _repository.CreateAsync(board.Id, "Label", "#ff0000", null);

        // Act
        IEnumerable<string> cardIds = await _repository.GetCardIdsByLabelIdAsync(label.Id);

        // Assert
        await Assert.That(cardIds.Any()).IsFalse();
    }

    [Test]
    public async Task DeleteLabel_RemovesCardLabelAssociations()
    {
        // Arrange
        (Board board, Column column) = await CreateTestBoardAndColumn();
        Card card = await _cardRepository.CreateAsync("Card", board.Id, column.Id);
        Label label = await _repository.CreateAsync(board.Id, "Label", "#ff0000", null);
        await _repository.AddLabelToCardAsync(card.Id, label.Id);

        // Act
        await _repository.DeleteAsync(label.Id);

        // Assert
        IEnumerable<Label> labels = await _repository.GetLabelsByCardIdAsync(card.Id);
        await Assert.That(labels.Any()).IsFalse();
    }

    [Test]
    public async Task DeleteBoard_CascadesDeleteToLabels()
    {
        // Arrange
        Board board = await CreateTestBoard();
        Label label = await _repository.CreateAsync(board.Id, "Label", "#ff0000", null);

        // Verify label exists
        Label? beforeDelete = await _repository.GetByIdAsync(label.Id);
        await Assert.That(beforeDelete).IsNotNull();

        // Act
        await _boardRepository.DeleteAsync(board.Id);

        // Assert
        Label? afterDelete = await _repository.GetByIdAsync(label.Id);
        await Assert.That(afterDelete).IsNull();
    }

    [Test]
    public async Task CreateAsync_GeneratesUniqueIds()
    {
        // Arrange
        Board board = await CreateTestBoard();

        // Act
        Label label1 = await _repository.CreateAsync(board.Id, "Label 1", "#ff0000", null);
        Label label2 = await _repository.CreateAsync(board.Id, "Label 2", "#00ff00", null);
        Label label3 = await _repository.CreateAsync(board.Id, "Label 3", "#0000ff", null);

        // Assert
        await Assert.That(label1.Id).IsNotEqualTo(label2.Id);
        await Assert.That(label1.Id).IsNotEqualTo(label3.Id);
        await Assert.That(label2.Id).IsNotEqualTo(label3.Id);
    }

    private async Task<Board> CreateTestBoard()
    {
        User user = await CreateTestUser();
        return await _boardRepository.CreateAsync("Test Board", user.Id);
    }

    private async Task<(Board board, Column column)> CreateTestBoardAndColumn()
    {
        Board board = await CreateTestBoard();
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
