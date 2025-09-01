using Sparrow.NET.Framework.Network; // <- فریمورک خودت

public class Program
{
    public static async Task Main()
    {
        // Start HTTP listener
        var ourServer = new OurHttpListener();
        ourServer.AddPrefix("http://localhost:11231/");
        ourServer.Start();

        Console.WriteLine("Server started at http://localhost:11231/");

        while (ourServer.IsListening)
        {
            try
            {
                var context = await ourServer.GetNewRequestAsync();

                Console.WriteLine($"Received request: {context.Request.HttpMethod} {context.Request.Path}");

                // نمونه: 200 یا 404 — طبق نیاز خودت تغییر بده
                context.Response.StatusCode = 200;

                // نمونه JSON (طبق عکس‌ها: Write و WriteJson روی Response)
                context.Response.WriteJson(new
                {
                    Name = "thisisnabi",
                    Version = "1.0.0",
                    Description = "This is a sample response from OurFramework.NET",
                    Timestamp = DateTime.UtcNow
                });

                await context.Response.CloseAsync();
            }
            catch (ObjectDisposedException)
            {
                break; // Listener stopped
            }
        }
    }

    // طبق اسکرین‌شات‌ها: این دو helper هم اگر بخواهی از بیرون صدا بزنی وجود دارند.
    static Task WriteTextAsync(OurHttpListenerResponse res, string text)
    {
        res.Write(text);
        return Task.CompletedTask;
    }

    static async Task WriteJsonAsync(OurHttpListenerResponse res, object data)
    {
        res.WriteJson(data);
        await res.CloseAsync();
    }
}
