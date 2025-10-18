using System.Net.Http.Json;

namespace Cart.Cli.Client;

internal static class CartClient
{
    public static async Task Run()
    {
        Console.Write("Type loop count: ");
        var input = Console.ReadLine();
        var loopCount = int.Parse(input ?? "1");
        Console.WriteLine($"Running test with {loopCount} loops...");
        Console.WriteLine("");
        Console.WriteLine("");

        for (var i = 0; i < loopCount; i++)
        {
            Console.WriteLine($"========== LOOP {i + 1} of {loopCount} ==========");
            await Execute();
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Thread.Sleep(100);
        }
    }

    static async Task Execute()
    {
        var products = new[]
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        var cartId = Guid.NewGuid();

        // Adding some stuff to cart
        var itemCount1 = RandomInt(1, 3);
        for (var i = 0; i < itemCount1; i++)
        {
            var productId = products[RandomInt(0, products.Length)];
            await AddItem(cartId, productId);
        }

        // Clearing the cart
        await ClearCart(cartId);

        // Adding some new stuff to cart
        var itemCount2 = RandomInt(1, 3);
        for (var i = 0; i < itemCount2; i++)
        {
            var productId = products[RandomInt(0, products.Length)];
            await AddItem(cartId, productId);
        }

        // Changing price for one of the products
        await ChangePrice(products[RandomInt(0, products.Length)]);

        // Presenting the cart items
        await ShowCartItems(cartId);

        // Submitting the cart
        await SubmitCart(cartId);
    }

    static async Task AddItem(Guid cartId, Guid productId)
    {
        Console.Write($"Adding item to cart... cartId='{cartId}', productId='{productId}'");
        using var client = new HttpClient();
        var url = "https://localhost:7165/api/cart/items/add/v1";
        var description = RandomDescription();
        var result = await client.PostAsJsonAsync(url, new
        {
            CartId = cartId,
            Description = description,
            Image = description.Replace(" ", "_").ToLower() + ".png",
            Price = 19.99m,
            ItemId = Guid.NewGuid(),
            ProductId = productId
        });
        var content = await result.Content.ReadAsStringAsync();
        Console.WriteLine($" - {result.StatusCode}: {content}");
    }

    static async Task ClearCart(Guid cartId)
    {
        Console.Write($"Clearing cart... cartId='{cartId}'");
        using var client = new HttpClient();
        var url = "https://localhost:7165/api/cart/clear/v1";
        var result = await client.PostAsJsonAsync(url, new
        {
            CartId = cartId,
        });
        var content = await result.Content.ReadAsStringAsync();
        Console.WriteLine($" - {result.StatusCode}: {content}");
    }

    static async Task SubmitCart(Guid cartId)
    {
        Console.Write($"Submitting cart... cartId='{cartId}'");
        using var client = new HttpClient();
        var url = "https://localhost:7165/api/cart/submit/v1";
        var result = await client.PostAsJsonAsync(url, new
        {
            CartId = cartId,
        });
        var content = await result.Content.ReadAsStringAsync();
        Console.WriteLine($" - {result.StatusCode}: {content}");
    }

    static async Task ChangePrice(Guid productId)
    {
        Console.Write($"Changing price for product... productId='{productId}'");
        using var client = new HttpClient();
        var url = "https://localhost:7165/api/external/change-price/v1";
        var result = await client.PostAsJsonAsync(url, new
        {
            ProductId = productId,
            NewPrice = 29.99m,
            OldPrice = 19.99m,
        });
        var content = await result.Content.ReadAsStringAsync();
        Console.WriteLine($" - {result.StatusCode}: {content}");
    }

    static async Task ShowCartItems(Guid cartId)
    {
        Console.WriteLine($"== CART ITEMS == '{cartId}'");
        using var client = new HttpClient();
        var url = $"https://localhost:7165/api/cart/items/v1?cartId={cartId}";
        var result = await client.GetAsync(url);
        var content = await result.Content.ReadAsStringAsync();
        var cart = await result.Content.ReadFromJsonAsync<Cart>();
        foreach (var e in cart?.Items ?? [])
        {
            Console.WriteLine(e);
        }
        Console.WriteLine($"Total price: {cart?.TotalPrice}");
    }

    private record Cart(Guid CartId, decimal TotalPrice, IEnumerable<object> Items);

    static int RandomInt(int min, int max)
    {
        var rnd = new Random();
        return rnd.Next(min, max);
    }

    static string RandomDescription()
    {
        var adjectives = new[] { "Small", "Ergonomic", "Rustic", "Intelligent", "Gorgeous", "Incredible", "Fantastic", "Practical", "Sleek", "Awesome" };
        var materials = new[] { "Steel", "Wooden", "Concrete", "Plastic", "Cotton", "Granite", "Rubber", "Leather", "Silk", "Wool" };
        var products = new[] { "Chair", "Car", "Computer", "Keyboard", "Mouse", "Bike", "Ball", "Gloves", "Pants", "Shirt" };
        var rnd = new Random();
        var adjective = adjectives[rnd.Next(adjectives.Length)];
        var material = materials[rnd.Next(materials.Length)];
        var product = products[rnd.Next(products.Length)];
        return $"{adjective} {material} {product}";
    }
}
