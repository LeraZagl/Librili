using Microsoft.AspNetCore.Identity;

namespace MyLibrary.Services
{
    // Переводит стандартные сообщения об ошибках ASP.NET Core Identity на русский.
    public class RussianIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DuplicateEmail(string email) => new()
        {
            Code = nameof(DuplicateEmail),
            Description = $"Пользователь с email «{email}» уже зарегистрирован."
        };

        public override IdentityError DuplicateUserName(string userName) => new()
        {
            Code = nameof(DuplicateUserName),
            Description = $"Пользователь «{userName}» уже существует."
        };

        public override IdentityError InvalidEmail(string? email) => new()
        {
            Code = nameof(InvalidEmail),
            Description = $"Email «{email}» некорректен."
        };

        public override IdentityError PasswordTooShort(int length) => new()
        {
            Code = nameof(PasswordTooShort),
            Description = $"Пароль должен быть не короче {length} символов."
        };

        public override IdentityError PasswordRequiresNonAlphanumeric() => new()
        {
            Code = nameof(PasswordRequiresNonAlphanumeric),
            Description = "Пароль должен содержать хотя бы один специальный символ."
        };

        public override IdentityError PasswordRequiresDigit() => new()
        {
            Code = nameof(PasswordRequiresDigit),
            Description = "Пароль должен содержать хотя бы одну цифру (0-9)."
        };

        public override IdentityError PasswordRequiresLower() => new()
        {
            Code = nameof(PasswordRequiresLower),
            Description = "Пароль должен содержать хотя бы одну строчную букву (a-z)."
        };

        public override IdentityError PasswordRequiresUpper() => new()
        {
            Code = nameof(PasswordRequiresUpper),
            Description = "Пароль должен содержать хотя бы одну заглавную букву (A-Z)."
        };

        public override IdentityError InvalidUserName(string? userName) => new()
        {
            Code = nameof(InvalidUserName),
            Description = $"Имя пользователя «{userName}» недопустимо."
        };

        public override IdentityError DefaultError() => new()
        {
            Code = nameof(DefaultError),
            Description = "Произошла неизвестная ошибка. Попробуйте ещё раз."
        };
    }
}
