#:property PublishAot=false
// The behaviour cases of docs/rules-check.md, run unchanged against every result.
// Usage: dotnet run behaviour-check.cs -- http://localhost:5199
// The API must have been started on a new database, so that books 1 to 20 are all available.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5199";
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
var results = new List<(string Case, bool Passed, string Detail)>();

await Run("1. Borrow an available book: 201, with the book and a due date 14 days ahead", async () =>
{
    var before = DateTimeOffset.UtcNow;
    var (status, loan) = await Borrow("c1", 1);
    Expect(status == HttpStatusCode.Created, $"status {status}");
    Expect(loan is not null && loan.BookId == 1, "the response does not give book 1");
    var expected = before.AddDays(14);
    Expect(Math.Abs((loan!.DueDate - expected).TotalMinutes) < 10, $"due date {loan.DueDate:O}, expected about {expected:O}");
});

await Run("2. Borrow a book another member has: 409", async () =>
{
    Expect((await Borrow("c2a", 2)).Status == HttpStatusCode.Created, "the first loan was not created");
    var (status, _) = await Borrow("c2b", 2);
    Expect(status == HttpStatusCode.Conflict, $"status {status}");
});

await Run("3. A fourth active loan: 409; after a return, the member can borrow again", async () =>
{
    var (_, first) = await Borrow("c3", 3);
    await Borrow("c3", 4);
    await Borrow("c3", 5);
    var (fourth, _) = await Borrow("c3", 6);
    Expect(fourth == HttpStatusCode.Conflict, $"fourth loan: status {fourth}");
    Expect(first is not null, "the first loan was not created");
    var returned = await Return("c3", first!.Id);
    Expect(returned == HttpStatusCode.NoContent, $"return: status {returned}");
    var (again, _) = await Borrow("c3", 6);
    Expect(again == HttpStatusCode.Created, $"loan after the return: status {again}");
});

await Run("4. Borrow a book that does not exist: 404", async () =>
{
    var (status, _) = await Borrow("c4", 999);
    Expect(status == HttpStatusCode.NotFound, $"status {status}");
});

await Run("5. Return your own loan: 204, and the book can be borrowed again", async () =>
{
    var (_, loan) = await Borrow("c5a", 7);
    Expect(loan is not null, "the loan was not created");
    var returned = await Return("c5a", loan!.Id);
    Expect(returned == HttpStatusCode.NoContent, $"return: status {returned}");
    var (again, _) = await Borrow("c5b", 7);
    Expect(again == HttpStatusCode.Created, $"loan by another member: status {again}");
});

await Run("6. Return someone else's loan: 404, and the loan stays active", async () =>
{
    var (_, loan) = await Borrow("c6a", 8);
    Expect(loan is not null, "the loan was not created");
    var returned = await Return("c6b", loan!.Id);
    Expect(returned == HttpStatusCode.NotFound, $"return by another member: status {returned}");
    var (_, loans) = await List("c6a");
    Expect(loans is not null && loans.Any(l => l.Id == loan.Id), "the loan is no longer in the owner's list");
});

await Run("7. Return a loan twice: the second time 409", async () =>
{
    var (_, loan) = await Borrow("c7", 9);
    Expect(loan is not null, "the loan was not created");
    await Return("c7", loan!.Id);
    var second = await Return("c7", loan.Id);
    Expect(second == HttpStatusCode.Conflict, $"second return: status {second}");
});

await Run("8. The list has only the member's own active loans", async () =>
{
    var (_, kept) = await Borrow("c8", 10);
    var (_, returnedLoan) = await Borrow("c8", 11);
    await Borrow("c8x", 12);
    Expect(kept is not null && returnedLoan is not null, "the loans were not created");
    await Return("c8", returnedLoan!.Id);
    var (status, loans) = await List("c8");
    Expect(status == HttpStatusCode.OK, $"status {status}");
    var ids = loans?.Select(l => l.Id).ToList() ?? [];
    Expect(ids.Count == 1 && ids[0] == kept!.Id, $"got loans [{string.Join(", ", ids)}], expected [{kept!.Id}]");
});

await Run("9. Without a member: 401 on the three endpoints", async () =>
{
    var (_, loan) = await Borrow("c9", 13);
    Expect(loan is not null, "the loan was not created");
    var borrow = (await Send(HttpMethod.Post, "/loans", null, new { bookId = 14 })).StatusCode;
    var list = (await Send(HttpMethod.Get, "/loans", null)).StatusCode;
    var giveBack = (await Send(HttpMethod.Post, $"/loans/{loan!.Id}/return", null)).StatusCode;
    var empty = (await Send(HttpMethod.Get, "/loans", "")).StatusCode;
    Expect(
        (borrow, list, giveBack, empty) == (HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized),
        $"borrow {borrow}, list {list}, return {giveBack}, empty header {empty}");
});

await Run("10. A missing or non-positive book ID: 400", async () =>
{
    var missing = (await Send(HttpMethod.Post, "/loans", "c10", new { })).StatusCode;
    var zero = (await Send(HttpMethod.Post, "/loans", "c10", new { bookId = 0 })).StatusCode;
    var negative = (await Send(HttpMethod.Post, "/loans", "c10", new { bookId = -1 })).StatusCode;
    Expect(
        (missing, zero, negative) == (HttpStatusCode.BadRequest, HttpStatusCode.BadRequest, HttpStatusCode.BadRequest),
        $"missing {missing}, zero {zero}, negative {negative}");
});

foreach (var (name, passed, detail) in results)
{
    Console.WriteLine($"{(passed ? "PASS" : "FAIL")}  {name}{(passed ? "" : $"\n      {detail}")}");
}

Console.WriteLine($"\n{results.Count(r => r.Passed)} of {results.Count} cases passed.");
return results.All(r => r.Passed) ? 0 : 1;

async Task Run(string name, Func<Task> body)
{
    try
    {
        await body();
        results.Add((name, true, ""));
    }
    catch (Exception ex)
    {
        results.Add((name, false, ex.Message));
    }
}

static void Expect(bool condition, string detail)
{
    if (!condition)
    {
        throw new CheckFailedException(detail);
    }
}

async Task<(HttpStatusCode Status, Loan? Loan)> Borrow(string member, int bookId)
{
    var response = await Send(HttpMethod.Post, "/loans", member, new { bookId });
    return (response.StatusCode, response.StatusCode == HttpStatusCode.Created ? await Read<Loan>(response) : null);
}

async Task<HttpStatusCode> Return(string member, int loanId) =>
    (await Send(HttpMethod.Post, $"/loans/{loanId}/return", member)).StatusCode;

async Task<(HttpStatusCode Status, List<Loan>? Loans)> List(string member)
{
    var response = await Send(HttpMethod.Get, "/loans", member);
    return (response.StatusCode, response.StatusCode == HttpStatusCode.OK ? await Read<List<Loan>>(response) : null);
}

async Task<HttpResponseMessage> Send(HttpMethod method, string path, string? member, object? body = null)
{
    using var request = new HttpRequestMessage(method, path);
    if (member is not null)
    {
        request.Headers.TryAddWithoutValidation("X-Member-Id", member);
    }

    if (body is not null)
    {
        request.Content = JsonContent.Create(body);
    }

    return await http.SendAsync(request);
}

static async Task<T?> Read<T>(HttpResponseMessage response)
{
    try
    {
        return await response.Content.ReadFromJsonAsync<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
    catch (JsonException ex)
    {
        throw new CheckFailedException($"the response body is not the expected JSON: {ex.Message}");
    }
}

internal sealed record Loan(int Id, int BookId, DateTimeOffset DueDate);

internal sealed class CheckFailedException(string message) : Exception(message);
