using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BarangayBudgetSystem.App.Data;
using BarangayBudgetSystem.App.Models;

namespace BarangayBudgetSystem.App.Services
{
    public interface IAuthenticationService
    {
        User? CurrentUser { get; }
        bool IsAuthenticated { get; }
        Task<(bool Success, string Message)> LoginAsync(string username, string password);
        void Logout();
        Task<(bool Success, string Message)> ChangePasswordAsync(string currentPassword, string newPassword);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly AppDbContext _context;
        private User? _currentUser;

        public AuthenticationService(AppDbContext context)
        {
            _context = context;
        }

        public User? CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;

        public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "Username and password are required.");
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() && u.IsActive);

                if (user == null)
                {
                    return (false, "Invalid username or password.");
                }

                if (!VerifyPassword(password, user.PasswordHash))
                {
                    return (false, "Invalid username or password.");
                }

                // Update last login
                user.LastLoginAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _currentUser = user;
                return (true, $"Welcome, {user.FullName}!");
            }
            catch (Exception ex)
            {
                return (false, $"Login failed: {ex.Message}");
            }
        }

        public void Logout()
        {
            _currentUser = null;
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            if (_currentUser == null)
            {
                return (false, "No user is logged in.");
            }

            if (!VerifyPassword(currentPassword, _currentUser.PasswordHash))
            {
                return (false, "Current password is incorrect.");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                return (false, "New password must be at least 6 characters.");
            }

            try
            {
                _currentUser.PasswordHash = HashPassword(newPassword);
                _currentUser.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return (true, "Password changed successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to change password: {ex.Message}");
            }
        }

        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password + "BarangayBudgetSalt2024");
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public bool VerifyPassword(string password, string hash)
        {
            var computedHash = HashPassword(password);
            return computedHash == hash;
        }
    }
}
