#nullable enable

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Order.Infrastructure.Messaging;
using Xunit;

namespace Order.Infrastructure.Tests;

public class ServiceBusPublisherTests
{
    [Fact]
    public async Task PublishOrderCreatedAsync_DeveCriarMessageComCorpoEHeadersCorretos_EChamarSendMessageAsync()
    {
        // arrange
        var fakeOrderId = Guid.NewGuid();
        var fakeCorrelationId = Guid.NewGuid();
        var eventType = "OrderCreated";
        var ct = CancellationToken.None;

        // mock do sender
        var senderMock = new Mock<ServiceBusSender>(MockBehavior.Strict);

        ServiceBusMessage? capturedMessage = null;

        senderMock
            .Setup(s => s.SendMessageAsync(
                It.IsAny<ServiceBusMessage>(),
                ct
            ))
            .Returns(Task.CompletedTask)
            .Callback<ServiceBusMessage, CancellationToken>((msg, _) =>
            {
                capturedMessage = msg;
            });

        // NEW: precisamos permitir que o publisher faça DisposeAsync()
        senderMock
            .Setup(s => s.DisposeAsync())
            .Returns(new ValueTask());

        // mock do client pra retornar o sender mockado
        var clientMock = new Mock<ServiceBusClient>(MockBehavior.Strict);

        clientMock
            .Setup(c => c.CreateSender("orders"))
            .Returns(senderMock.Object);

        // NEW: idem aqui, o publisher vai chamar _client.DisposeAsync()
        clientMock
            .Setup(c => c.DisposeAsync())
            .Returns(new ValueTask());

        // logger "vazio"
        ILogger<ServiceBusPublisher> logger = NullLogger<ServiceBusPublisher>.Instance;

        // SUT
        await using var publisher = new ServiceBusPublisher(
            clientMock.Object,
            logger
        );

        // act
        await publisher.PublishOrderCreatedAsync(
            fakeOrderId,
            eventType,
            fakeCorrelationId,
            ct
        );

        // assert: verificar que SendMessageAsync foi chamado
        senderMock.Verify(s =>
            s.SendMessageAsync(
                It.IsAny<ServiceBusMessage>(),
                ct
            ),
            Times.Once,
            "deve enviar exatamente uma mensagem no bus"
        );

        // assert: conferir a mensagem enviada
        capturedMessage.Should().NotBeNull("precisamos inspecionar a mensagem montada");

        capturedMessage!.MessageId.Should().Be(fakeOrderId.ToString());
        capturedMessage.CorrelationId.Should().Be(fakeCorrelationId.ToString());
        capturedMessage.ContentType.Should().Be("application/json");

        // O body do ServiceBusMessage é BinaryData. Vamos desserializar de volta
        var bodyJson = capturedMessage.Body.ToString();
        var bodyObj = JsonSerializer.Deserialize<BodyContract>(bodyJson);
        bodyObj.Should().NotBeNull();
        bodyObj!.OrderId.Should().Be(fakeOrderId);

        // application properties
        capturedMessage.ApplicationProperties.Should().ContainKey("EventType");
        capturedMessage.ApplicationProperties.Should().ContainKey("OrderId");

        capturedMessage.ApplicationProperties["EventType"]
            .Should().Be(eventType);

        capturedMessage.ApplicationProperties["OrderId"]
            .Should().Be(fakeOrderId.ToString());
    }

    private sealed class BodyContract
    {
        public Guid OrderId { get; set; }
    }

    [Fact]
    public async Task DisposeAsync_DeveChamarDisposeNoSenderEClient()
    {
        // arrange
        var senderMock = new Mock<ServiceBusSender>(MockBehavior.Strict);

        senderMock
            .Setup(s => s.DisposeAsync())
            .Returns(new ValueTask());

        var clientMock = new Mock<ServiceBusClient>(MockBehavior.Strict);

        clientMock
            .Setup(c => c.CreateSender("orders"))
            .Returns(senderMock.Object);

        clientMock
            .Setup(c => c.DisposeAsync())
            .Returns(new ValueTask());

        ILogger<ServiceBusPublisher> logger = NullLogger<ServiceBusPublisher>.Instance;

        // act
        var publisher = new ServiceBusPublisher(
            clientMock.Object,
            logger
        );

        await publisher.DisposeAsync();

        // assert
        senderMock.Verify(s => s.DisposeAsync(), Times.Once);
        clientMock.Verify(c => c.DisposeAsync(), Times.Once);
    }
}
