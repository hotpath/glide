using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using Dapper;

namespace Glide.Data.Boards;

public class BoardRepository(IDbConnectionFactory connectionFactory) : IBoardRepository
{
    public async Task<Board> CreateAsync(string name, string userId)
    {
        string id = Guid.CreateVersion7().ToString();

        const string statement = "INSERT INTO boards(id, name) VALUES(@Id, @Name)";

        using IDbConnection conn = connectionFactory.CreateConnection();
        conn.Open();
        using IDbTransaction transaction = conn.BeginTransaction();
        await conn.ExecuteAsync(statement, new { Id = id, Name = name }, transaction);

        const string ownerStatement =
            "INSERT INTO boards_users(board_id, user_id, is_owner) VALUES (@BoardId, @UserId, 1)";

        await conn.ExecuteAsync(ownerStatement, new { BoardId = id, UserId = userId }, transaction);
        transaction.Commit();

        return new Board
        {
            Id = id,
            Name = name,
            BoardUsers = [new BoardUser { BoardId = id, UserId = userId, IsOwner = true }]
        };
    }

    public async Task<IEnumerable<Board>> GetByUserIdAsync(string userId)
    {
        const string query = """
                             SELECT b.id AS Id, b.name AS Name,
                                    bu.board_id AS BoardId, bu.user_id AS UserId, bu.is_owner AS IsOwner
                             FROM boards b
                             JOIN main.boards_users bu on b.id = bu.board_id
                             WHERE bu.user_id = @UserId
                             """;
        using IDbConnection conn = connectionFactory.CreateConnection();
        IEnumerable<Board> results = await conn.QueryAsync<Board, BoardUser, Board>(
            query,
            (board, boardUser) =>
            {
                board.BoardUsers = [boardUser];
                return board;
            },
            new { UserId = userId },
            splitOn: "BoardId"
        );

        // Group by board ID to aggregate multiple BoardUsers per board
        return results
            .GroupBy(b => b.Id)
            .Select(g =>
            {
                Board first = g.First();
                first.BoardUsers = g.SelectMany(b => b.BoardUsers).ToList();
                return first;
            });
    }

    public async Task<Board?> GetByIdAsync(string boardId)
    {
        const string query = """
                             SELECT b.id AS Id, b.name AS Name,
                                    bu.board_id AS BoardId, bu.user_id AS UserId, bu.is_owner AS IsOwner
                             FROM boards b
                             JOIN main.boards_users bu on b.id = bu.board_id
                             where b.id = @BoardId
                             """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        IEnumerable<Board> results = await conn.QueryAsync<Board, BoardUser, Board>(
            query,
            (board, boardUser) =>
            {
                board.BoardUsers = [boardUser];
                return board;
            },
            new { BoardId = boardId },
            splitOn: "BoardId");

        // Group by board ID to aggregate multiple BoardUsers per board
        IEnumerable<Board> boards = results
            .GroupBy(b => b.Id)
            .Select(g =>
            {
                Board first = g.First();
                first.BoardUsers = g.SelectMany(b => b.BoardUsers).ToList();
                return first;
            });

        return boards.SingleOrDefault();
    }

    public async Task<Board?> UpdateAsync(string id, string name)
    {
        const string statement = """
                                 UPDATE boards
                                 SET name= @Name
                                 WHERE id = @Id
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { Id = id, Name = name });
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(string boardId)
    {
        const string statement = """
                                 DELETE FROM boards WHERE id = @BoardId
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { BoardId = boardId });
    }

    public async Task<IEnumerable<BoardUser>> GetBoardUsersAsync(string boardId)
    {
        const string query = """
                             SELECT board_id AS BoardId, user_id AS UserId, is_owner AS IsOwner
                             FROM boards_users
                             WHERE board_id = @BoardId
                             """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<BoardUser>(query, new { BoardId = boardId });
    }

    public async Task<IEnumerable<BoardMemberDetails>> GetBoardMembersWithEmailAsync(string boardId)
    {
        const string query = """
                             SELECT bu.user_id AS UserId, u.email AS Email, bu.is_owner AS IsOwner
                             FROM boards_users bu
                             JOIN users u ON bu.user_id = u.id
                             WHERE bu.board_id = @BoardId
                             ORDER BY u.email
                             """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        return await conn.QueryAsync<BoardMemberDetails>(query, new { BoardId = boardId });
    }

    public async Task AddUserToBoardAsync(string boardId, string userId, bool isOwner)
    {
        const string statement = """
                                 INSERT INTO boards_users(board_id, user_id, is_owner) 
                                 VALUES (@BoardId, @UserId, @IsOwner)
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { BoardId = boardId, UserId = userId, IsOwner = isOwner });
    }

    public async Task UpdateUserRoleAsync(string boardId, string userId, bool isOwner)
    {
        const string statement = """
                                 UPDATE boards_users
                                 SET is_owner = @IsOwner
                                 WHERE board_id = @BoardId AND user_id = @UserId
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { BoardId = boardId, UserId = userId, IsOwner = isOwner });
    }

    public async Task RemoveUserFromBoardAsync(string boardId, string userId)
    {
        const string statement = """
                                 DELETE FROM boards_users
                                 WHERE board_id = @BoardId AND user_id = @UserId
                                 """;

        using IDbConnection conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(statement, new { BoardId = boardId, UserId = userId });
    }
}

public record Board
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public IEnumerable<BoardUser> BoardUsers { get; set; } = [];
}

public record BoardUser
{
    public string BoardId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
}

public record BoardMemberDetails
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
}