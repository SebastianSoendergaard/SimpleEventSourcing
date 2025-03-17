# Understanding Eventsourcing

A C# implementation of the examples from the book: [**Understanding Eventsourcing** - Planning and Implementing scalable Systems with Eventmodeling and Eventsourcing](https://leanpub.com/eventmodeling-and-eventsourcing). The original code examples can be found here: [Github Examples](https://github.com/dilgerma/eventsourcing-book)

While I was playing around trying to implement my own event store just for fun and learning, Martin Dilgers book was released. Going through the book I found that I was actually rigth in many of my idears and concepts. So to test my event store I decided to try implement all the examples from the book. During the implementation minor changes and new concepts was needed which was a fun challange. But by now almost all the examples from the book can be handled nicely. Compared to the original Kotlin/Axon examples I have tried to use as little magic as possible making it very clear what is going on. The hope is that this repository can help demystify Event Sourcing and prove that is is not really that complex afterall.

To run the project, start by running docker-compose.

The actual implementation is located in the Cart and Cart.Tests folders.

In the Doc folder a [draw.io](https://www.drawio.com/) Event Modeling diagram can be found. 
