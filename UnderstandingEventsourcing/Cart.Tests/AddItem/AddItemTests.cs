using AutoFixture;
using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventStore.InMemory;
using UnderstandingEventsourcingExample.Cart.AddItem;
using UnderstandingEventsourcingExample.Cart.Domain;
using UnderstandingEventsourcingExample.Cart.Domain.EventUpcast;

namespace UnderstandingEventsourcingExample.Tests.AddItem;

public class AddItemTests
{
    private readonly Fixture _fixture = new Fixture();
    private IEventStore _eventStore;
    private CartRepository _repository;
    private AddItemCommandHandler _handler;
    private FakeDeviceFingerPrintCalculator _fingerPrintCalculator;

    public AddItemTests()
    {
        _eventStore = new InMemoryEventStore();
        _eventStore.RegisterUpcaster(new ItemAddedEventUpcaster());
        _repository = new CartRepository(_eventStore);
        _fingerPrintCalculator = new FakeDeviceFingerPrintCalculator();
        _handler = new AddItemCommandHandler(_repository, _fingerPrintCalculator);
    }

    [Fact]
    public async Task CanAddItem()
    {
        var cartId = Guid.NewGuid();
        _fingerPrintCalculator.FingerPrint = Guid.NewGuid().ToString();

        List<IDomainEvent> givenEvents = [];

        var command = new AddItemCommand(
            CartId: cartId,
            Description: _fixture.Create<string>(),
            Image: _fixture.Create<string>(),
            Price: _fixture.Create<decimal>(),
            TotalPrice: _fixture.Create<decimal>(),
            ItemId: _fixture.Create<Guid>(),
            ProductId: _fixture.Create<Guid>()
        );

        List<IDomainEvent> expectedEvents =
        [
            new CartCreatedEvent(cartId),
            new ItemAddedEventV2(
                CartId: command.CartId,
                Description: command.Description,
                Image: command.Image,
                Price: command.Price,
                ItemId: command.ItemId,
                ProductId: command.ProductId,
                DeviceFingerPrint: _fingerPrintCalculator.FingerPrint
            )
        ];

        await CommandValidator
            .Setup(_eventStore, cartId.ToString())
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then(expectedEvents);
    }

    [Fact]
    [Obsolete]
    public async Task CanAddAdditionalItem()
    {
        var cartId = Guid.NewGuid();
        _fingerPrintCalculator.FingerPrint = Guid.NewGuid().ToString();

        List<IDomainEvent> givenEvents =
        [
            new CartCreatedEvent(cartId),
            new ItemAddedEvent(
                CartId: cartId,
                Description: _fixture.Create<string>(),
                Image: _fixture.Create<string>(),
                Price: _fixture.Create<decimal>(),
                ItemId: _fixture.Create<Guid>(),
                ProductId: _fixture.Create<Guid>()
            )
        ];

        var command = new AddItemCommand(
            CartId: cartId,
            Description: _fixture.Create<string>(),
            Image: _fixture.Create<string>(),
            Price: _fixture.Create<decimal>(),
            TotalPrice: _fixture.Create<decimal>(),
            ItemId: _fixture.Create<Guid>(),
            ProductId: _fixture.Create<Guid>()
        );

        List<IDomainEvent> expectedEvents =
        [
            new ItemAddedEventV2(
                CartId: command.CartId,
                Description: command.Description,
                Image: command.Image,
                Price: command.Price,
                ItemId: command.ItemId,
                ProductId: command.ProductId,
                DeviceFingerPrint: _fingerPrintCalculator.FingerPrint
            )
        ];

        await CommandValidator
            .Setup(_eventStore, cartId.ToString())
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then(expectedEvents);
    }

    [Fact]
    [Obsolete]
    public async Task FailsWhenMoreThat3ItemsAreAdded()
    {
        var cartId = Guid.NewGuid();

        List<IDomainEvent> givenEvents =
        [
            new CartCreatedEvent(cartId),
            new ItemAddedEvent(
                CartId: cartId,
                Description: _fixture.Create<string>(),
                Image: _fixture.Create<string>(),
                Price: _fixture.Create<decimal>(),
                ItemId: _fixture.Create<Guid>(),
                ProductId: _fixture.Create<Guid>()
            ),
            new ItemAddedEvent(
                CartId: cartId,
                Description: _fixture.Create<string>(),
                Image: _fixture.Create<string>(),
                Price: _fixture.Create<decimal>(),
                ItemId: _fixture.Create<Guid>(),
                ProductId: _fixture.Create<Guid>()
            ),
            new ItemAddedEvent(
                CartId: cartId,
                Description: _fixture.Create<string>(),
                Image: _fixture.Create<string>(),
                Price: _fixture.Create<decimal>(),
                ItemId: _fixture.Create<Guid>(),
                ProductId: _fixture.Create<Guid>()
            )
        ];

        var command = new AddItemCommand(
            CartId: cartId,
            Description: _fixture.Create<string>(),
            Image: _fixture.Create<string>(),
            Price: _fixture.Create<decimal>(),
            TotalPrice: _fixture.Create<decimal>(),
            ItemId: _fixture.Create<Guid>(),
            ProductId: _fixture.Create<Guid>()
        );

        await CommandValidator
            .Setup(_eventStore, cartId.ToString())
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then<CartException>();
    }
}
