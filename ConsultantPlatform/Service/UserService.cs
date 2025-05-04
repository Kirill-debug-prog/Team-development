using ConsultantPlatform.Models.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ConsultantPlatform.Models.DTO;

namespace ConsultantPlatform.Service
{
    public class UserService
    {
        private readonly MentiContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly PasswordHasher<User> _passwordHasher;

        public UserService(MentiContext context, ILogger<UserService> logger, PasswordHasher<User> passwordHasher)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        /// <summary>
        /// Создает нового пользователя в базе данных.
        /// </summary>
        /// <param name="user">Сущность пользователя для создания (пароль должен быть уже хеширован).</param>
        /// <returns>Созданная сущность пользователя.</returns>
        /// <exception cref="ArgumentNullException">Если входная сущность пользователя равна null.</exception>
        /// <exception cref="Exception">Происходит при ошибках базы данных или других ошибках сохранения.</exception>
        public async Task<User> CreateUser(User user)
        {
            _logger.LogInformation("Попытка создать пользователя с логином: {Login}", user?.Login);
            try
            {
                if (user == null)
                {
                    _logger.LogWarning("Вызов CreateUser с нулевыми данными пользователя.");
                    throw new ArgumentNullException(nameof(user));
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Пользователь с ID {UserId} и логином {Login} успешно создан.", user.Id, user.Login);
                return user;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка базы данных при создании пользователя с логином {Login}", user.Login);
                // Можно проверить ex.InnerException на специфические ошибки (например, Unique Constraint Violation)
                throw new Exception("Не удалось создать пользователя в базе данных.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при создании пользователя с логином {Login}", user.Login);
                throw new Exception("Произошла ошибка при создании пользователя.", ex);
            }
        }

        /// <summary>
        /// Удаляет пользователя из базы данных.
        /// </summary>
        /// <param name="user">Сущность пользователя для удаления.</param>
        /// <returns>Удаленная сущность пользователя.</returns>
        /// <exception cref="ArgumentNullException">Если входная сущность пользователя равна null.</exception>
        /// <exception cref="Exception">Происходит при ошибках базы данных или других ошибках удаления.</exception>
        public async Task<User> DeleteUser(User user)
        {
            _logger.LogInformation("Попытка удалить пользователя с ID {UserId}", user?.Id);
            try
            {
                if (user == null)
                {
                    _logger.LogWarning("Вызов DeleteUser с нулевыми данными пользователя.");
                    throw new ArgumentNullException(nameof(user));
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Пользователь с ID {UserId} успешно удален.", user.Id);
                return user;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка базы данных при удалении пользователя с ID {UserId}", user.Id);
                throw new Exception("Не удалось удалить пользователя из базы данных.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при удалении пользователя с ID {UserId}", user.Id);
                throw new Exception("Произошла ошибка при удалении пользователя.", ex);
            }
        }

        /// <summary>
        /// Получает пользователя по уникальному идентификатору.
        /// </summary>
        /// <param name="id">ID пользователя.</param>
        /// <returns>Сущность пользователя или null, если пользователь не найден.</returns>
        /// <exception cref="Exception">Происходит при ошибках базы данных или других непредвиденных ошибках.</exception>
        public async Task<User?> GetUserById(Guid id)
        {
            _logger.LogInformation("Попытка получить пользователя по ID {UserId}", id);
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    _logger.LogWarning("Пользователь с ID {UserId} не найден.", id);
                    return null;
                }

                _logger.LogInformation("Пользователь с ID {UserId} успешно получен.", id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении пользователя по ID {UserId}", id);
                throw new Exception($"Произошла ошибка при получении пользователя с ID {id}", ex);
            }
        }

        /// <summary>
        /// Получает пользователя по логину.
        /// </summary>
        /// <param name="login">Логин пользователя.</param>
        /// <returns>Сущность пользователя или null, если пользователь не найден.</returns>
        /// <exception cref="ArgumentNullException">Если логин равен null или пуст.</exception>
        /// <exception cref="Exception">Происходит при ошибках базы данных или других непредвиденных ошибках.</exception>
        public async Task<User?> GetUserByLogin(string login)
        {
            _logger.LogInformation("Попытка получить пользователя по логину: {Login}", login);
            try
            {
                if (string.IsNullOrEmpty(login))
                {
                    _logger.LogWarning("Вызов GetUserByLogin с пустым логином.");
                    throw new ArgumentNullException(nameof(login));
                }


                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Login == login);

                if (user == null)
                {
                    _logger.LogWarning("Пользователь с логином {Login} не найден.", login);
                    return null;
                }

                _logger.LogInformation("Пользователь с логином {Login} успешно получен.", login);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении пользователя по логину {Login}", login);
                throw new Exception($"Произошла ошибка при получении пользователя с логином {login}", ex);
            }
        }

        /// <summary>
        /// Получает информацию профиля для указанного пользователя.
        /// </summary>
        /// <param name="userId">ID пользователя.</param>
        /// <returns>Сущность User или null, если пользователь не найден.</returns>
        public async Task<User?> GetUserProfileAsync(Guid userId)
        {
            _logger.LogInformation("Попытка получить профиль для пользователя с ID {UserId}", userId);
            return await GetUserById(userId);
        }

        /// <summary>
        /// Обновляет информацию профиля для указанного пользователя.
        /// </summary>
        /// <param name="userId">ID пользователя для обновления.</param>
        /// <param name="profileData">DTO с обновленными данными профиля.</param>
        /// <returns>Обновленная сущность User или null, если пользователь не найден.</returns>
        /// <exception cref="ArgumentNullException">Если profileData равен null.</exception>
        /// <exception cref="ApplicationException">Происходит при конфликте параллелизма или ошибках базы данных.</exception>
        /// <exception cref="Exception">Происходит при других непредвиденных ошибках.</exception>
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

                // Обновляем поля из DTO, включая новые
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
                throw new Exception("Произошла непредвиденная ошибка при обновлении профиля.", ex);
            }
        }


        /// <summary>
        /// Изменяет пароль для указанного пользователя после проверки текущего пароля.
        /// </summary>
        /// <param name="userId">ID пользователя, чей пароль нужно изменить.</param>
        /// <param name="oldPassword">Текущий пароль пользователя.</param>
        /// <param name="newPassword">Новый пароль.</param>
        /// <returns>True, если пароль успешно изменен, false, если старый пароль неверный.</returns>
        /// <exception cref="KeyNotFoundException">Происходит, если пользователь с указанным ID не найден.</exception>
        /// <exception cref="Exception">Происходит при ошибках базы данных или других непредвиденных ошибках.</exception>
        public async Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
        {
            _logger.LogInformation("Попытка сменить пароль для пользователя с ID {UserId}", userId);

            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("Пользователь не найден при попытке смены пароля. User ID {UserId}", userId);
                    throw new KeyNotFoundException($"Пользователь с ID {userId} не найден.");
                }

                var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, oldPassword);

                if (verificationResult == PasswordVerificationResult.Failed)
                {
                    _logger.LogWarning("Указан неверный текущий пароль при смене пароля для пользователя с ID {UserId}", userId);
                    return false;
                }

                var newPasswordHash = _passwordHasher.HashPassword(user, newPassword);
                user.Password = newPasswordHash;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Пароль успешно изменен для пользователя с ID {UserId}.", userId);
                return true;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка базы данных при смене пароля для пользователя с ID {UserId}.", userId);
                throw new Exception("Произошла ошибка при сохранении нового пароля.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при смене пароля для пользователя с ID {UserId}", userId);
                throw new Exception("Произошла непредвиденная ошибка при смене пароля.", ex);
            }
        }


    }
}
