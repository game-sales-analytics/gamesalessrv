using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using App.DB.Models;

namespace App.DB.Repository
{
    public class GameSalesRepository : IGameSalesRepository
    {
        private readonly GameSalesContext _context;

        private readonly ILogger<GameSalesRepository> _logger;

        public GameSalesRepository(
            GameSalesContext context,
            ILogger<GameSalesRepository> logger
        )
        {
            _context = context;
            _logger = logger;
        }

        public async Task SaveBulkGameSalesAsync(IEnumerable<App.Models.GameSale> gameSales, CancellationToken ct)
        {
            var temp = gameSales.Select(g => new App.DB.Models.GameSale
            {
                EuSales = g.EuSales,
                Genre = g.Genre,
                GlobalSales = g.GlobalSales,
                OtherSales = g.OtherSales,
                JpSales = g.JpSales,
                Id = g.Id,
                Name = g.Name,
                Rank = g.Rank,
                Publisher = g.Publisher,
                Year = g.Year,
                RegisteredAt = g.RegisteredAt,
                Platform = g.Platform,
                NaSales = g.NaSales,
            });
            await _context.GameSales.AddRangeAsync(temp, ct);

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                switch (ex.InnerException)
                {
                    case PostgresException exception when exception.Severity == "ERROR" && exception.SqlState == PostgresErrorCodes.UniqueViolation:
                        throw new DuplicateRecordException();
                }

                throw ex;
            }
        }

        public async Task<IEnumerable<Models.GameSale>> SearchGameByName(string name, CancellationToken ct)
        {
            return await _context.GameSales
                .AsNoTracking()
                .Where(g => EF.Functions.ToTsVector("english", g.Name).Matches(name))
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Models.GameSale>> GetGameSalesWithMoreSalesInEUThanNA(CancellationToken ct)
        {
            return await _context.GameSales.AsNoTracking().Where(g => g.EuSales > g.NaSales).ToListAsync(ct);
        }

        public async Task<GameSale?> GetGameSalesByRank(ulong rank, CancellationToken ct)
        {
            try
            {
                return await _context.GameSales.AsNoTracking().SingleOrDefaultAsync(g => g.Rank.Equals(rank), ct);
            }
            catch (System.ArgumentNullException)
            {
                return await Task.FromResult<GameSale?>(null);
            }
            catch (System.InvalidOperationException ex)
            {
                _logger.LogError(ex, "multiple games with the same rank found");
                return null;
            }
        }
    }
}