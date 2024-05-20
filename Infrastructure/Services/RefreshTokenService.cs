
using Infrastructure.Data.Context;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Infrastructure.Services;

public interface IRefreshTokenService
{
    Task<RefreshTokenResult> GetRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task<bool> SaveRefreshTokenAsync(string refreshToken,string userId, CancellationToken cancellationToken);
}
public class RefreshTokenService(IDbContextFactory<DataContext> dbContextFactory) :IRefreshTokenService
{
    private readonly IDbContextFactory<DataContext> _dbContextFactory = dbContextFactory;

    public async Task<RefreshTokenResult> GetRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        RefreshTokenResult refreshTokenResult = null!;

        var refreshTokenEntity = await context.RefreshTokens.FirstOrDefaultAsync(x => x.RefreshToken == refreshToken && x.ExpiryDate > DateTime.Now, cancellationToken);

        if (refreshTokenEntity != null)
        {
            refreshTokenResult = new RefreshTokenResult()
            {
                StatusCode = (int)HttpStatusCode.OK,
                Token = refreshTokenEntity.RefreshToken,
                ExpiryDate = refreshTokenEntity.ExpiryDate,
            };
        }
        else
        {
            refreshTokenResult = new RefreshTokenResult()
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Error = "Refresh Token not found or expired",
            };

        }
        return refreshTokenResult;
    }

    public Task<bool> SaveRefreshTokenAsync(string refreshToken, string userId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
