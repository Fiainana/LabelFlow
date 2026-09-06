using LabelFlow.Models;
using Microsoft.Data.SqlClient;

namespace LabelFlow.Services;

public sealed class ArticlePageResult
{
    public IReadOnlyList<Article> Articles { get; init; } = Array.Empty<Article>();
    public int TotalCount { get; init; }
    public bool HasMore { get; init; }
}

public static class ArticleService
{
    public const int PageSize = 50;
    private const int MaxInClause = 500;

    private const string SqlCount = """
        SELECT COUNT(*)
        FROM F_ARTICLE A
        WHERE A.AR_Sommeil = 0
          AND (
                @Search IS NULL OR @Search = ''
                OR A.AR_Ref LIKE '%' + @Search + '%'
                OR A.AR_Design LIKE '%' + @Search + '%'
                OR A.AR_CodeBarre LIKE '%' + @Search + '%'
              )
        """;

    private const string SqlPage = """
        SELECT
            A.AR_Ref       AS Reference,
            A.AR_Design    AS Designation,
            A.AR_CodeBarre AS CodeBarre,
            A.AR_PrixVen   AS PrixVente,
            U.U_Intitule   AS UniteVente
        FROM F_ARTICLE A
        LEFT JOIN P_UNITE U ON A.AR_UniteVen = U.cbIndice
        WHERE A.AR_Sommeil = 0
          AND (
                @Search IS NULL OR @Search = ''
                OR A.AR_Ref LIKE '%' + @Search + '%'
                OR A.AR_Design LIKE '%' + @Search + '%'
                OR A.AR_CodeBarre LIKE '%' + @Search + '%'
              )
        ORDER BY A.AR_Ref
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
        """;

    public static async Task<ArticlePageResult> GetActiveArticlesPageAsync(
        int pageIndex, string? search = null, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));

        var config = ConfigService.Load()
            ?? throw new InvalidOperationException("Aucune configuration de connexion trouvée.");

        string? searchTerm = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        int offset = pageIndex * PageSize;

        await using var connection = SqlConnectionFactory.Create(config);
        await connection.OpenAsync(cancellationToken);

        int totalCount = await GetTotalCountAsync(connection, searchTerm, cancellationToken);
        var articles = await GetPageAsync(connection, offset, searchTerm, cancellationToken);

        return new ArticlePageResult
        {
            Articles = articles,
            TotalCount = totalCount,
            HasMore = offset + articles.Count < totalCount
        };
    }

    public static async Task<IReadOnlyList<Article>> GetByReferencesAsync(
        IEnumerable<string> references, CancellationToken cancellationToken = default)
    {
        var refs = references.Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (refs.Count == 0) return Array.Empty<Article>();

        var config = ConfigService.Load()
            ?? throw new InvalidOperationException("Aucune configuration de connexion trouvée.");

        await using var connection = SqlConnectionFactory.Create(config);
        await connection.OpenAsync(cancellationToken);

        var all = new List<Article>(refs.Count);

        foreach (var batch in refs.Chunk(MaxInClause))
        {
            var batchList = batch.ToList();
            await using var command = new SqlCommand { Connection = connection };

            var parameters = new List<string>(batchList.Count);
            for (int i = 0; i < batchList.Count; i++)
            {
                string paramName = $"@r{i}";
                parameters.Add(paramName);
                command.Parameters.AddWithValue(paramName, batchList[i]);
            }

            command.CommandText = $"""
                SELECT A.AR_Ref AS Reference, A.AR_Design AS Designation, A.AR_CodeBarre AS CodeBarre,
                       A.AR_PrixVen AS PrixVente, U.U_Intitule AS UniteVente
                FROM F_ARTICLE A
                LEFT JOIN P_UNITE U ON A.AR_UniteVen = U.cbIndice
                WHERE A.AR_Sommeil = 0 AND A.AR_Ref IN ({string.Join(", ", parameters)})
                ORDER BY A.AR_Ref
                """;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                all.Add(MapArticle(reader));
        }

        return all;
    }

    private static async Task<int> GetTotalCountAsync(SqlConnection connection, string? search, CancellationToken ct)
    {
        await using var command = new SqlCommand(SqlCount, connection);
        command.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }

    private static async Task<List<Article>> GetPageAsync(SqlConnection connection, int offset, string? search, CancellationToken ct)
    {
        var articles = new List<Article>(PageSize);
        await using var command = new SqlCommand(SqlPage, connection);
        command.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
        command.Parameters.AddWithValue("@Offset", offset);
        command.Parameters.AddWithValue("@PageSize", PageSize);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            articles.Add(MapArticle(reader));
        return articles;
    }

    private static Article MapArticle(SqlDataReader reader) => new()
    {
        Reference = reader["Reference"]?.ToString() ?? string.Empty,
        Designation = reader["Designation"]?.ToString() ?? string.Empty,
        CodeBarre = reader.IsDBNull(reader.GetOrdinal("CodeBarre")) ? null : reader["CodeBarre"]?.ToString(),
        PrixVente = reader.IsDBNull(reader.GetOrdinal("PrixVente")) ? 0m : Convert.ToDecimal(reader["PrixVente"]),
        UniteVente = reader.IsDBNull(reader.GetOrdinal("UniteVente")) ? null : reader["UniteVente"]?.ToString()
    };
}
