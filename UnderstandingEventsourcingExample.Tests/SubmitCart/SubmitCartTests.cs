using AutoFixture;
using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventStore.InMemory;
using UnderstandingEventsourcingExample.Cart.ClearCart;
using UnderstandingEventsourcingExample.Cart.Domain;
using UnderstandingEventsourcingExample.Cart.Domain.EventUpcast;

namespace UnderstandingEventsourcingExample.Tests.SubmitCartTests;

public class SubmitCartTests
{
    private readonly Fixture _fixture = new Fixture();
    private IEventStore _eventStore;
    private CartRepository _repository;
    private SubmitCartCommandHandler _handler;

    public SubmitCartTests()
    {
        _eventStore = new InMemoryEventStore();
        _eventStore.RegisterUpcaster(new ItemAddedEventUpcaster());
        _repository = new CartRepository(_eventStore);
        _handler = new SubmitCartCommandHandler(_repository);
    }

    [Fact]
    public async Task CanSubmitCart()
    {
        var cartId = Guid.NewGuid();
        var productId1 = _fixture.Create<Guid>();
        var price1 = _fixture.Create<decimal>();
        var productId2 = _fixture.Create<Guid>();
        var price2 = _fixture.Create<decimal>();

        List<IDomainEvent> givenEvents =
        [
            new CartCreatedEvent(cartId),
            new ItemAddedEventV2(
                cartId,
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                price1,
                _fixture.Create<Guid>(),
                productId1,
                _fixture.Create<Guid>().ToString()
            ),
            new ItemAddedEventV2(
                cartId,
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                price2,
                _fixture.Create<Guid>(),
                productId2,
                _fixture.Create<Guid>().ToString()
            )
        ];

        var command = new SubmitCartCommand(cartId);

        List<IDomainEvent> expectedEvents =
        [
            new CartSubmittedEvent(
                cartId,
                [
                    new OrderedProduct(productId1, price1),
                    new OrderedProduct(productId2, price2)
                ],
                price1 + price2
            )
        ];

        await CommandValidator
            .Setup(_eventStore, cartId.ToString())
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then(expectedEvents, (e, a) =>
            {
                var expected = (CartSubmittedEvent)e;
                var actual = (CartSubmittedEvent)a;
                Assert.Equal(expected.CartId, actual.CartId);
                Assert.Equal(expected.OrderedProducts, actual.OrderedProducts);
                Assert.Equal(expected.TotalPrice, actual.TotalPrice);
            });
    }

    [Fact]
    public async Task FailsWhenCartSubmittedTwice()
    {
        var cartId = Guid.NewGuid();

        List<IDomainEvent> givenEvents =
        [
            new CartCreatedEvent(cartId),
            new ItemAddedEventV2(
                cartId,
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<decimal>(),
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>().ToString()
            ),
            new CartSubmittedEvent(cartId, [], _fixture.Create<decimal>())
        ];

        var command = new SubmitCartCommand(cartId);

        await CommandValidator
            .Setup(_eventStore, cartId.ToString())
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then<CartException>();
    }

    [Fact]
    public async Task FailsWhenSubmittingEmptyCart()
    {
        var cartId = Guid.NewGuid();

        List<IDomainEvent> givenEvents =
        [
            new CartCreatedEvent(cartId),
            new ItemAddedEventV2(
                cartId,
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<decimal>(),
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>().ToString()
            ),
            new CartClearedEvent(cartId)
        ];

        var command = new SubmitCartCommand(cartId);

        await CommandValidator
            .Setup(_eventStore, cartId.ToString())
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then<CartException>();
    }

    [Fact]
    public async Task FailsWhenSubmittingUnknownCart()
    {
        var cartId = Guid.NewGuid();

        List<IDomainEvent> givenEvents = [];

        var command = new SubmitCartCommand(cartId);

        await CommandValidator
            .Setup(_eventStore, cartId.ToString())
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then<CartException>();
    }
}
