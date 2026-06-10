using Basses.SimpleEventStore.Enablers;

namespace UnderstandingEventsourcingExample.Loyalty.Domain;

internal class MembershipAggregate : Aggregate,
    IDomainEventHandler<MembershipRegisteredEvent>,
    IDomainEventHandler<MembershipConfirmedEvent>,
    IDomainEventHandler<MembershipCanceledEvent>,
    IDomainEventHandler<MembershipTransferRequestedEvent>,
    IDomainEventHandler<MembershipTransferConfirmedEvent>
{
    public MemberInformation? Info { get; private set; }

    private Guid _memberInformationId = Guid.Empty;
    private Guid _confirmationId = Guid.Empty;
    private bool _isConfirmed = false;
    private bool _isCanceled = false;
    private Guid _newMembershipId = Guid.Empty;
    private Guid _transferConfirmationId = Guid.Empty;

    public MembershipAggregate(IEnumerable<IDomainEvent> events) : base(events) { }

    public MembershipAggregate(MembershipId id, string phoneNumber)
    {
        Info = new MemberInformation(phoneNumber);
        Apply(new MembershipRegisteredEvent(id.Value, Info.Id, Guid.NewGuid()));
    }

    public void Confirm(Guid confirmationId)
    {
        if (!_isConfirmed && !_isCanceled && _confirmationId == confirmationId)
        {
            Apply(new MembershipConfirmedEvent(new Guid(Id), _memberInformationId));
        }
    }

    public void Cancel()
    {
        if (!_isCanceled)
        {
            Apply(new MembershipCanceledEvent(new Guid(Id), _memberInformationId));
        }
    }

    public void ReRegister(string phoneNumber)
    {
        if (_isCanceled)
        {
            Info = new MemberInformation(phoneNumber);
            Apply(new MembershipRegisteredEvent(new Guid(Id), Info.Id, Guid.NewGuid()));
        }
    }

    public void RequestTransferTo(MembershipId newMembershipId, string phoneNumber)
    {
        if (_isConfirmed)
        {
            // TODO: how to store new phone number until actual transfer
            Info.SetPhoneNumberToTransferTo(phoneNumber);
            Apply(new MembershipTransferRequestedEvent(new Guid(Id), newMembershipId.Value, Guid.NewGuid()));
        }
    }

    public MembershipAggregate? ConfirmTransfer(Guid confirmationId)
    {
        if (_transferConfirmationId != confirmationId)
        {
            return null;
        }

        // TODO: how to get new phone number
        Apply(new MembershipTransferConfirmedEvent(new Guid(Id), _newMembershipId, Guid.NewGuid()));
        Apply(new MembershipCanceledEvent(new Guid(Id), Guid.Empty));
        return new MembershipAggregate(MembershipId.FromId(_newMembershipId), "");
    }

    public void On(MembershipRegisteredEvent @event)
    {
        Id = @event.MembershipId.ToString();
        _memberInformationId = @event.MemberInformationId;
        _confirmationId = @event.ConfirmationId;
    }

    public void On(MembershipConfirmedEvent @event)
    {
        _isConfirmed = true;
    }

    public void On(MembershipCanceledEvent @event)
    {
        _isConfirmed = false;
        _isCanceled = true;
    }

    public void On(MembershipTransferRequestedEvent @event)
    {
        _newMembershipId = @event.NewMembershipId;
        _transferConfirmationId = @event.ConfirmationId;
    }

    public void On(MembershipTransferConfirmedEvent @event)
    {

    }
}
