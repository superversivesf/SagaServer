using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SagaUtil.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SagaDb.Databases;
using SagaDb.Models;

namespace SagaUtil.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;
        private UserCommands _userCommands;

        public UserController(IConfiguration config)
        {  
            _config = config;
            _userCommands = new UserCommands(SystemVariables.Instance.UserDb);
        }

        [HttpPost("Login", Name = "Login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody]UserDto login)
        {
            IActionResult response = Unauthorized();
            User user = AuthenticateUser(login);
            if (user != null)
            {
                var tokenString = GenerateJWTToken(user);
                var refreshString = GenerateRefreshToken(user, login.ClientId);
                response = Ok(new
                {
                    token = tokenString,
                    refresh = refreshString 
                });
            }
            return response;
        }

        [HttpPost("Refresh", Name = "Refresh")]
        [AllowAnonymous]
        public IActionResult Refresh([FromBody]RefreshDto refresh)
        {
            IActionResult response = Unauthorized();
            User user = RefreshUser(refresh);
            if (user != null)
            {
                var tokenString = GenerateJWTToken(user);
                var refreshString = GenerateRefreshToken(user, refresh.ClientID);
                response = Ok(new
                {
                    token = tokenString,
                    refresh = refreshString
                });
            }
            return response;
        }

        private User RefreshUser(RefreshDto refresh)
        {
            var _token = refresh.RefreshTokenValue;
            var _username = refresh.UserName;
            var _clientId = refresh.ClientID;

            var _refreshToken = this._userCommands.GetRefreshToken(_token);

            if (_refreshToken == null)
                return null;

            if (_refreshToken.ExpiryDate < DateTime.Now)
                return null;

            if (_refreshToken.ClientID != _clientId)
                return null;

            if (_refreshToken.UserId != _username)
                return null;
            
            // Refresh token found and matched, so it is used, discard it
            this._userCommands.DeleteRefreshToken(_refreshToken);

            return this._userCommands.GetUser(_username);
        }

        private User AuthenticateUser(UserDto loginCredentials)
        {
            var _user = this._userCommands.GetUserByUsername(loginCredentials.UserName);

            if (_user == null)
                return null;

            var _passwordHash = loginCredentials.Password;

            if (_passwordHash != _user.Password)
                return null;

            return _user;
        }

        private string GenerateJWTToken(User userInfo)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SystemVariables.Instance.JwtSigningKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
                {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.UserName),
                new Claim("fullName", userInfo.FullName.ToString()),
                new Claim("role",userInfo.UserRole),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };
            var token = new JwtSecurityToken(
                issuer: SystemVariables.Instance.JwtAudience,
                audience: SystemVariables.Instance.JwtAudience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(5),
                signingCredentials: credentials
                );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken(User user, string ClientId)
        {
            var _rng = RandomNumberGenerator.Create();

            var _refreshToken = new RefreshToken();

            _refreshToken.RefreshTokenValue = RandomStringGenerator.StringGenerator.GetUniqueString(64);
            _refreshToken.ClientID = ClientId;
            _refreshToken.ExpiryDate = DateTime.Now.AddDays(30);
            _refreshToken.UserId = user.UserName;

            this._userCommands.InsertRefreshToken(_refreshToken);

            return _refreshToken.RefreshTokenValue;
        }
    }
}
