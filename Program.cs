using BookStoreApi.Models;
using BookStoreApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Read the BookStoreDatabase settings and register the service (one MongoClient for the app)
builder.Services.Configure<BookStoreDatabaseSettings>(
    builder.Configuration.GetSection("BookStoreDatabase"));
builder.Services.AddSingleton<BooksService>();

// 2. Keep JSON property names as written in C# (Name, Price), not camelCase
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});

// 3. OpenAPI document at /openapi/v1.json.
//    Version 3.0 so Swagger UI can read it (.NET 11 defaults to 3.2, which Swagger UI rejects).
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
});

var app = builder.Build();

// 4. Swagger UI at /swagger (Development only).
//    Needs: dotnet add package Swashbuckle.AspNetCore.SwaggerUI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "BookStore API");
    });
}

// 5. Endpoints, all under /books, shown under "Books" in Swagger
var books = app.MapGroup("/books").WithTags("Books");

// GET /books
books.MapGet("/", async (BooksService service) =>
    await service.GetAsync());

// GET /books/{id}   (an ObjectId is 24 characters)
books.MapGet("/{id:length(24)}", async (string id, BooksService service) =>
    await service.GetAsync(id) is Book book
        ? Results.Ok(book)
        : Results.NotFound());

// POST /books
books.MapPost("/", async (Book newBook, BooksService service) =>
{
    await service.CreateAsync(newBook);
    return Results.Created($"/books/{newBook.Id}", newBook);
});

// PUT /books/{id}
books.MapPut("/{id:length(24)}", async (string id, Book updatedBook, BooksService service) =>
{
    var book = await service.GetAsync(id);
    if (book is null)
    {
        return Results.NotFound();
    }

    updatedBook.Id = book.Id;
    await service.UpdateAsync(id, updatedBook);
    return Results.NoContent();
});

// DELETE /books/{id}
books.MapDelete("/{id:length(24)}", async (string id, BooksService service) =>
{
    var book = await service.GetAsync(id);
    if (book is null)
    {
        return Results.NotFound();
    }

    await service.RemoveAsync(id);
    return Results.NoContent();
});

app.Run();
