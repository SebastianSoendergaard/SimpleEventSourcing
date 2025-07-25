using AutoFixture;
using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventStore.InMemory;
using UnderstandingEventsourcingExample.Cart.ChangeInventory;
using UnderstandingEventsourcingExample.Cart.Domain;
using UnderstandingEventsourcingExample.Cart.Domain.EventUpcast;

namespace UnderstandingEventsourcingExample.Tests.ChangeInventory;

public class ChangeInventoryTests
{
    private readonly Fixture _fixture = new Fixture();
    private IEventStore _eventStore;
    private InventoryRepository _repository;
    private ChangeInventoryCommandHandler _handler;

    public ChangeInventoryTests()
    {
        _eventStore = new InMemoryEventStore();
        _eventStore.RegisterUpcaster(new ItemAddedEventUpcaster());
        _repository = new InventoryRepository(_eventStore);
        _handler = new ChangeInventoryCommandHandler(_repository);
    }

    [Fact]
    public async Task CanChangeInventory()
    {
        var productId = Guid.NewGuid();
        var inventory = _fixture.Create<int>();

        List<IDomainEvent> givenEvents = [];

        var command = new ChangeInventoryCommand(productId, inventory);

        List<IDomainEvent> expectedEvents =
        [
            new InventoryChangedEvent(productId, inventory)
        ];

        await CommandValidator
            .Setup(_eventStore, InventoryAggregate.CreateInventoryIdFromGuid(productId))
            .Given(givenEvents)
            .When(async () => await _handler.Handle(command))
            .Then(expectedEvents);
    }
}
