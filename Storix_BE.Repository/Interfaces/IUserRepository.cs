using Storix_BE.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Storix_BE.Repository.Interfaces
{
    public interface IUserRepository
    {
        Task<User> Login(string email, string password);
        Task<User> LoginWithGoogleAsync(ClaimsPrincipal? claimsPrincipal);
        Task<User> RegisterCompanyAdministratorAsync(
            string companyName,
            string? businessCode,
            string? address,
            string? contactEmail,
            string? contactPhone,
            string adminFullName,
            string adminEmail,
            string? adminPhone,
            string password);
    }
}
