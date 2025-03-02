using AutoFixture;
using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventStore.InMemory;
using UnderstandingEventsourcingExample.Cart.AddItem;
using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Tests.AddItem;

public class AddItemTests
{
    private readonly Fixture _fixture = new Fixture();
    private IEventStore _eventStore;
    private CartRepository _repository;
    private AddItemCommandHandler _handler;

    public AddItemTests()
    {
        _eventStore = new InMemoryEventStore();
        _repository = new CartRepository(_eventStore);
        _handler = new AddItemCommandHandler(_repository);
    }

    [Fact]
    public async Task CanAddItem()
    {
        var cartId = Guid.NewGuid();

        List<IDomainEvent> givenEvents = [];

        var command = new AddItemCommand(
            cartId,
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<decimal>(),
            _fixture.Create<decimal>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>()
        );

        List<IDomainEvent> expectedEvents =
        [
            new CartCreatedEvent(cartId),
            new ItemAddedEvent(
                command.CartId,
                command.Description,
                command.Image,
                command.Price,
                command.ItemId,
                command.ProductId
            )
        ];

        await CommandValidator
            .Setup(_eventStore, cartId)
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then(expectedEvents);
    }

    [Fact]
    public async Task CanAddAdditionalItem()
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

        var command = new AddItemCommand(
            cartId,
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<decimal>(),
            _fixture.Create<decimal>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>()
        );

        List<IDomainEvent> expectedEvents =
        [
            new ItemAddedEvent(
                command.CartId,
                command.Description,
                command.Image,
                command.Price,
                command.ItemId,
                command.ProductId
            )
        ];

        await CommandValidator
            .Setup(_eventStore, cartId)
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then(expectedEvents);
    }

    [Fact]
    public async Task FailsWhenMoreThat3ItemsAreAdded()
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

        var command = new AddItemCommand(
            cartId,
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<decimal>(),
            _fixture.Create<decimal>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>()
        );

        await CommandValidator
            .Setup(_eventStore, cartId)
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then<CartException>();
    }
}
