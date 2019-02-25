# Epos.Messaging

![Build Status](https://eposgmbh.visualstudio.com/Epos.Messaging/_apis/build/status/Epos.Messaging%20Build)
[![NuGet](https://img.shields.io/nuget/v/Epos.Messaging.svg)](https://www.nuget.org/packages/Epos.Messaging/)
![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Downloads](https://img.shields.io/nuget/dt/Epos.Messaging.svg)

Simple library for reliable messaging with RabbitMQ (for publishing messages and requests between Microservices).

Build and Release deployment ([NuGet](https://www.nuget.org/)) is automated with
[Azure Devops](https://azure.microsoft.com/en-us/services/devops/). Try it, it's free and very powerful.

## Installation

Via NuGet you can install the NuGet packages **Epos.Messaging** and **Epos.Messaging.RabbitMQ**.

```bash
$ dotnet add package Epos.Messaging
$ dotnet add package Epos.Messaging.RabbitMQ
```

## Usage

### Supported patterns

|Pattern|Meaning|Types|
|-|-|-|
|Integration command|Persistent commands that need to be handled|IIntegrationCommandPublisher<br>IIntegrationCommandSubscriber<br>IntegrationCommand<br>IIntegrationCommandHandler|
|Integration request|Transient request that can timeout and delivers a reply, if successful|IIntegrationRequestPublisher<br>IIntegrationRequestSubscriber<br>IntegrationRequest<br>IntegrationReply<br>IIntegrationRequestHandler|
|Integration event|Transient events that can be handled by multiple subscribers|Not implemented yet|

### Publishing an Integration Command

An `Integration Command` is durable, persistent and **must** be handeled by exactly one handler.

```csharp
// Model and corresponding integration command

public class Note
{
    public string Id { get; set; }
    public string Text { get; set; }
    public string Author { get; set; }
    public DateTime Updated { get; set; }
}

public class NoteAddedIntegrationCommand : IntegrationCommand
{
    public Note AddedNote { get; set; }
}

// Startup.cs

services.AddIntegrationCommandPublisherRabbitMQ();
services.Configure<RabbitMQOptions>(options => {
    options.Hostname = "localhost";
    opions.Username = "guest";
    options.Password = "guest";
});

// NoteController.cs

public class NoteController
{
    // ...

    public NoteController(IIntegrationCommandPublisher publisher) {
        this.publisher = publisher;
    }

    // ...

    [HttpPost]
    public async Task<ActionResult> Post(Note note) {
        // ...

        await this.publisher.PublishAsync(new NoteAddedIntegrationCommand { AddedNote = note });

        // ...
    }
}
```
The command is published reliably to a persistent RabbitMQ queue.

### Handling an Integration Command

As long as no command handler is registered, the command is waiting and persisted in a RabbitMQ Queue.

**IMPORTANT** Do not forget to acknowledge the command after successfully handling the command. Otherwise the command
will be redelivered.

```csharp
// Command handler class

public class NoteAddedCommandHandler : IIntegrationCommandHandler<NoteAddedIntegrationCommand>
{
    public Task Handle(NoteAddedIntegrationCommand c, CancellationToken token, CommandHelper h) {
        var theMessage = $"Added note '{c.AddedNote.Text}' by {e.AddedNote.Author}.";

        Console.WriteLine(theMessage);
        h.Ack(); // <- acknowledge command is handled succesfully

        return Task.CompletedTask;
    }
}

// Startup.cs

services.AddIntegrationCommandSubscriberRabbitMQ();
services.Configure<RabbitMQOptions>(options => {
    options.Hostname = "localhost";
    opions.Username = "guest";
    options.Password = "guest";
});
services.AddIntegrationCommandHandler<NoteAddedCommandHandler>();

// App code (e.g. in Program.cs before IHost.Run)

ISubscription subscription = await theSubscriber.SubscribeAsync<NoteAddedIntegrationCommand>();

// ...

// The subscription can be gracefully canceled
```

### Publishing an Integration Request

An `Integration Request` is transient and **must** be handeled by exactly one handler. The handler can timeout,
so be prepared for that.

```csharp
// Integration request

public class CalculationRequest : IntegrationRequest
{
    public int Number { get; set; }
}

// Integration reply

public class CalculationReply : IntegrationReply
{
    public int DoubledNumber { get; set; }
}

// Startup.cs

services.AddIntegrationRequestPublisherRabbitMQ();
services.Configure<RabbitMQOptions>(options => {
    options.Hostname = "localhost";
    opions.Username = "guest";
    options.Password = "guest";
});

// CalculationController.cs

public class CalculationController
{
    // ...

    public CalculationController(IIntegrationRequestPublisher publisher) {
        this.publisher = publisher;
    }

    // ...

    [HttpPost]
    public async Task<ActionResult> Post(CalculationRequest request) {
        // ...

        try {
            CalculationReply reply =
                await this.publisher.PublishAsync<CalculationRequest, CalculationReply>(request);
        } catch (TimeoutException) {
            // Handle timeout
        }

        // ...
    }
}
```

The request is published to a transient RabbitMQ queue.

### Handling an Integration Request

As long as no request handler is registered, the request will timeout.

```csharp
// Request handler class

public class CalculationRequestHandler : IIntegrationRequestHandler<CalculationRequest, CalculationReply>
{
    public Task<CalculationReply> Handle(CalculationRequest request, CancellationToken token) =>
        Task.FromResult(new CalculationReply { DoubledNumber = request.Number * 2 });
}

// Startup.cs

services.AddIntegrationRequestSubscriberRabbitMQ();
services.Configure<RabbitMQOptions>(options => {
    options.Hostname = "localhost";
    opions.Username = "guest";
    options.Password = "guest";
});
services.AddIntegrationRequestHandler<CalculationRequestHandler>();

// App code (e.g. in Program.cs before IHost.Run)

ISubscription subscription = await theSubscriber.SubscribeAsync<CalculationRequest, CalculationReply>();

// ...

// The subscription can be canceled

subscription.Cancel();
```

### Examples

See unit tests in `Epos.Messaging.RabbitMQ.Tests`.

## License

MIT License

Copyright (c) 2019 eposgmbh

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
