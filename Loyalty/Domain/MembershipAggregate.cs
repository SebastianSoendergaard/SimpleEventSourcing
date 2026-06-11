using Basses.SimpleEventStore.Enablers;

namespace UnderstandingEventsourcingExample.Loyalty.Domain;

internal class MembershipAggregate : Aggregate,
    IDomainEventHandler<MembershipRegisteredEvent>,
    IDomainEventHandler<MembershipConfirmedEvent>,
    IDomainEventHandler<MembershipCanceledEvent>,
    IDomainEventHandler<MembershipTransferRequestedEvent>,
    IDomainEventHandler<MembershipTransferConfirmedEvent>
{
    public MemberInformation? UncommitedMemberInformation { get; private set; }
    public Guid MemberInformationId { get; private set; }

    private MemberInformation? _memberInformation;
    private Guid _confirmationId = Guid.Empty;
    private bool _isConfirmed = false;
    private bool _isCanceled = false;
    private Guid _newMembershipId = Guid.Empty;
    private Guid _transferConfirmationId = Guid.Empty;

    public MembershipAggregate(IEnumerable<IDomainEvent> events) : base(events) { }

    public MembershipAggregate(MembershipId id, string phoneNumber)
    {
        UncommitedMemberInformation = new MemberInformation(phoneNumber);
        Apply(new MembershipRegisteredEvent(id.Value, UncommitedMemberInformation.Id, Guid.NewGuid()));
    }

    public void SetMemberInformation(MemberInformation memberInformation)
    {
        if (_memberInformation != MemberInformation.Empty)
        {
            throw new LoyaltyException("Can only be called once, when aggregate is loaded");
        }

        _memberInformation = memberInformation;
    }

    public void UpdateMemberInformation(string name, string email)
    {
        if (_memberInformation != null)
        {
            _memberInformation.UpdateInfo(name, email);
            UncommitedMemberInformation = _memberInformation;
        }
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
            Apply(new MembershipCanceledEvent(new Guid(Id), MemberInformationId));
        }
    }

    public void ReRegister(string phoneNumber)
    {
        if (_isCanceled)
        {
            UncommitedMemberInformation = new MemberInformation(phoneNumber);
            Apply(new MembershipRegisteredEvent(new Guid(Id), UncommitedMemberInformation.Id, Guid.NewGuid()));
        }
    }

    public void RequestTransferTo(MembershipId newMembershipId, string phoneNumber)
    {
        if (_isConfirmed)
        {
            if (_memberInformation != null)
            {
                _memberInformation.SetPhoneNumberToTransferTo(phoneNumber);
                UncommitedMemberInformation = _memberInformation;
            }
            Apply(new MembershipTransferRequestedEvent(new Guid(Id), newMembershipId.Value, Guid.NewGuid()));
        }
    }

    public MembershipAggregate? ConfirmTransfer(Guid confirmationId)
    {
        if (_transferConfirmationId != confirmationId)
        {
            return null;
        }

        if (_memberInformation == null || string.IsNullOrEmpty(_memberInformation.TransferToPhoneNumber))
        {
            return null;
        }

        Apply(new MembershipTransferConfirmedEvent(new Guid(Id), _newMembershipId, Guid.NewGuid()));
        Apply(new MembershipCanceledEvent(new Guid(Id), Guid.Empty));
        return new MembershipAggregate(MembershipId.FromId(_newMembershipId), _memberInformation.TransferToPhoneNumber);
    }

    public void On(MembershipRegisteredEvent @event)
    {
        Id = @event.MembershipId.ToString();
        MemberInformationId = @event.MemberInformationId;
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
