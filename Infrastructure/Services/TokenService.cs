
using Infrastructure.Data.Context;
using Infrastructure.Data.Entities;
using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Infrastructure.Services;

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(TokenRequest tokenRequest, string? refreshToken);
    Task<RefreshTokenResult> GenerateRefreshTokenAsync(string userId, CancellationToken cancellationToken);
    Task<RefreshTokenResult> GetRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task<bool> SaveRefreshTokenAsync(string refreshToken, string userId, CancellationToken cancellationToken);
}

public class TokenService(IDbContextFactory<DataContext> dbContextFactory) : ITokenService
{
    private readonly IDbContextFactory<DataContext> _dbContextFactory = dbContextFactory;

    public async Task<RefreshTokenResult> GetRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        RefreshTokenResult refreshTokenResult = null!;

        try
        {
            await using var context = _dbContextFactory.CreateDbContext();

          

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
       
        }

        catch (Exception ex)
        {
            refreshTokenResult = new RefreshTokenResult()
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Error = ex.Message,
            };

        }
        return refreshTokenResult;
    }

    public async Task<bool> SaveRefreshTokenAsync(string refreshToken, string userId, CancellationToken cancellationToken)
    {
        try
        {
            var tokenLifeTime = double.TryParse(Environment.GetEnvironmentVariable("TOKEN_REFRESHTOKEN_LIFETIME"), out double refreshTokenLifeTime) ? refreshTokenLifeTime : 7;
            await using var context = _dbContextFactory.CreateDbContext();

            var refreshTokenEntity = new RefreshTokenEntity()
            {
                RefreshToken = refreshToken,
                UserId = userId,
                ExpiryDate = DateTime.Now.AddDays(tokenLifeTime),
            };

            context.RefreshTokens.Add(refreshTokenEntity);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }

        catch
        {
            return false;
        }

    }

    public async Task<RefreshTokenResult> GenerateRefreshTokenAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new RefreshTokenResult { StatusCode = (int)HttpStatusCode.BadRequest, Error = "Invalid body request no userId was found" };
            }
            var claims = new[]
            {
                new Claim (ClaimTypes.NameIdentifier, userId)
            };

            var token = GenerateJwtToken(new ClaimsIdentity(claims), DateTime.Now.AddMinutes(5));

            if (token == null)
            {
                return new RefreshTokenResult { StatusCode = (int)HttpStatusCode.InternalServerError, Error = "Unexpected error occured when token was generated" };
            }

            var cookieOptions = GenerateCookie(DateTimeOffset.Now.AddDays(7));

            if (cookieOptions == null)
            {
                return new RefreshTokenResult { StatusCode = (int)HttpStatusCode.InternalServerError, Error = "Unexpected error occured when cookie was generated" };
            }

            var result = await SaveRefreshTokenAsync(token, userId, cancellationToken);
            if (!result)
            {
                return new RefreshTokenResult { StatusCode = (int)HttpStatusCode.InternalServerError, Error = "Unexpected error occured while saving refresh token" };
            }

            return new RefreshTokenResult
            {
                StatusCode = (int)HttpStatusCode.OK,
                Token = token,
                CookieOptions = cookieOptions
            };
        }
        catch (Exception ex)
        {
            return new RefreshTokenResult { StatusCode = (int)HttpStatusCode.InternalServerError, Error = ex.Message };
        }

    }

    public static string GenerateJwtToken(ClaimsIdentity claims, DateTime expires)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claims,
            Expires = expires,
            Issuer = Environment.GetEnvironmentVariable("TOKEN_ISSUER"),
            Audience = Environment.GetEnvironmentVariable("TOKEN_AUDIENCE"),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("TOKEN_SECRETKEY")!)), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public AccessTokenResult GenerateAccessToken(TokenRequest tokenRequest, string? refreshToken)
    {
        try
        {
            if (string.IsNullOrEmpty(tokenRequest.UserId) || string.IsNullOrEmpty(tokenRequest.Email))
            {
                return new AccessTokenResult { StatusCode = (int)HttpStatusCode.BadRequest, Error = "Invalid request body. UserId and Email address must be provided" };
            }
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, tokenRequest.UserId),
                new Claim(ClaimTypes.Name, tokenRequest.Email),
                new Claim(ClaimTypes.Email, tokenRequest.Email)
            };
            if (!string.IsNullOrEmpty(refreshToken))
            {
                claims = [.. claims, new Claim("refreshToken", refreshToken)];

            }
            var token = GenerateJwtToken(new ClaimsIdentity(claims), DateTime.Now.AddMinutes(5));
            if (token == null)
            {
                return new AccessTokenResult { StatusCode = (int)HttpStatusCode.InternalServerError, Error = "Something went wrong while creating token" };
            }

            return new AccessTokenResult { StatusCode = (int)HttpStatusCode.OK, Token = token };
        }

        catch (Exception ex)
        {
            return new AccessTokenResult { StatusCode = (int)HttpStatusCode.InternalServerError, Error = ex.Message };
        }
    }

    public static CookieOptions GenerateCookie(DateTimeOffset expiryDate)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = expiryDate,
        };

        return cookieOptions;
    }
}
