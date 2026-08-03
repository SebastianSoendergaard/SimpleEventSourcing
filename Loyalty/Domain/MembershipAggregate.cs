using Basses.SimpleEventStore.Enablers;
using UnderstandingEventsourcingExample.Framework;

namespace UnderstandingEventsourcingExample.Loyalty.Domain;

internal class MembershipAggregate : PiiAggregate<MemberInformation>,
    IDomainEventHandler<MembershipRegisteredEvent>,
    IDomainEventHandler<MembershipConfirmedEvent>,
    IDomainEventHandler<MembershipCanceledEvent>,
    IDomainEventHandler<MembershipTransferRequestedEvent>,
    IDomainEventHandler<MembershipTransferConfirmedEvent>
{
    private Guid MemberInformationId => PiiDataId;
    private MemberInformation? MemberInformation => PiiData;

    private Guid _confirmationId = Guid.Empty;
    private bool _isConfirmed = false;
    private bool _isCanceled = false;
    private Guid _newMembershipId = Guid.Empty;
    private Guid _transferConfirmationId = Guid.Empty;

    public MembershipAggregate(IEnumerable<IDomainEvent> events) : base(events) { }

    public MembershipAggregate(MembershipId id, string phoneNumber)
    {
        UpdatePiiData(new MemberInformation(phoneNumber));
        Apply(new MembershipRegisteredEvent(id.Value, Guid.NewGuid(), Guid.NewGuid()));
    }

    public void UpdateMemberInformation(string name, string email)
    {
        UpdatePiiData(MemberInformation?.WithNameAndEmail(name, email));
        Apply(new MemberNameUpdatedEvent(new Guid(Id), MemberInformationId));
        Apply(new MemberEmailUpdatedEvent(new Guid(Id), MemberInformationId));
    }

    public void Confirm(Guid confirmationId)
    {
        if (!_isConfirmed && !_isCanceled && _confirmationId == confirmationId)
        {
            Apply(new MembershipConfirmedEvent(new Guid(Id), MemberInformationId));
        }
    }

    public void Cancel()
    {
        if (!_isCanceled)
        {
            ClearPiiData();
            Apply(new MembershipCanceledEvent(new Guid(Id), MemberInformationId));
        }
    }

    public void ReRegister(string phoneNumber)
    {
        if (_isCanceled)
        {
            UpdatePiiData(new MemberInformation(phoneNumber));
            Apply(new MembershipRegisteredEvent(new Guid(Id), Guid.NewGuid(), Guid.NewGuid()));
        }
    }

    public void RequestTransferTo(MembershipId newMembershipId, string phoneNumber)
    {
        if (_isConfirmed)
        {
            UpdatePiiData(MemberInformation?.WithPhoneNumberToTransferTo(phoneNumber));
            Apply(new MembershipTransferRequestedEvent(new Guid(Id), newMembershipId.Value, Guid.NewGuid()));
        }
    }

    public MembershipAggregate? ConfirmTransfer(Guid confirmationId)
    {
        if (_transferConfirmationId != confirmationId)
        {
            return null;
        }

        if (string.IsNullOrEmpty(MemberInformation?.TransferToPhoneNumber))
        {
            return null;
        }

        Apply(new MembershipTransferConfirmedEvent(new Guid(Id), _newMembershipId, confirmationId));
        Apply(new MembershipCanceledEvent(new Guid(Id), MemberInformationId)); // not sure we should cancel here!

        return new MembershipAggregate(MembershipId.FromId(_newMembershipId), MemberInformation.TransferToPhoneNumber);
    }

    public void On(MembershipRegisteredEvent @event)
    {
        Id = @event.MembershipId.ToString();
        SetPiiDataId(@event.MemberInformationId);
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
        _isConfirmed = true;
    }
}
