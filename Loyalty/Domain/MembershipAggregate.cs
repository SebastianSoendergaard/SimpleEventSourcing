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
        Apply(new MemberInformation(phoneNumber));
        Apply(new MembershipRegisteredEvent(id.Value, Guid.NewGuid(), Guid.NewGuid()));
    }

    public void UpdateMemberInformation(string name, string email)
    {
        Apply(MemberInformation?.WithNameAndEmail(name, email));
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
            ApplyClearPiiData();
            Apply(new MembershipCanceledEvent(new Guid(Id), MemberInformationId));
        }
    }

    public void ReRegister(string phoneNumber)
    {
        if (_isCanceled)
        {
            Apply(new MemberInformation(phoneNumber));
            Apply(new MembershipRegisteredEvent(new Guid(Id), Guid.NewGuid(), Guid.NewGuid()));
        }
    }

    public void RequestTransferTo(MembershipId newMembershipId, string phoneNumber)
    {
        if (_isConfirmed)
        {
            Apply(MemberInformation?.WithPhoneNumberToTransferTo(phoneNumber));
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

        Apply(new MembershipTransferConfirmedEvent(new Guid(Id), _newMembershipId, Guid.NewGuid()));
        Apply(new MembershipCanceledEvent(new Guid(Id), Guid.Empty));

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

    }
}
