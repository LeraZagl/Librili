using System.ComponentModel.DataAnnotations;

namespace MyLibrary.Models
{
    public enum ReadStatus
    {
        [Display(Name = "Хочу прочитать")]
        WantToRead = 0,

        [Display(Name = "Читаю")]
        Reading = 1,

        [Display(Name = "Прочитано")]
        Read = 2
    }

    public class Book
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Укажите название книги")]
        [StringLength(200)]
        [Display(Name = "Название")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите автора")]
        [StringLength(150)]
        [Display(Name = "Автор")]
        public string Author { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Жанр")]
        public string? Genre { get; set; }

        [Range(0, 3000)]
        [Display(Name = "Год издания")]
        public int? Year { get; set; }

        [Range(1, 20000)]
        [Display(Name = "Страниц всего")]
        public int? Pages { get; set; }

        [Range(0, 20000)]
        [Display(Name = "Текущая страница")]
        public int? CurrentPage { get; set; }

        [Range(0, 5)]
        [Display(Name = "Рейтинг (0-5)")]
        public double? Rating { get; set; }

        [Display(Name = "Статус")]
        public ReadStatus Status { get; set; } = ReadStatus.WantToRead;

        [StringLength(500)]
        [Display(Name = "Ссылка на обложку")]
        public string? CoverUrl { get; set; }

        [StringLength(2000)]
        [Display(Name = "Заметки / рецензия")]
        public string? Notes { get; set; }

        [Display(Name = "Избранное")]
        public bool IsFavorite { get; set; }

        [Display(Name = "Дата завершения чтения")]
        [DataType(DataType.Date)]
        public DateTime? DateFinished { get; set; }

        [Display(Name = "Добавлено")]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Владелец книги — каждый пользователь видит только свои книги
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        // Вспомогательное свойство для прогресс-бара чтения
        public int ProgressPercent =>
            (Pages is > 0 && CurrentPage.HasValue)
                ? (int)Math.Clamp(Math.Round(CurrentPage.Value * 100.0 / Pages.Value), 0, 100)
                : 0;
    }
}
