using AutoFixture;
using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventStore.InMemory;
using UnderstandingEventsourcingExample.Cart.Domain;
using UnderstandingEventsourcingExample.Cart.Domain.EventUpcast;
using UnderstandingEventsourcingExample.Cart.GetCartItems;

namespace UnderstandingEventsourcingExample.Tests.GetCartItems;

public class GetCartItemsTests
{
    private readonly Fixture _fixture = new Fixture();
    private IEventStore _eventStore;
    private GetCartItemsQueryHandler _handler;

    public GetCartItemsTests()
    {
        _eventStore = new InMemoryEventStore();
        _eventStore.RegisterUpcaster(new ItemAddedEventUpcaster());
        _handler = new GetCartItemsQueryHandler(_eventStore);
    }

    [Fact]
    [Obsolete]
    public async Task ReturnsAllAddedItems()
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
            ),
            new ItemAddedEvent(
                cartId,
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<decimal>(),
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>()
            )
        ];

        var query = new GetCartItemsQuery(cartId);

        await QueryValidator
            .Setup(_eventStore, cartId)
            .Given(givenEvents)
            .Then<CartItemsReadModel>(
                async () => await _handler.Handle(query),
                x =>
                {
                    Assert.Equal(2, x.Items.Count());
                });
    }

    [Fact]
    public async Task FailsIfNoItemsAdded()
    {
        var cartId = Guid.NewGuid();

        List<IDomainEvent> givenEvents = [];

        var query = new GetCartItemsQuery(cartId);

        await QueryValidator
            .Setup(_eventStore, cartId)
            .Given(givenEvents)
            .Then<CartException>(async () => await _handler.Handle(query));
    }

}
