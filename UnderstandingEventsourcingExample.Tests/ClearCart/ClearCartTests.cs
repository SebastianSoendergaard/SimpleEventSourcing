using AutoFixture;
using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventStore.InMemory;
using UnderstandingEventsourcingExample.Cart.ClearCart;
using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Tests.ClearCart;

public class ClearCartTests
{
    private readonly Fixture _fixture = new Fixture();
    private IEventStore _eventStore;
    private CartRepository _repository;
    private ClearCartCommandHandler _handler;

    public ClearCartTests()
    {
        _eventStore = new InMemoryEventStore();
        _repository = new CartRepository(_eventStore);
        _handler = new ClearCartCommandHandler(_repository);
    }

    [Fact]
    public async Task CanClearCart()
    {
        var cartId = Guid.NewGuid();

        List<IDomainEvent> givenEvents =
        [
            new CartCreatedEvent(cartId),
            new ItemAddedEvent(
                cartId,
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<decimal>(),
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>()
            )
        ];

        var command = new ClearCartCommand(cartId);

        List<IDomainEvent> expectedEvents =
        [
            new CartClearedEvent(cartId)
        ];

        await CommandValidator
            .Setup(_eventStore, cartId)
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then(expectedEvents);
    }
}
