using AutoFixture;
using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventStore.InMemory;
using UnderstandingEventsourcingExample.Cart.Domain;
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
        _repository = new CartRepository(_eventStore);
        _handler = new RemoveItemCommandHandler(_repository);
    }

    [Fact]
    public async Task CanRemoveItem()
    {
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        List<IDomainEvent> givenEvents =
        [
            new CartCreatedEvent(cartId),
            new ItemAddedEvent(
                cartId,
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<decimal>(),
                itemId,
                _fixture.Create<Guid>()
            )
        ];

        var command = new RemoveItemCommand(
            cartId,
            itemId
        );

        List<IDomainEvent> expectedEvents =
        [
            new ItemRemovedEvent(cartId, itemId)
        ];

        await CommandValidator
            .Setup(_eventStore, cartId)
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then(expectedEvents);
    }

    [Fact]
    public async Task FailsWhenItemAlreadyRemoved()
    {
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        List<IDomainEvent> givenEvents =
        [
            new CartCreatedEvent(cartId),
            new ItemAddedEvent(
                cartId,
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<decimal>(),
                itemId,
                _fixture.Create<Guid>()
            ),
            new ItemRemovedEvent(cartId, itemId)
        ];

        var command = new RemoveItemCommand(
            cartId,
            itemId
        );

        await CommandValidator
            .Setup(_eventStore, cartId)
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then<CartException>();
    }
}
