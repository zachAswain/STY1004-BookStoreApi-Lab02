using BookStoreApi.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BookStoreApi.Services;

public class BooksService(IOptions<BookStoreDatabaseSettings> bookStoreDatabaseSettings)
{
    private readonly IMongoCollection<Book> _booksCollection = new MongoClient(bookStoreDatabaseSettings.Value.ConnectionString)
        .GetDatabase(bookStoreDatabaseSettings.Value.DatabaseName)
        .GetCollection<Book>(bookStoreDatabaseSettings.Value.BooksCollectionName);

    public async Task<List<Book>> GetAsync() =>
        await _booksCollection.Find(_ => true).ToListAsync();

    public async Task<Book?> GetAsync(string id) =>
        await _booksCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<List<Book>> SearchAsync(string? category, decimal? minPrice, decimal? maxPrice)
    {
        var filter = Builders<Book>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(category))
            filter &= Builders<Book>.Filter.Eq(b => b.Category, category);

        if (minPrice.HasValue)
            filter &= Builders<Book>.Filter.Gte(b => b.Price, minPrice.Value);

        if (maxPrice.HasValue)
            filter &= Builders<Book>.Filter.Lte(b => b.Price, maxPrice.Value);

        return await _booksCollection.Find(filter).ToListAsync();
    }

    public async Task<List<TopBook>> GetTopAsync(int count) =>
    await _booksCollection.Find(_ => true)
        .SortByDescending(b => b.Price)
        .Limit(count)
        .Project(b => new TopBook { Name = b.BookName, Price = b.Price })
        .ToListAsync();

    public async Task CreateAsync(Book newBook) =>
        await _booksCollection.InsertOneAsync(newBook);

    public async Task<List<string>> CreateManyAsync(List<Book> newBooks)
    {
        await _booksCollection.InsertManyAsync(newBooks);
        return newBooks.Select(b => b.Id!).ToList();
    }

    public async Task UpdateAsync(string id, Book updatedBook) =>
        await _booksCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

    public async Task RemoveAsync(string id) =>
        await _booksCollection.DeleteOneAsync(x => x.Id == id);
}