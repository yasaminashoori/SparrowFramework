using Sparrow.NET.Framework.Network;

public class Program
{
    public static async Task Main()
    {
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
                context.Response.StatusCode = 200;
                context.Response.WriteJson(new
                {
                    Name = "thisisnabi",
                    Version = "1.0.0",
                    Description = "Sample response",
                    Timestamp = DateTime.UtcNow
                });

                await context.Response.CloseAsync();
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    // TODO
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
