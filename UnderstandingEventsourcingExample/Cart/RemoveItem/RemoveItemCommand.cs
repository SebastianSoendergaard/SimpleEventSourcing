﻿using Basses.SimpleEventStore.Enablers;
using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.AddItem;

public record RemoveItemCommand(
    Guid CartId,
    Guid ItemId
);

public class RemoveItemCommandHandler(EventSourcedRepository<CartAggregate> repository)
{
    public async Task Handle(RemoveItemCommand command)
    {
        var cart = await repository.TryGet(command.CartId);
        if (cart == null)
        {
            throw new CartException("Cart does not exist");
        }

        cart.RemoveItem(command.ItemId);
        await repository.Update(cart);
    }
}
