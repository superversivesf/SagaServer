﻿using System;
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
using SagaDb.Database;

namespace SagaUtil.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;
        private UserCommands _userCommands;
        private BookCommands _bookCommands;

        public UserController(IConfiguration config)
        {  
            _config = config;
            _userCommands = new UserCommands(SystemVariables.Instance.UserDb);
            _bookCommands = new BookCommands(SystemVariables.Instance.BookDb);
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

        [HttpPost("SetProgress", Name = "SetProgress")]
        public IActionResult Progress([FromBody] SetProgressDto setProgress)
        {
            var _user = this._userCommands.GetUser(setProgress.UserName);
            var _book = this._bookCommands.GetBook(setProgress.BookId);
            var _file = this._bookCommands.GetAudioFile(setProgress.FileId);
            var _progress = this._userCommands.GetProgress(setProgress.UserName, setProgress.BookId);
                

            if (_user!= null && _book != null && _file != null) // User, book or file is missing
            {
                if (setProgress.Offset <= _file.Duration) // Not past the end of the file
                {
                    if (_progress == null) // No progress on this book yet
                    {
                        _progress = new BookProgress();
                        _progress.UserId = _user.UserId;
                        _progress.Location = setProgress.Offset;
                        _progress.BookId = _book.BookId;
                        _progress.AudioFileId = _file.AudioFileId;
                        this._userCommands.InsertProgress(_progress);
                    }
                    else
                    {
                        _progress.AudioFileId = _file.AudioFileId;
                        _progress.Location = setProgress.Offset;
                        this._userCommands.UpdateProgress(_progress);
                    }

                    return Ok();
                }
            }
            return NotFound(new
            {
                user = _user,
                book = _book,
                fileId = _file,
                offset = setProgress.Offset
            });
        }

        [HttpPost("GetProgress", Name = "GetProgress")]
        public IActionResult Progress([FromBody] GetProgressDto getProgress)
        {
            var _user = this._userCommands.GetUser(getProgress.UserName);

            List<BookProgress> _bookProgresses = null;

            if (getProgress.BookId == null)
            {
                _bookProgresses = this._userCommands.GetProgressByUser(_user.UserId);
            }
            else
            {
                _bookProgresses = this._userCommands.GetProgressByUserAndBookID(_user.UserId, getProgress.BookId);
            }

            return Ok(new
            {
                bookProgress = _bookProgresses
            }) ;
        }

        [HttpPost("SetRead", Name = "SetRead")]
        public IActionResult GetRead([FromBody] SetReadDto setRead)
        {
            IActionResult progressResult = Unauthorized();

            // If a book is specified then progress for that book
            // else progress for all books

            return progressResult;
        }

        [HttpPost("GetRead", Name = "GetRead")]
        public IActionResult GetRead([FromBody] GetReadDto getRead)
        {
            IActionResult progressResult = Unauthorized();

            // If a book is specified then progress for that book
            // else progress for all books

            return progressResult;
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

            var _salt = _user.PasswordSalt;
            var _pass = _user.Password;
            var _userpass = UserHelper.GenerateSaltedHash(loginCredentials.Password, _salt);

            if (!UserHelper.ComparePasswords(_pass, _userpass))
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
                expires: DateTime.Now.AddHours(3), //AddSeconds(30),// AddMinutes(5),
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
