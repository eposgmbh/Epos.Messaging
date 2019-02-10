# Epos.Eventing

![Build Status](https://eposgmbh.visualstudio.com/_apis/public/build/definitions/25d5aae4-7b25-4a62-b533-5682b0d20fe1/7/badge)
[![NuGet](https://img.shields.io/nuget/v/Epos.Eventing.svg)](https://www.nuget.org/packages/Epos.Eventing/)
![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Downloads](https://img.shields.io/nuget/dt/Epos.Eventing.svg)

Simple library for reliable messaging with RabbitMQ (for publishing messages between Microservices).

Build and Release deployment ([NuGet](https://www.nuget.org/)) is automated with
[Visual Studio Team Services](https://www.visualstudio.com/team-services). Try it, it's free and powerful.

## Installation

Via NuGet you can install the NuGet packages **Epos.Eventing** and **Epos.Eventing.RabbitMQ**.

```bash
$ dotnet add package Epos.Eventing
$ dotnet add package Epos.Eventing.RabbitMQ
```

## Usage

### Sending an Integration Command

An `Integration Command` is durable, persistent and **must** be handeled by exactly one handler.

```csharp
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

// ...

Note theNote = ...;

IConnectionFactory theConnectionFactory =
    new ConnectionFactory { HostName = "localhost" };
IIntegrationCommandPublisher thePublisher =
    new RabbitMQIntegrationCommandPublisher(theConnectionFactory);

thePublisher.Publish(new NoteAddedIntegrationCommand { AddedNote = theNote });

// ...

thePublisher.Dispose();
```

The command is published reliably to a persistent rabbitMQ queue (retry count of 5 and exponential backoff). In a real
world app you should create a singleton `RabbitMQIntegrationCommandPublisher` in your application composition root (eg.
`Startup` class). For that you can use the `EposEventingServiceCollectionExtensions` class.

### Handling an Integration Command

As long as no command handler is registered, the command is waiting and persisted in a RabbitMQ Queue.

**IMPORTANT** Do not forget to acknowledge the command after successfully handling the command. Otherwise the command
will be redelivered.

```csharp
public class NoteAddedIntegrationCommandHandler : IntegrationCommandHandler<NoteAddedIntegrationCommand>
{
    public override Task Handle(NoteAddedIntegrationCommand c, MessagingHelper h) {
        var theMessage = $"Added note '{c.AddedNote.Text}' by {e.AddedNote.Author}.";

        Console.WriteLine(theMessage);
        h.Ack(); // <- acknowledge command is handled succesfully

        return Task.CompletedTask;
    }
}

IServiceProvider theServiceProvider = ...;
IConnectionFactory theConnectionFactory = new ConnectionFactory { HostName = "localhost" };

IIntegrationCommandSubscriber theSubriber =
    new RabbitMQIntegrationCommandSubscriber(theServiceProvider, theConnectionFactory);

theSubscriber.Subscribe<NoteAddedIntegrationCommand, NoteAddedIntegrationCommandHandler>();
```

In a real world app you should create a singleton `RabbitMQIntegrationCommandSubscriber` in your application composition
root (eg. `Startup` class). For that you can use the `EposEventingServiceCollectionExtensions` class.

### Integration Events

`Integration Events` can be applied and handled just like Integration Commands (see above). The difference is that
Integration Events need not be delivered and are therefore not persisted. Furthermore anyone subscribing to an
Integration Event recieves the same Integration Event (i.e. fanout message, an Integration Command is only handled by
exactly one handler.)

### Examples

See unit tests in `Epos.Eventing.RabbitMQ.Tests`.

## License

MIT License

Copyright (c) 2017 eposgmbh

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
