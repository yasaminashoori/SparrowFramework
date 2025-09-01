# 🐦 Sparrow Framework

<div align="center">

![Sparrow Logo](sparrow-logo.svg)

**An educational and lightweight HTTP framework for .NET**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](LICENSE)
[![Stars](https://img.shields.io/github/stars/yourusername/sparrow?style=for-the-badge&color=yellow)](https://github.com/yourusername/sparrow/stargazers)

*small, fast, and efficient* ✨

[Quick Start](#-quick-start) • [Examples](#-examples) • [Documentation](#-documentation) • [Contributing](#-contributing)

</div>

---

## 💝 Acknowledgments

- Inspired by **ASP.NET Core Course By Nabi Karampour** [Github Link](https://github.com/thisisnabi)
- Built with ❤️ for the .NET community  

## 🌟 Features

- **🔧 Lightweight** - No external dependencies, pure .NET  
- **📡 Basic HTTP** - Handle requests & responses with JSON or text  
- **⚡ Async/Await** - Modern async loop with `Task`  
- **🧩 Educational** - Learn how HTTP servers work under the hood  

---

## 🏁 Quick Start

### Installation

```bash
git clone https://github.com/yourusername/sparrow.git
cd sparrow
dotnet run
```

### Your First Sparrow App

```csharp
using Sparrow.NET.Framework.Network;

var server = new OurHttpListener();
server.AddPrefix("http://localhost:11231/");
server.Start();

Console.WriteLine("🚀 Sparrow is flying at http://localhost:11231/");

while (server.IsListening)
{
    var context = await server.GetNewRequestAsync();

    Console.WriteLine($"→ {context.Request.HttpMethod} {context.Request.Path}");

    context.Response.StatusCode = 200;
    context.Response.WriteJson(new
    {
        message = "Hello from Sparrow! 🐦",
        timestamp = DateTime.UtcNow
    });

    await context.Response.CloseAsync();
}
```

Visit [http://localhost:11231/](http://localhost:11231/) 🎉

---

## 📚 Examples

### Plain Text

```csharp
context.Response.StatusCode = 200;
context.Response.Write("Hello World from Sparrow!");
await context.Response.CloseAsync();
```

### JSON

```csharp
context.Response.StatusCode = 200;
context.Response.WriteJson(new { message = "Hello JSON!", time = DateTime.UtcNow });
await context.Response.CloseAsync();
```

---

## 🔧 API Reference

### OurHttpListener

```csharp
var server = new OurHttpListener();
server.AddPrefix("http://localhost:11231/");
server.Start();
await server.StopAsync();                   // stop gracefully
server.IsListening                          // check if running

var context = await server.GetNewRequestAsync(); // get next request
```

### Request Object

```csharp
context.Request.HttpMethod   // GET, POST, etc.
context.Request.Path         // /, /api, etc.
context.Request.Protocol     // HTTP/1.1
context.Request.Headers      // Dictionary<string,string>
context.Request.Body         // Raw body (ReadOnlyMemory<byte>)
```

### Response Object

```csharp
context.Response.StatusCode = 200;
context.Response.Write("plain text");
context.Response.WriteJson(new { hello = "world" });
await context.Response.CloseAsync();
```

---

## 🛣️ Roadmap

- [ ] Routing (map paths to handlers)  
- [ ] Query parameters parsing  
- [ ] Middleware pipeline  
- [ ] Static file serving  
- [ ] Cookies  
- [ ] Basic authentication  
- [ ] WebSocket support  

---

## 🧪 Testing

```bash
# Run server
dotnet run

# Test with curl
curl http://localhost:11231/
curl -H "Accept: application/json" http://localhost:11231/
```

---

## 🤝 Contributing

1. Fork 🍴 the repo  
2. Create 🌿 a branch  
3. Commit 💫 your changes  
4. Push 🚀 and PR 🎯  

---

<div align="center">

**Made with ❤️ and ☕**  

⭐ If you like Sparrow, give it a star on GitHub!

</div>
