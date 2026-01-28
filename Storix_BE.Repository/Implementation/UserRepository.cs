using Microsoft.EntityFrameworkCore;
using Storix_BE.Domain.Context;
using Storix_BE.Domain.Exception;
using Storix_BE.Domain.Models;
using Storix_BE.Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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

        public async Task<User> LoginWithGoogleAsync(ClaimsPrincipal? claimsPrincipal)
        {
            if(claimsPrincipal==null)
            {
                throw new ExternalLoginProviderException("Google","Claims principal is null");
            }
            var email = claimsPrincipal?.FindFirst(ClaimTypes.Email)?.Value;
            if(email == null)
            {
                throw new ExternalLoginProviderException("Google", "Email is null");
            }

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

            if(user == null)
            {
                return null;
                /*var newUser = new User
                {
                    Email = email,
                    FullName = claimsPrincipal?.FindFirst(ClaimTypes.GivenName)?.Value ?? String.Empty,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123")
                };*/
                //Add user sau
            }

            return user;
        }

        public async Task<User> RegisterCompanyAdministratorAsync(
            string companyName,
            string? businessCode,
            string? address,
            string? contactEmail,
            string? contactPhone,
            string adminFullName,
            string adminEmail,
            string? adminPhone,
            string password)
        {
            // Ensure email is unique
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email is already registered.");
            }

            // Find Company Administrator role by name
            var companyAdminRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "Company Administrator");

            if (companyAdminRole == null)
            {
                throw new InvalidOperationException("Role 'Company Administrator' not found. Please seed this role in the database.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var company = new Company
                {
                    Name = companyName,
                    BusinessCode = businessCode,
                    Address = address,
                    ContactEmail = contactEmail,
                    ContactPhone = contactPhone,
                    SubscriptionPlan = null,
                    Status = "Active",
                    // Npgsql + timestamp without time zone yêu cầu DateTimeKind.Unspecified
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                    UpdatedAt = null
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                var user = new User
                {
                    CompanyId = company.Id,
                    FullName = adminFullName,
                    Email = adminEmail,
                    Phone = adminPhone,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    RoleId = companyAdminRole.Id,
                    Status = "Active",
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                    UpdatedAt = null
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return user;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
