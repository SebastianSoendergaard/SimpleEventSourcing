﻿using UnderstandingEventsourcingExample.Cart.Domain;

namespace UnderstandingEventsourcingExample.Cart.AddItem;

public record AddItemCommand(
    Guid CartId,
    string Description,
    string Image,
    decimal Price,
    decimal TotalPrice,
    Guid ItemId,
    Guid ProductId
);

public class AddItemCommandHandler(CartRepository repository, IDeviceFingerPrintCalculator fingerPrintCalculator)
{
    public async Task Handle(AddItemCommand command)
    {
        var cart = await repository.TryGet(command.CartId.ToString());
        if (cart == null)
        {
            cart = new CartAggregate(command.CartId);
            await repository.Add(cart);
        }

        cart.AddItem(command.Description, command.Image, command.Price, command.ItemId, command.ProductId, fingerPrintCalculator);
        await repository.Update(cart);
    }
}
