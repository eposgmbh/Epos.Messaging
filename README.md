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

### Sending a persistent message

```csharp
public class Note
{
    public string Id { get; set; }
    public string Text { get; set; }
    public string Author { get; set; }
    public DateTime Updated { get; set; }
}

public class NoteAddedIntegrationEvent : IntegrationEvent
{
    public Note AddedNote { get; set; }
}

// ...

Note theNote = ...;

IConnectionFactory theConnectionFactory =
    new ConnectionFactory { HostName = "localhost" };
IIntegrationEventPublisher thePublisher =
    new RabbitMQIntegrationEventPublisher(theConnectionFactory);

thePublisher.Publish(new NoteAddedIntegrationEvent { AddedNote = theNote });

// ...

thePublisher.Dispose();
```

The event is published reliably to a persistent rabbitMQ queue (retry count of 5 and exponential backoff). In a real world app you should create a singleton `RabbitMQIntegrationEventPublisher` in your application composition root (eg. `Startup` class). For that you can use the `ServiceCollectionExtensions` class.

### Recieving messages

```csharp
public class NoteAddedIntegrationEventHandler : IntegrationEventHandler<NoteAddedIntegrationEvent>
{
    public override Task Handle(NoteAddedIntegrationEvent e, MessagingHelper h) {
        var theMessage = $"Added note '{e.AddedNote.Text}' by {e.AddedNote.Author}.";

        Console.WriteLine(theMessage);
        h.Ack(); // <- acknowledge message is handled succesfully

        return Task.CompletedTask;
    }
}

IServiceProvider theServiceProvider = ...;
IConnectionFactory theConnectionFactory = new ConnectionFactory { HostName = "localhost" };

IIntegrationEventSubscriber theSubriber =
    new RabbitMQIntegrationEventSubscriber(theServiceProvider, theConnectionFactory);

theSubscriber.Subscribe<NoteAddedIntegrationEvent, NoteAddedIntegrationEventHandler>();
```

In a real world app you should create a singleton `RabbitMQIntegrationEventSubscriber` in your application composition root (eg. `Startup` class). For that you can use the `ServiceCollectionExtensions` class.

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
