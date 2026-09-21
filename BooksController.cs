using System.Globalization;
using System.Security.Claims;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyLibrary.Data;
using MyLibrary.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MyLibrary.Controllers
{
    [Authorize]
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BooksController(ApplicationDbContext db)
        {
            _db = db;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ---------- СПИСОК / ФИЛЬТРАЦИЯ ----------
        public async Task<IActionResult> Index(BookFilter filter)
        {
            var query = _db.Books.Where(b => b.UserId == UserId).AsQueryable();

            if (filter.Status.HasValue)
                query = query.Where(b => b.Status == filter.Status);

            if (!string.IsNullOrWhiteSpace(filter.Genre))
                query = query.Where(b => b.Genre == filter.Genre);

            if (filter.MinRating.HasValue)
                query = query.Where(b => b.Rating >= filter.MinRating);

            if (filter.YearFrom.HasValue)
                query = query.Where(b => b.Year >= filter.YearFrom);

            if (filter.YearTo.HasValue)
                query = query.Where(b => b.Year <= filter.YearTo);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                query = query.Where(b => EF.Functions.Like(b.Title, $"%{s}%")
                                       || EF.Functions.Like(b.Author, $"%{s}%"));
            }

            query = filter.Sort switch
            {
                "title_asc" => query.OrderBy(b => b.Title),
                "title_desc" => query.OrderByDescending(b => b.Title),
                "rating_desc" => query.OrderByDescending(b => b.Rating),
                "year_desc" => query.OrderByDescending(b => b.Year),
                "year_asc" => query.OrderBy(b => b.Year),
                _ => query.OrderByDescending(b => b.DateAdded) // added_desc по умолчанию
            };

            var genres = await _db.Books
                .Where(b => b.UserId == UserId && b.Genre != null && b.Genre != "")
                .Select(b => b.Genre!)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            var vm = new BooksIndexViewModel
            {
                Books = await query.ToListAsync(),
                Filter = filter,
                AvailableGenres = genres
            };
            vm.TotalCount = vm.Books.Count;

            return View(vm);
        }

        // ---------- ДЕТАЛИ ----------
        public async Task<IActionResult> Details(int id)
        {
            var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == id && b.UserId == UserId);
            if (book == null) return NotFound();
            return View(book);
        }

        // ---------- СОЗДАНИЕ ----------
        public IActionResult Create() => View(new Book());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book)
        {
            if (!ModelState.IsValid) return View(book);

            book.UserId = UserId;
            book.DateAdded = DateTime.Now;
            if (book.Status == ReadStatus.Read && book.DateFinished == null)
                book.DateFinished = DateTime.Now;

            _db.Books.Add(book);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Книга добавлена в библиотеку.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- РЕДАКТИРОВАНИЕ ----------
        public async Task<IActionResult> Edit(int id)
        {
            var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == id && b.UserId == UserId);
            if (book == null) return NotFound();
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book book)
        {
            if (id != book.Id) return NotFound();

            var existing = await _db.Books.FirstOrDefaultAsync(b => b.Id == id && b.UserId == UserId);
            if (existing == null) return NotFound();

            if (!ModelState.IsValid) return View(book);

            existing.Title = book.Title;
            existing.Author = book.Author;
            existing.Genre = book.Genre;
            existing.Year = book.Year;
            existing.Pages = book.Pages;
            existing.CurrentPage = book.CurrentPage;
            existing.Rating = book.Rating;
            existing.Status = book.Status;
            existing.CoverUrl = book.CoverUrl;
            existing.Notes = book.Notes;
            existing.IsFavorite = book.IsFavorite;

            if (book.Status == ReadStatus.Read && existing.DateFinished == null)
                existing.DateFinished = DateTime.Now;
            if (book.Status != ReadStatus.Read)
                existing.DateFinished = null;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Изменения сохранены.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- УДАЛЕНИЕ ----------
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == id && b.UserId == UserId);
            if (book == null) return NotFound();
            return View(book);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == id && b.UserId == UserId);
            if (book != null)
            {
                _db.Books.Remove(book);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Книга удалена.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------- БЫСТРОЕ ИЗБРАННОЕ (AJAX) ----------
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == id && b.UserId == UserId);
            if (book == null) return NotFound();
            book.IsFavorite = !book.IsFavorite;
            await _db.SaveChangesAsync();
            return Json(new { ok = true, isFavorite = book.IsFavorite });
        }

        // ---------- СТАТИСТИКА ----------
        public async Task<IActionResult> Statistics()
        {
            var books = await _db.Books.Where(b => b.UserId == UserId).ToListAsync();

            var vm = new StatisticsViewModel
            {
                TotalBooks = books.Count,
                ReadCount = books.Count(b => b.Status == ReadStatus.Read),
                ReadingCount = books.Count(b => b.Status == ReadStatus.Reading),
                WantToReadCount = books.Count(b => b.Status == ReadStatus.WantToRead),
                TotalPagesRead = books.Where(b => b.Status == ReadStatus.Read).Sum(b => b.Pages ?? 0),
                AverageRating = books.Any(b => b.Rating.HasValue)
                    ? Math.Round(books.Where(b => b.Rating.HasValue).Average(b => b.Rating!.Value), 2)
                    : 0,
                ByGenre = books.Where(b => !string.IsNullOrWhiteSpace(b.Genre))
                    .GroupBy(b => b.Genre!)
                    .OrderByDescending(g => g.Count())
                    .ToDictionary(g => g.Key, g => g.Count()),
                ByAuthor = books.GroupBy(b => b.Author)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count()),
                FinishedByMonth = books.Where(b => b.DateFinished.HasValue)
                    .GroupBy(b => b.DateFinished!.Value.ToString("yyyy-MM"))
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            var wantToRead = books.Where(b => b.Status == ReadStatus.WantToRead).ToList();
            if (wantToRead.Count > 0)
            {
                var rnd = new Random();
                vm.BookOfTheMonth = wantToRead[rnd.Next(wantToRead.Count)];
            }

            return View(vm);
        }

        // ---------- ЭКСПОРТ CSV ----------
        public async Task<IActionResult> ExportCsv()
        {
            var books = await _db.Books.Where(b => b.UserId == UserId)
                .OrderBy(b => b.Title).ToListAsync();

            using var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, new UTF8Encoding(true), leaveOpen: true))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";" }))
            {
                csv.WriteField("Название");
                csv.WriteField("Автор");
                csv.WriteField("Жанр");
                csv.WriteField("Год");
                csv.WriteField("Страниц");
                csv.WriteField("Рейтинг");
                csv.WriteField("Статус");
                csv.WriteField("Дата завершения");
                csv.NextRecord();

                foreach (var b in books)
                {
                    csv.WriteField(b.Title);
                    csv.WriteField(b.Author);
                    csv.WriteField(b.Genre ?? "");
                    csv.WriteField(b.Year?.ToString() ?? "");
                    csv.WriteField(b.Pages?.ToString() ?? "");
                    csv.WriteField(b.Rating?.ToString(CultureInfo.InvariantCulture) ?? "");
                    csv.WriteField(b.Status.ToString());
                    csv.WriteField(b.DateFinished?.ToString("yyyy-MM-dd") ?? "");
                    csv.NextRecord();
                }
            }

            return File(memoryStream.ToArray(), "text/csv", "my-library.csv");
        }

        // ---------- ИМПОРТ CSV ----------
        [HttpGet]
        public IActionResult ImportCsv() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Выберите CSV-файл.");
                return View();
            }

            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HeaderValidated = null,
                MissingFieldFound = null
            });

            await csv.ReadAsync();
            csv.ReadHeader();

            int imported = 0;
            while (await csv.ReadAsync())
            {
                var title = csv.GetField("Название");
                var author = csv.GetField("Автор");
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(author)) continue;

                var book = new Book
                {
                    Title = title!,
                    Author = author!,
                    Genre = csv.GetField("Жанр"),
                    UserId = UserId,
                    DateAdded = DateTime.Now
                };

                if (int.TryParse(csv.GetField("Год"), out var year)) book.Year = year;
                if (int.TryParse(csv.GetField("Страниц"), out var pages)) book.Pages = pages;
                if (double.TryParse(csv.GetField("Рейтинг"), NumberStyles.Any, CultureInfo.InvariantCulture, out var rating)) book.Rating = rating;
                if (Enum.TryParse<ReadStatus>(csv.GetField("Статус"), out var status)) book.Status = status;

                _db.Books.Add(book);
                imported++;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Импортировано книг: {imported}.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- ЭКСПОРТ PDF ----------
        public async Task<IActionResult> ExportPdf()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var books = await _db.Books.Where(b => b.UserId == UserId)
                .OrderBy(b => b.Title).ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Text("Моя библиотека").FontSize(20).Bold();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Название
                            columns.RelativeColumn(2); // Автор
                            columns.RelativeColumn(2); // Жанр
                            columns.RelativeColumn(1); // Год
                            columns.RelativeColumn(1); // Рейтинг
                            columns.RelativeColumn(2); // Статус
                        });

                        table.Header(header =>
                        {
                            void HeaderCell(string text) => header.Cell().Element(c => c
                                .Background(Colors.Brown.Darken2).Padding(5))
                                .Text(text).FontColor(Colors.White).Bold();

                            HeaderCell("Название");
                            HeaderCell("Автор");
                            HeaderCell("Жанр");
                            HeaderCell("Год");
                            HeaderCell("Рейтинг");
                            HeaderCell("Статус");
                        });

                        foreach (var b in books)
                        {
                            table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Text(b.Title);
                            table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Text(b.Author);
                            table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Text(b.Genre ?? "-");
                            table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Text(b.Year?.ToString() ?? "-");
                            table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Text(b.Rating?.ToString(CultureInfo.InvariantCulture) ?? "-");
                            table.Cell().Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Text(b.Status.ToString());
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Сформировано ");
                        x.Span(DateTime.Now.ToString("dd.MM.yyyy")).SemiBold();
                    });
                });
            });

            var bytes = document.GeneratePdf();
            return File(bytes, "application/pdf", "my-library.pdf");
        }
    }
}
