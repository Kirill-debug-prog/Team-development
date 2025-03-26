using ConsultantPlatform.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace ConsultantPlatform.Service
{
    public class UserService
    {
        private readonly MentiContext _context;

        public UserService(MentiContext context)
        {
            _context = context;
        }

        public async Task<User> CreateUser(User user)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return user;
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific exceptions
                throw new Exception("Failed to create user in database", ex);
            }
            catch (Exception ex)
            {
                // Handle any other unexpected exceptions
                throw new Exception("An error occurred while creating the user", ex);
            }
        }

        public async Task<User> UpdateUser(User user)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return user;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Handle concurrency conflicts
                throw new Exception("User was modified or deleted by another process", ex);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific exceptions
                throw new Exception("Failed to update user in database", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while updating the user", ex);
            }
        }

        public async Task<User> DeleteUser(User user)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return user;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("Failed to delete user from database", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while deleting the user", ex);
            }
        }

        public async Task<User> GetUserById(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    throw new KeyNotFoundException($"User with ID {id} not found");

                return user;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving user with ID {id}", ex);
            }
        }

        public async Task<User> GetUserByLogin(string login)
        {
            try
            {
                if (string.IsNullOrEmpty(login))
                    throw new ArgumentNullException(nameof(login));

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Login == login);

                if (user == null)
                    return null;

                return user;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving user with name {login}", ex);
            }
        }
    }
}
