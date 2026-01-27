using Microsoft.EntityFrameworkCore;
using Storix_BE.Domain.Context;
using Storix_BE.Domain.Models;
using Storix_BE.Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storix_BE.Repository.Implementation
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        private readonly StorixDbContext _context;
        public UserRepository(StorixDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<User> Login(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return user;
            }
            return null;
        }
    }
}
