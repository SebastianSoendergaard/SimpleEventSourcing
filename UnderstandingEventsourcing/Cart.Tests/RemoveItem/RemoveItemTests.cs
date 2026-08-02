using AutoFixture;
using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventStore.InMemory;
using UnderstandingEventsourcingExample.Cart.Domain;
using UnderstandingEventsourcingExample.Cart.Domain.EventUpcast;
using UnderstandingEventsourcingExample.Cart.RemoveItem;

namespace UnderstandingEventsourcingExample.Tests.RemoveItem;

public class RemoveItemTests
{
    private readonly Fixture _fixture = new Fixture();
    private IEventStore _eventStore;
    private CartRepository _repository;
    private RemoveItemCommandHandler _handler;

    public RemoveItemTests()
    {
        _eventStore = new InMemoryEventStore();
        _eventStore.RegisterUpcaster(new ItemAddedEventUpcaster());
        _repository = new CartRepository(_eventStore);
        _handler = new RemoveItemCommandHandler(_repository);
    }

    [Fact]
    [Obsolete]
    public async Task CanRemoveItem()
    {
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        List<IDomainEvent> givenEvents =
        [
            new CartCreatedEvent(cartId),
            new ItemAddedEvent(
                CartId: cartId,
                Description: _fixture.Create<string>(),
                Image: _fixture.Create<string>(),
                Price: _fixture.Create<decimal>(),
                ItemId: itemId,
                ProductId: _fixture.Create<Guid>()
            )
        ];

        var command = new RemoveItemCommand(
            CartId: cartId,
            ItemId: itemId
        );

        List<IDomainEvent> expectedEvents =
        [
            new ItemRemovedEvent(CartId: cartId, ItemId: itemId)
        ];

        await CommandValidator
            .Setup(_eventStore, cartId.ToString())
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then(expectedEvents);
    }

    [Fact]
    [Obsolete]
    public async Task FailsWhenItemAlreadyRemoved()
    {
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        List<IDomainEvent> givenEvents =
        [
            new CartCreatedEvent(cartId),
            new ItemAddedEvent(
                CartId: cartId,
                Description: _fixture.Create<string>(),
                Image: _fixture.Create<string>(),
                Price: _fixture.Create<decimal>(),
                ItemId: itemId,
                ProductId: _fixture.Create<Guid>()
            ),
            new ItemRemovedEvent(CartId: cartId, ItemId: itemId)
        ];

        var command = new RemoveItemCommand(
            CartId: cartId,
            ItemId: itemId
        );

        await CommandValidator
            .Setup(_eventStore, cartId.ToString())
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then<CartException>();
    }
}
