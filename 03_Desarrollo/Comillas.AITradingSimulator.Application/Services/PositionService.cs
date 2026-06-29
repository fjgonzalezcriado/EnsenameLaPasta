using System.Globalization;
using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Domain.Entities;
using Comillas.AITradingSimulator.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Comillas.AITradingSimulator.Application.Services;

public sealed class PositionService : IPositionService
{
    private readonly ITradingDbContext _db;
    private readonly IWatchlistService _watchlist;
    private readonly TimeProvider _time;

    public PositionService(ITradingDbContext db, IWatchlistService watchlist, TimeProvider time)
    {
        _db = db;
        _watchlist = watchlist;
        _time = time;
    }

    public async Task<Guid> OpenAsync(string symbol, decimal entryPrice, decimal quantity, DateTime? openedAtUtc = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol es obligatorio.", nameof(symbol));

        var normalized = symbol.Trim().ToUpperInvariant();

        // Asegura que el símbolo se sigue (así el generador obtiene su precio en vivo).
        await _watchlist.AddAsync(normalized, string.Empty, cancellationToken);

        var openedAt = openedAtUtc ?? _time.GetUtcNow().UtcDateTime;
        var trade = Trade.Open(normalized, entryPrice, quantity, openedAt);
        _db.Trades.Add(trade);
        await _db.SaveChangesAsync(cancellationToken);
        return trade.Id;
    }

    public async Task<bool> CloseAsync(Guid id, decimal exitPrice, DateTime? closedAtUtc = null, CancellationToken cancellationToken = default)
    {
        var trade = await _db.Trades.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (trade is null || trade.Status == TradeStatus.Closed)
            return false;

        trade.Close(exitPrice, closedAtUtc ?? _time.GetUtcNow().UtcDateTime);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var trade = await _db.Trades.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (trade is null)
            return false;

        _db.Trades.Remove(trade);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ImportResultDto> ImportCsvAsync(string csv, CancellationToken cancellationToken = default)
    {
        var errors = new List<ImportErrorDto>();
        var imported = 0;

        if (string.IsNullOrWhiteSpace(csv))
            return new ImportResultDto(0, 0, errors);

        // Delimitador: ';' si aparece (Excel español, permite decimales con ','), si no ','.
        var delimiter = csv.Contains(';') ? ';' : ',';
        var lines = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var lineNo = i + 1;
            var line = lines[i].Trim();
            if (line.Length == 0) continue;

            var fields = line.Split(delimiter);
            var symbol = fields[0].Trim();

            // Cabecera opcional.
            if (symbol.Equals("symbol", StringComparison.OrdinalIgnoreCase)
                || symbol.Equals("simbolo", StringComparison.OrdinalIgnoreCase)
                || symbol.Equals("símbolo", StringComparison.OrdinalIgnoreCase))
                continue;

            if (fields.Length < 3)
            {
                errors.Add(new ImportErrorDto(lineNo, "Se esperan al menos 3 columnas: symbol, entry, qty[, date]."));
                continue;
            }
            if (string.IsNullOrWhiteSpace(symbol))
            {
                errors.Add(new ImportErrorDto(lineNo, "Símbolo vacío."));
                continue;
            }
            if (!TryParseDecimal(fields[1], delimiter, out var entry) || entry <= 0)
            {
                errors.Add(new ImportErrorDto(lineNo, $"Precio de entrada inválido: '{fields[1].Trim()}'."));
                continue;
            }
            if (!TryParseDecimal(fields[2], delimiter, out var qty) || qty <= 0)
            {
                errors.Add(new ImportErrorDto(lineNo, $"Cantidad inválida: '{fields[2].Trim()}'."));
                continue;
            }

            DateTime? openedAt = null;
            if (fields.Length >= 4 && !string.IsNullOrWhiteSpace(fields[3]))
            {
                if (!TryParseDate(fields[3].Trim(), out var date))
                {
                    errors.Add(new ImportErrorDto(lineNo, $"Fecha inválida: '{fields[3].Trim()}' (usa yyyy-MM-dd o dd/MM/yyyy)."));
                    continue;
                }
                openedAt = date;
            }

            try
            {
                await OpenAsync(symbol, entry, qty, openedAt, cancellationToken);
                imported++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errors.Add(new ImportErrorDto(lineNo, ex.Message));
            }
        }

        return new ImportResultDto(imported, errors.Count, errors);
    }

    private static bool TryParseDecimal(string raw, char delimiter, out decimal value)
    {
        var s = raw.Trim();
        // Con delimitador ';' admitimos coma decimal (Excel ES); la normalizamos a punto.
        if (delimiter == ';') s = s.Replace(',', '.');
        return decimal.TryParse(
            s,
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static bool TryParseDate(string raw, out DateTime value)
    {
        string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy", "dd-MM-yyyy", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss" };
        if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value)
            || DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value))
        {
            value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return true;
        }
        return false;
    }
}
