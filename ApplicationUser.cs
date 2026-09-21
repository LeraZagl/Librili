using Microsoft.AspNetCore.Identity;

namespace MyLibrary.Models
{
    // Расширяем стандартного пользователя Identity — если у вас уже есть
    // свой ApplicationUser (созданный мастером VS), просто добавьте в него
    // свойство Books, как показано ниже.
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
