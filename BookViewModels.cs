namespace MyLibrary.Models
{
    // Параметры фильтрации, приходят из query string формы на странице "Все книги"
    public class BookFilter
    {
        public ReadStatus? Status { get; set; }
        public string? Genre { get; set; }
        public double? MinRating { get; set; }
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public string? Search { get; set; }
        public string Sort { get; set; } = "added_desc";
        public string ViewMode { get; set; } = "cards"; // cards | list
    }

    public class BooksIndexViewModel
    {
        public List<Book> Books { get; set; } = new();
        public BookFilter Filter { get; set; } = new();
        public List<string> AvailableGenres { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class StatisticsViewModel
    {
        public int TotalBooks { get; set; }
        public int ReadCount { get; set; }
        public int ReadingCount { get; set; }
        public int WantToReadCount { get; set; }
        public int TotalPagesRead { get; set; }
        public double AverageRating { get; set; }

        public Dictionary<string, int> ByGenre { get; set; } = new();
        public Dictionary<string, int> ByAuthor { get; set; } = new();
        public Dictionary<string, int> FinishedByMonth { get; set; } = new(); // "2026-01" -> count

        public Book? BookOfTheMonth { get; set; } // случайная книга из "хочу прочитать"
    }
}
