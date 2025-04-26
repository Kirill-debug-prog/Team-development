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
                // Handle database-specific exceptions
                throw new Exception("Failed to create user in database", ex);
            }
            catch (Exception ex)
            {
                // Handle any other unexpected exceptions
                throw new Exception("An error occurred while creating the user", ex);
            }
        }

        public async Task<User?> UpdateUserAsync(User userUpdateData) // Возвращаем User? для ясности (или использовать Result pattern)
        {
            // 1. Базовая валидация входных данных
            if (userUpdateData == null)
            {
                _logger.LogWarning("UpdateUserAsync called with null user data.");
                throw new ArgumentNullException(nameof(userUpdateData));
            }

            try
            {
                // 2. Найти существующего пользователя по ID в базе данных
                var existingUser = await _context.Users.FindAsync(userUpdateData.Id);

                // 3. Проверить, найден ли пользователь
                if (existingUser == null)
                {
                    _logger.LogWarning("Attempted to update non-existent user with ID {UserId}", userUpdateData.Id);
                    // Можно вернуть null, чтобы контроллер вернул NotFound, или бросить KeyNotFoundException
                    return null;
                    // throw new KeyNotFoundException($"User with ID {userUpdateData.Id} not found.");
                }

                // 4. Применить изменения к найденной сущности (Контролируемое обновление)
                // Обновляем только те поля, которые разрешено изменять через этот метод.
                // Явно НЕ обновляем Id, Password, и, возможно, Login здесь.

                existingUser.FirstName = userUpdateData.FirstName;
                existingUser.LastName = userUpdateData.LastName;
                existingUser.MiddleName = userUpdateData.MiddleName;

                // **ВАЖНО:**
                // - Обновление Login: Если вы хотите разрешить обновление логина,
                //   вам НУЖНО добавить проверку на уникальность нового логина
                //   *перед* сохранением изменений. Например:
                //   if (existingUser.Login != userUpdateData.Login) {
                //       bool loginExists = await _context.Users.AnyAsync(u => u.Login == userUpdateData.Login && u.Id != existingUser.Id);
                //       if (loginExists) {
                //           _logger.LogWarning("Attempted to update user {UserId} with already existing login {Login}", existingUser.Id, userUpdateData.Login);
                //           throw new InvalidOperationException($"Login '{userUpdateData.Login}' is already taken."); // Или вернуть ошибку валидации
                //       }
                //       existingUser.Login = userUpdateData.Login;
                //   }

                // - Обновление Пароля: НИКОГДА не присваивайте пароль напрямую.
                //   Создайте отдельный метод `ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)`
                //   который будет проверять старый пароль и хешировать новый перед сохранением.

                // Альтернатива (если DTO точно соответствует обновляемым полям):
                // _context.Entry(existingUser).CurrentValues.SetValues(userUpdateData);
                // Этот метод скопирует все совпадающие по имени свойства. Используйте с осторожностью,
                // чтобы не перезаписать Id, Password или другие не предназначенные для обновления поля.
                // Обычно безопаснее присваивать вручную.

                // 5. Сохранить изменения в базе данных
                // EF Core автоматически отслеживает изменения в existingUser и сгенерирует нужный SQL UPDATE.
                await _context.SaveChangesAsync();
                _logger.LogInformation("User with ID {UserId} updated successfully.", existingUser.Id);

                // 6. Вернуть обновленную сущность
                return existingUser;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Эта ошибка возникает, если запись была изменена другим процессом
                // после того, как мы ее загрузили (existingUser).
                _logger.LogError(ex, "Concurrency conflict occurred while updating user with ID {UserId}.", userUpdateData.Id);
                // Здесь можно реализовать стратегию разрешения конфликтов или просто сообщить об ошибке.
                throw new Exception("Failed to update user due to a concurrency conflict. Please try again.", ex);
            }
            catch (DbUpdateException ex) // Ловим ошибки уровня БД (например, нарушение ограничений)
            {
                _logger.LogError(ex, "Database error occurred while updating user with ID {UserId}.", userUpdateData.Id);
                // Можно проверить InnerException на специфические ошибки БД (например, unique constraint)
                throw new Exception("An error occurred while saving user changes to the database.", ex);
            }
            // Не ловим здесь общие Exception, чтобы не скрывать неожиданные ошибки. Пусть они всплывают выше.
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

        public async Task<User?> GetUserById(Guid id) // Изменили int id на Guid id и возвращаемый тип на User?
        {
            _logger.LogInformation("Attempting to retrieve user by ID {UserId}", id);
            try
            {
                // FindAsync корректно работает с Guid ключами
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", id);
                    // Возвращаем null, если не найден. Контроллер должен обработать это как NotFound.
                    return null;
                    // Или можно бросать исключение, если требуется другое поведение:
                    // throw new KeyNotFoundException($"User with ID {id} not found");
                }

                _logger.LogInformation("Successfully retrieved user with ID {UserId}", id);
                return user;
            }
            // Не ловим KeyNotFoundException здесь, если возвращаем null выше
            catch (Exception ex) // Ловим только общие/неожиданные ошибки
            {
                // В сообщении об ошибке используем строковую интерполяцию или параметры логирования
                _logger.LogError(ex, "Error retrieving user with ID {UserId}", id);
                // Перебрасываем как общее исключение, чтобы не терять stack trace и тип ошибки
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
            // Используем существующий метод GetUserById, так как он делает то же самое
            // и уже содержит логирование и обработку ошибок.
            // Убедимся, что GetUserById возвращает User? (nullable)
            return await GetUserById(userId);
        }

        public async Task<User?> UpdateUserProfileAsync(Guid userId, UpdateUserProfileDTO profileData)
        {
            _logger.LogInformation("Attempting to update profile for user ID {UserId}", userId);

            if (profileData == null)
            {
                _logger.LogWarning("UpdateUserProfileAsync called with null profile data for user ID {UserId}.", userId);
                throw new ArgumentNullException(nameof(profileData));
            }

            try
            {
                var existingUser = await _context.Users.FindAsync(userId);

                if (existingUser == null)
                {
                    _logger.LogWarning("User profile not found for update. User ID {UserId}", userId);
                    return null; // Пользователь не найден
                }

                // Обновляем только разрешенные поля из DTO
                existingUser.FirstName = profileData.FirstName;
                existingUser.LastName = profileData.LastName;
                existingUser.MiddleName = profileData.MiddleName; // Это безопасно, так как DTO его содержит

                // НЕ обновляем Login, Password, Id

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated profile for user ID {UserId}", userId);
                return existingUser;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict occurred while updating profile for user ID {UserId}.", userId);
                throw new Exception("Failed to update profile due to a concurrency conflict. Please try again.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while updating profile for user ID {UserId}.", userId);
                throw new Exception("An error occurred while saving profile changes to the database.", ex);
            }
            // Общие Exception пусть обрабатываются выше
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
