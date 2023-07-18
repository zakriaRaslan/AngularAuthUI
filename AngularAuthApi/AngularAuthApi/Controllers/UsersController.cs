﻿using AngularAuthApi.Data;
using AngularAuthApi.Helpers;
using AngularAuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace AngularAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User model)
        {
            if (model == null) { return BadRequest(error: "User Is Null"); }

            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName == model.UserName);
            if (user == null)
            {
                return NotFound(new
                {
                    message = "User Not Found"
                });
            }
            if (!PasswordHasher.VerifyPassword(model.Password, user.Password))
            {
                return BadRequest(new
                {
                    message = "Incorrect Password"
                });
            }
            user.Token = CreateJwtToken(user);
            return Ok(new
            {
                Token = user.Token,
                Message = "Login Successfuly"
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (user == null)
            {
                return BadRequest(new
                {
                    message = "User Is Null"
                });
            }

            var passwordTest = CheckPassword(user.Password);

            if (!string.IsNullOrEmpty(passwordTest))
            {
                return BadRequest(new { message = passwordTest });
            }

            user.Password = PasswordHasher.HashPassword(user.Password);

            if (await UserNameIsExist(user.UserName))
            {
                return BadRequest(new { message = "UserName Is Alerady Exist" });
            }
            if (await EmailNameIsExist(user.Email))
            {
                return BadRequest(new { message = "Email Is Already Exist" });
            }

            user.Role = "User";
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = "Registered Successfuly"
            });
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _context.Users.ToListAsync();
            return Ok(result);
        }
        private async Task<bool> UserNameIsExist(string username)
        {
            return await _context.Users.AnyAsync(x => x.UserName == username);
        }
        private async Task<bool> EmailNameIsExist(string Email)
        {
            return await _context.Users.AnyAsync(x => x.Email == Email);
        }
        private string CheckPassword(string password)
        {
            StringBuilder sb = new StringBuilder();


            if (password.Length < 8) { sb.Append("Password Should Be At Least 8 Character" + Environment.NewLine); }
            if (!(Regex.IsMatch(password, "[a-z]") && (Regex.IsMatch(password, "[A-Z]") && (Regex.IsMatch(password, "[1-9]")))))
            {
                sb.Append("Password Should Be Include A Small & Capital Char & Numbers " + Environment.NewLine);
            }
            if (Regex.IsMatch(password, "\"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$\""))
            {
                sb.Append("Password Should Contain A Special Char");
            }

            return sb.ToString();
        }

        private string CreateJwtToken(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("veryverySercrit.....");
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Name,$"{user.FirstName} {user.LastName}")
            });

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = credentials,
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            return jwtTokenHandler.WriteToken(token);
        }
    }
}
