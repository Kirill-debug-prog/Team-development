using ConsultantPlatform.Models.Entity;
using Microsoft.AspNetCore.Identity; // <-- Добавить using для PasswordHasher
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ConsultantPlatform.Models.DTO; // <-- Добавить using для DTO

namespace ConsultantPlatform.Service
{
    public class UserService
    {
        private readonly MentiContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly PasswordHasher<User> _passwordHasher;

        // Изменить конструктор для приема логгера
        public UserService(MentiContext context, ILogger<UserService> logger, PasswordHasher<User> passwordHasher)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
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
                throw new Exception("Failed to create user in database", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while creating the user", ex);
            }
        }

        public async Task<User?> UpdateUserAsync(User userUpdateData)
        {
            if (userUpdateData == null)
            {
                _logger.LogWarning("UpdateUserAsync called with null user data.");
                throw new ArgumentNullException(nameof(userUpdateData));
            }

            try
            {
                var existingUser = await _context.Users.FindAsync(userUpdateData.Id);

                if (existingUser == null)
                {
                    _logger.LogWarning("Attempted to update non-existent user with ID {UserId}", userUpdateData.Id);
                    return null;
                }

                existingUser.FirstName = userUpdateData.FirstName;
                existingUser.LastName = userUpdateData.LastName;
                existingUser.MiddleName = userUpdateData.MiddleName;

                await _context.SaveChangesAsync();
                _logger.LogInformation("User with ID {UserId} updated successfully.", existingUser.Id);

                return existingUser;
            }
            catch (DbUpdateConcurrencyException ex)
            {

                _logger.LogError(ex, "Concurrency conflict occurred while updating user with ID {UserId}.", userUpdateData.Id);
                throw new Exception("Failed to update user due to a concurrency conflict. Please try again.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while updating user with ID {UserId}.", userUpdateData.Id);
                throw new Exception("An error occurred while saving user changes to the database.", ex);
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

        public async Task<User?> GetUserById(Guid id)
        {
            _logger.LogInformation("Attempting to retrieve user by ID {UserId}", id);
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", id);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved user with ID {UserId}", id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID {UserId}", id);
                throw new Exception($"An error occurred while retrieving user with ID {id}", ex);
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

        public async Task<User?> GetUserProfileAsync(Guid userId)
        {
            _logger.LogInformation("Attempting to retrieve profile for user ID {UserId}", userId);
            return await GetUserById(userId);
        }

        /// <summary>
        /// Обновляет информацию профиля для указанного пользователя.
        /// </summary>
        /// <param name="userId">ID пользователя для обновления.</param>
        /// <param name="profileData">DTO с обновленными данными профиля.</param>
        /// <returns>Обновленная сущность User или null, если пользователь не найден.</returns>
        /// <exception cref="ApplicationException">Происходит при ошибках базы данных.</exception>
        /// <exception cref="KeyNotFoundException">Происходит, если пользователь не найден (если GetUserById бросает, а не возвращает null).</exception>
        public async Task<User?> UpdateUserProfileAsync(Guid userId, UpdateUserProfileDTO profileData)
        {
            _logger.LogInformation("Попытка обновить профиль для пользователя с ID {UserId}", userId);

            if (profileData == null)
            {
                _logger.LogWarning("Вызов UpdateUserProfileAsync с нулевыми данными профиля для пользователя ID {UserId}.", userId);
                throw new ArgumentNullException(nameof(profileData));
            }

            try
            {
                var existingUser = await _context.Users.FindAsync(userId);

                if (existingUser == null)
                {
                    _logger.LogWarning("Профиль пользователя не найден в базе данных при попытке обновления для ID {UserId}.", userId);
                    return null;
                }

                existingUser.FirstName = profileData.FirstName;
                existingUser.LastName = profileData.LastName;
                existingUser.MiddleName = profileData.MiddleName;
                existingUser.PhoneNumber = profileData.PhoneNumber;
                existingUser.Email = profileData.Email;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Профиль для пользователя {UserId} успешно обновлен.", userId);
                return existingUser;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Конфликт параллелизма при обновлении профиля для пользователя с ID {UserId}.", userId);
                throw new ApplicationException("Не удалось обновить профиль из-за конфликта параллелизма. Попробуйте снова.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка базы данных при обновлении профиля для пользователя с ID {UserId}.", userId);
                throw new ApplicationException("Ошибка сохранения изменений профиля в базе данных.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при обновлении профиля для пользователя с ID {UserId}", userId);
                throw new Exception("Произошла внутренняя ошибка сервера при обновлении профиля.", ex);
            }
        }

        /// <summary>
        /// Changes the password for a specified user after verifying the current password.
        /// </summary>
        /// <param name="userId">The ID of the user whose password to change.</param>
        /// <param name="oldPassword">The user's current password.</param>
        /// <param name="newPassword">The desired new password.</param>
        /// <returns>True if the password was changed successfully, false if the old password was incorrect.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the user with the specified ID is not found.</exception>
        /// <exception cref="Exception">Thrown for database or other unexpected errors.</exception>
        public async Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
        {
            _logger.LogInformation("Attempting to change password for user ID {UserId}", userId);

            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User not found when attempting to change password. User ID {UserId}", userId);
                    // Бросаем исключение, так как это неожиданная ситуация для запроса смены пароля
                    // аутентифицированным пользователем.
                    throw new KeyNotFoundException($"User with ID {userId} not found.");
                }

                // 1. Проверить старый пароль
                var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, oldPassword);

                if (verificationResult == PasswordVerificationResult.Failed)
                {
                    _logger.LogWarning("Incorrect current password provided for user ID {UserId}", userId);
                    return false; // Старый пароль неверный
                }

                // Если результат SuccessRehashNeeded, пароль верный, но хеш нужно обновить.
                // Мы все равно будем генерировать новый хеш, так что это покрывается.

                // 2. Сгенерировать хеш для нового пароля
                var newPasswordHash = _passwordHasher.HashPassword(user, newPassword);

                // 3. Обновить пароль пользователя
                user.Password = newPasswordHash;

                // 4. Сохранить изменения
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password successfully changed for user ID {UserId}", userId);
                return true; // Пароль успешно изменен
            }
            // KeyNotFoundException будет проброшена выше
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while changing password for user ID {UserId}.", userId);
                throw new Exception("An error occurred while saving the new password.", ex);
            }
            catch (Exception ex) // Ловим другие неожиданные ошибки
            {
                _logger.LogError(ex, "Unexpected error occurred while changing password for user ID {UserId}", userId);
                // Не скрываем KeyNotFoundException
                if (ex is KeyNotFoundException) throw;
                throw new Exception("An unexpected error occurred while changing the password.", ex);
            }
        }


    }
}
