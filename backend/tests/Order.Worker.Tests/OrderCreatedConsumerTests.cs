using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Order.Core.Domain.Repositories;
using Order.Infrastructure.Persistence;
using Order.Worker.Consumers;
using Xunit;
using orderEnum = Order.Core.Enums.OrderStatus;

public class OrderCreatedConsumerTests
{
    private static ServiceProvider BuildProvider(out OrderDbContext db, out SqliteConnection connection)
    {
        var services = new ServiceCollection();

        // 1. Criar uma conexão única em memória e manter aberta
        var sharedConnection = new SqliteConnection("DataSource=:memory:");
        sharedConnection.Open();
        // 2. Registrar o DbContext usando SEMPRE essa mesma conexão
        services.AddDbContext<OrderDbContext>(opt =>
        {
            opt.UseSqlite(sharedConnection);
        });

        // 3. Registrar o repositório real
        services.AddScoped<IOrderRepository, EfOrderRepository>();

        // 4. Criar o provider
        var sp = services.BuildServiceProvider();

        // 5. Criar o schema nessa conexão compartilhada
        db = sp.GetRequiredService<OrderDbContext>();
        db.Database.EnsureCreated();

        connection = sharedConnection;

        return sp;
    }

    [Fact]
    public async Task Should_Update_Status_To_Processing_Then_Finalized()
    {
        // Arrange
        var logger = NullLogger<OrderCreatedConsumer>.Instance;

        var sp = BuildProvider(out var db, out var connection);

        var order = Order.Core.Domain.Entities.Order.Create(
            clienteNome: "Tulio",
            produto: "Notebook",
            valor: 1000m
        );

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var fakeClient = new ServiceBusClient(
            "Endpoint=sb://fake.servicebus.local/;SharedAccessKeyName=Test;SharedAccessKey=TestKey="
        );

        var consumer = new OrderCreatedConsumer(
            logger,
            sp.GetRequiredService<IServiceScopeFactory>(),
            queueName: "orders"
        );

        // Act
        await consumer.ProcessOrderAsync(
            orderId: order.Id,
            messageId: order.Id.ToString(),
            ct: CancellationToken.None
        );

        // Assert
        using (var verificationScope = sp.CreateScope())
        {
            var freshDb = verificationScope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var reloaded = await freshDb.Orders.FirstAsync(x => x.Id == order.Id);
            reloaded.Status.Should().Be(orderEnum.Finalizado);
        }

        connection.Close();
    }

    private static Order.Core.Domain.Entities.Order NewOrder(
        string cliente,
        string produto,
        decimal valor,
        orderEnum? status = null)
    {
        var o = Order.Core.Domain.Entities.Order.Create(
            clienteNome: cliente,
            produto: produto,
            valor: valor
        );

        if (status is not null)
        {
            o.Status = status.Value;
        }

        return o;
    }


    [Fact]
    public async Task Should_Be_Idempotent_When_Same_Message_Is_Processed_Twice()
    {
        // Arrange
        var logger = NullLogger<OrderCreatedConsumer>.Instance;
        var sp = BuildProvider(out var db, out var connection);

        // cria pedido inicial pendente
        var order = Order.Core.Domain.Entities.Order.Create(
            clienteNome: "Tulio",
            produto: "Notebook Gamer",
            valor: 8000m
        );

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // mesmo messageId nas duas tentativas → simula recebimento duplicado
        var messageId = order.Id.ToString();

        var fakeClient = new ServiceBusClient(
            "Endpoint=sb://fake.servicebus.local/;SharedAccessKeyName=Test;SharedAccessKey=TestKey="
        );

        var consumer = new OrderCreatedConsumer(
            logger,
            sp.GetRequiredService<IServiceScopeFactory>(),
            queueName: "orders"
        );

        // Act 1: primeira vez processa normalmente
        await consumer.ProcessOrderAsync(
            orderId: order.Id,
            messageId: messageId,
            ct: CancellationToken.None
        );

        // Act 2: segunda vez, mesma mensagem repetida
        // se a idempotência estiver funcionando, isso NÃO deve reprocesar nem quebrar
        await consumer.ProcessOrderAsync(
            orderId: order.Id,
            messageId: messageId,
            ct: CancellationToken.None
        );

        // Assert
        using (var verificationScope = sp.CreateScope())
        {
            var freshDb = verificationScope.ServiceProvider.GetRequiredService<OrderDbContext>();

            // 1. Pedido continua finalizado
            var reloaded = await freshDb.Orders.FirstAsync(x => x.Id == order.Id);
            reloaded.Status.Should().Be(orderEnum.Finalizado);

            // 2. Só um registro de processed_messages pra esse messageId
            var occurrences = await freshDb.ProcessedMessages
                .CountAsync(pm => pm.MessageId == messageId);

            occurrences.Should().Be(1);
        }

        connection.Close();
    }
 [Fact]
    public async Task AddAsync_Should_Persist_Order_And_GetByIdAsync_Should_Return_It()
    {
        // Arrange
        var sp = BuildProvider(out var db, out var conn);
        var repo = sp.GetRequiredService<IOrderRepository>();

        var order = NewOrder("Tulio", "Notebook", 1000m);

        // Act
        await repo.AddAsync(order, CancellationToken.None);

        var loaded = await repo.GetByIdAsync(order.Id, CancellationToken.None);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.ClienteNome.Should().Be("Tulio");
        loaded.Produto.Should().Be("Notebook");
        loaded.Valor.Should().Be(1000m);
        loaded.Status.Should().Be(orderEnum.Pendente);

        conn.Close();
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_In_Descending_Order_By_data_criacao()
    {
        // Arrange
        var sp = BuildProvider(out var db, out var conn);
        var repo = sp.GetRequiredService<IOrderRepository>();

        var older = NewOrder("A", "Item1", 10m);
        // força data_criacao mais antiga
        older.data_criacao = DateTime.UtcNow.AddMinutes(-10);

        var newer = NewOrder("B", "Item2", 20m);
        newer.data_criacao = DateTime.UtcNow;

        await repo.AddAsync(older, CancellationToken.None);
        await repo.AddAsync(newer, CancellationToken.None);

        // Act
        var all = await repo.GetAllAsync(CancellationToken.None);

        // Assert: mais novo primeiro
        all.Should().HaveCount(2);
        all.First().Id.Should().Be(newer.Id);
        all.Last().Id.Should().Be(older.Id);

        conn.Close();
    }

    [Fact]
    public async Task UpdateAsync_Should_Save_Modified_Entity()
    {
        // Arrange
        var sp = BuildProvider(out var db, out var conn);
        var repo = sp.GetRequiredService<IOrderRepository>();

        var order = NewOrder("Tulio", "Mouse Gamer", 300m);
        await repo.AddAsync(order, CancellationToken.None);

        // muda o status em memória
        order.Status = orderEnum.Processando;

        // Act
        await repo.UpdateAsync(order, CancellationToken.None);

        // Assert -> precisamos ler com um novo DbContext pra garantir que persistiu
        using (var scope = sp.CreateScope())
        {
            var freshDb = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var again = await freshDb.Orders.FirstAsync(x => x.Id == order.Id);

            again.Status.Should().Be(orderEnum.Processando);
        }

        conn.Close();
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_True_When_Order_Exists()
    {
        // Arrange
        var sp = BuildProvider(out var db, out var conn);
        var repo = sp.GetRequiredService<IOrderRepository>();

        var order = NewOrder("Cliente X", "Teclado", 150m);
        await repo.AddAsync(order, CancellationToken.None);

        // Act
        var exists = await repo.ExistsAsync(order.Id, CancellationToken.None);
        var notExists = await repo.ExistsAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        exists.Should().BeTrue();
        notExists.Should().BeFalse();

        conn.Close();
    }


    [Fact]
    public async Task MarkProcessingIfPendingAsync_Should_Update_Status_From_Pendente_To_Processando_Once()
    {
        // Arrange
        var sp = BuildProvider(out var db, out var conn);
        var repo = sp.GetRequiredService<IOrderRepository>();

        // cria uma ordem PENDENTE
        var order = NewOrder("Tulio", "SSD", 500m, status: orderEnum.Pendente);
        await repo.AddAsync(order, CancellationToken.None);

        // Act 1: tenta marcar como Processando
        var changed1 = await repo.MarkProcessingIfPendingAsync(order.Id, CancellationToken.None);

        // Act 2: tenta de novo (agora ela já não está mais Pendente)
        var changed2 = await repo.MarkProcessingIfPendingAsync(order.Id, CancellationToken.None);

        // Assert
        changed1.Should().BeTrue("primeira chamada deve atualizar de Pendente -> Processando");
        changed2.Should().BeFalse("segunda chamada não deve atualizar nada porque já não está mais Pendente");

        // e vamos confirmar no banco que o status é Processando
        using (var scope = sp.CreateScope())
        {
            var freshDb = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var refreshed = await freshDb.Orders.FirstAsync(x => x.Id == order.Id);
            refreshed.Status.Should().Be(orderEnum.Processando);
        }

        conn.Close();
    }

}
