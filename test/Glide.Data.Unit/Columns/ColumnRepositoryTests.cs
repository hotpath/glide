using Glide.Data.Boards;
using Glide.Data.Columns;
using Glide.Data.Users;

namespace Glide.Data.Unit.Columns;

public class ColumnRepositoryTests : RepositoryTestBase
{
    private readonly IBoardRepository _boardRepository;
    private readonly IColumnRepository _repository;
    private readonly IUserRepository _userRepository;

    public ColumnRepositoryTests()
    {
        _repository = new ColumnRepository(ConnectionFactory);
        _boardRepository = new BoardRepository(ConnectionFactory);
        _userRepository = new UserRepository(ConnectionFactory);
    }

    [Test]
    public async Task CreateAsync_WithValidData_CreatesColumn()
    {
        // Arrange
        Board board = await CreateTestBoard("Test Board");

        // Act
        Column column = await _repository.CreateAsync("To Do", board.Id, 0);

        // Assert
        await Assert.That(column).IsNotNull();
        await Assert.That(column.Id).IsNotEmpty();
        await Assert.That(column.Name).IsEqualTo("To Do");
        await Assert.That(column.BoardId).IsEqualTo(board.Id);
        await Assert.That(column.Position).IsEqualTo(0);
    }

    [Test]
    public async Task GetByIdAsync_WithExistingColumn_ReturnsColumn()
    {
        // Arrange
        Board board = await CreateTestBoard("Test Board");
        Column created = await _repository.CreateAsync("In Progress", board.Id, 1);

        // Act
        Column? result = await _repository.GetByIdAsync(created.Id);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(created.Id);
        await Assert.That(result.Name).IsEqualTo("In Progress");
        await Assert.That(result.Position).IsEqualTo(1);
    }

    [Test]
    public async Task GetByIdAsync_WithNonExistentColumn_ReturnsNull()
    {
        // Act
        Column? result = await _repository.GetByIdAsync("nonexistent-column-id");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetAllByBoardIdAsync_WithNoColumns_ReturnsEmpty()
    {
        // Arrange
        Board board = await CreateTestBoard("Empty Board");

        // Act
        IEnumerable<Column> result = (await _repository.GetAllByBoardIdAsync(board.Id)).ToList();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task GetAllByBoardIdAsync_WithMultipleColumns_ReturnsAllColumns()
    {
        // Arrange
        Board board = await CreateTestBoard("Multi Column Board");
        Column column1 = await _repository.CreateAsync("Todo", board.Id, 0);
        Column column2 = await _repository.CreateAsync("In Progress", board.Id, 1);
        Column column3 = await _repository.CreateAsync("Done", board.Id, 2);

        // Act
        IEnumerable<Column> result = await _repository.GetAllByBoardIdAsync(board.Id);

        // Assert
        List<Column> columns = result.ToList();
        await Assert.That(columns.Count).IsEqualTo(3);
        await Assert.That(columns.Any(c => c.Id == column1.Id)).IsTrue();
        await Assert.That(columns.Any(c => c.Id == column2.Id)).IsTrue();
        await Assert.That(columns.Any(c => c.Id == column3.Id)).IsTrue();
    }

    [Test]
    public async Task GetAllByBoardIdAsync_OrdersByPosition()
    {
        // Arrange
        Board board = await CreateTestBoard("Ordered Board");
        await _repository.CreateAsync("Third", board.Id, 2);
        await _repository.CreateAsync("First", board.Id, 0);
        await _repository.CreateAsync("Second", board.Id, 1);

        // Act
        IEnumerable<Column> result = await _repository.GetAllByBoardIdAsync(board.Id);

        // Assert
        List<Column> columns = result.ToList();
        await Assert.That(columns[0].Name).IsEqualTo("First");
        await Assert.That(columns[1].Name).IsEqualTo("Second");
        await Assert.That(columns[2].Name).IsEqualTo("Third");
    }

    [Test]
    public async Task GetAllByBoardIdAsync_OnlyReturnsBoardColumns()
    {
        // Arrange
        Board board1 = await CreateTestBoard("Board 1");
        Board board2 = await CreateTestBoard("Board 2");

        Column board1Column = await _repository.CreateAsync("Board 1 Column", board1.Id, 0);
        Column board2Column = await _repository.CreateAsync("Board 2 Column", board2.Id, 0);

        // Act
        IEnumerable<Column> board1Columns = await _repository.GetAllByBoardIdAsync(board1.Id);
        IEnumerable<Column> board2Columns = await _repository.GetAllByBoardIdAsync(board2.Id);

        // Assert
        List<Column> board1List = board1Columns.ToList();
        List<Column> board2List = board2Columns.ToList();

        await Assert.That(board1List.Count).IsEqualTo(1);
        await Assert.That(board2List.Count).IsEqualTo(1);

        await Assert.That(board1List[0].Id).IsEqualTo(board1Column.Id);
        await Assert.That(board2List[0].Id).IsEqualTo(board2Column.Id);
    }

    [Test]
    public async Task GetMaxPositionAsync_WithNoColumns_ReturnsMinusOne()
    {
        // Arrange
        Board board = await CreateTestBoard("Empty Board");

        // Act
        int maxPosition = await _repository.GetMaxPositionAsync(board.Id);

        // Assert
        await Assert.That(maxPosition).IsEqualTo(-1);
    }

    [Test]
    public async Task GetMaxPositionAsync_WithColumns_ReturnsHighestPosition()
    {
        // Arrange
        Board board = await CreateTestBoard("Position Board");
        await _repository.CreateAsync("Column 1", board.Id, 0);
        await _repository.CreateAsync("Column 2", board.Id, 5);
        await _repository.CreateAsync("Column 3", board.Id, 3);

        // Act
        int maxPosition = await _repository.GetMaxPositionAsync(board.Id);

        // Assert
        await Assert.That(maxPosition).IsEqualTo(5);
    }

    [Test]
    public async Task CreateDefaultColumnsAsync_CreatesThreeColumns()
    {
        // Arrange
        Board board = await CreateTestBoard("Default Columns Board");

        // Act
        await _repository.CreateDefaultColumnsAsync(board.Id);

        // Assert
        IEnumerable<Column> columns = await _repository.GetAllByBoardIdAsync(board.Id);
        List<Column> columnList = columns.ToList();

        await Assert.That(columnList.Count).IsEqualTo(3);
    }

    [Test]
    public async Task CreateDefaultColumnsAsync_CreatesColumnsWithCorrectNames()
    {
        // Arrange
        Board board = await CreateTestBoard("Named Columns Board");

        // Act
        await _repository.CreateDefaultColumnsAsync(board.Id);

        // Assert
        IEnumerable<Column> columns = await _repository.GetAllByBoardIdAsync(board.Id);
        List<string> columnNames = columns.Select(c => c.Name).ToList();

        await Assert.That(columnNames).Contains("To Do");
        await Assert.That(columnNames).Contains("In Progress");
        await Assert.That(columnNames).Contains("Done");
    }

    [Test]
    public async Task CreateDefaultColumnsAsync_CreatesColumnsInCorrectOrder()
    {
        // Arrange
        Board board = await CreateTestBoard("Ordered Default Board");

        // Act
        await _repository.CreateDefaultColumnsAsync(board.Id);

        // Assert
        List<Column> columns = (await _repository.GetAllByBoardIdAsync(board.Id)).ToList();

        await Assert.That(columns[0].Name).IsEqualTo("To Do");
        await Assert.That(columns[0].Position).IsEqualTo(0);

        await Assert.That(columns[1].Name).IsEqualTo("In Progress");
        await Assert.That(columns[1].Position).IsEqualTo(1);

        await Assert.That(columns[2].Name).IsEqualTo("Done");
        await Assert.That(columns[2].Position).IsEqualTo(2);
    }

    [Test]
    public async Task DeleteAsync_WithExistingColumn_RemovesColumn()
    {
        // Arrange
        Board board = await CreateTestBoard("Delete Test Board");
        Column column = await _repository.CreateAsync("Column To Delete", board.Id, 0);

        // Verify it exists
        Column? beforeDelete = await _repository.GetByIdAsync(column.Id);
        await Assert.That(beforeDelete).IsNotNull();

        // Act
        await _repository.DeleteAsync(column.Id);

        // Assert
        Column? afterDelete = await _repository.GetByIdAsync(column.Id);
        await Assert.That(afterDelete).IsNull();

        IEnumerable<Column> boardColumns = await _repository.GetAllByBoardIdAsync(board.Id);
        await Assert.That(boardColumns.Any(c => c.Id == column.Id)).IsFalse();
    }

    [Test]
    public async Task DeleteAsync_WithNonExistentColumn_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _repository.DeleteAsync("nonexistent-column");
    }

    [Test]
    public async Task DeleteAsync_OnlyDeletesSpecifiedColumn()
    {
        // Arrange
        Board board = await CreateTestBoard("Multi Delete Board");
        Column column1 = await _repository.CreateAsync("Keep This", board.Id, 0);
        Column column2 = await _repository.CreateAsync("Delete This", board.Id, 1);

        // Act
        await _repository.DeleteAsync(column2.Id);

        // Assert
        Column? remaining = await _repository.GetByIdAsync(column1.Id);
        Column? deleted = await _repository.GetByIdAsync(column2.Id);

        await Assert.That(remaining).IsNotNull();
        await Assert.That(deleted).IsNull();
    }

    [Test]
    public async Task CreateAsync_WithDuplicatePositions_AllowsBoth()
    {
        // Arrange
        Board board = await CreateTestBoard("Duplicate Position Board");

        // Act
        Column column1 = await _repository.CreateAsync("Column A", board.Id, 0);
        Column column2 = await _repository.CreateAsync("Column B", board.Id, 0);

        // Assert
        await Assert.That(column1.Position).IsEqualTo(0);
        await Assert.That(column2.Position).IsEqualTo(0);

        IEnumerable<Column> columns = await _repository.GetAllByBoardIdAsync(board.Id);
        await Assert.That(columns.Count()).IsEqualTo(2);
    }

    [Test]
    public async Task CreateAsync_WithNegativePosition_CreatesColumn()
    {
        // Arrange
        Board board = await CreateTestBoard("Negative Position Board");

        // Act
        Column column = await _repository.CreateAsync("Negative", board.Id, -5);

        // Assert
        await Assert.That(column.Position).IsEqualTo(-5);
    }

    [Test]
    public async Task GetMaxPositionAsync_OnlyConsidersBoardColumns()
    {
        // Arrange
        Board board1 = await CreateTestBoard("Board 1");
        Board board2 = await CreateTestBoard("Board 2");

        await _repository.CreateAsync("Board 1 Column", board1.Id, 10);
        await _repository.CreateAsync("Board 2 Column", board2.Id, 100);

        // Act
        int board1Max = await _repository.GetMaxPositionAsync(board1.Id);
        int board2Max = await _repository.GetMaxPositionAsync(board2.Id);

        // Assert
        await Assert.That(board1Max).IsEqualTo(10);
        await Assert.That(board2Max).IsEqualTo(100);
    }

    private async Task<Board> CreateTestBoard(string name)
    {
        User user = await CreateTestUser();
        return await _boardRepository.CreateAsync(name, user.Id);
    }

    private async Task<User> CreateTestUser()
    {
        string providerId = Guid.NewGuid().ToString();
        User user = new()
        {
            Id = Guid.NewGuid().ToString(),
            OAuthProvider = "forgejo",
            OAuthProviderId = providerId,
            DisplayName = "Test User",
            Email = $"{providerId}@example.com"
        };
        await _userRepository.Create(user);
        return user;
    }
}