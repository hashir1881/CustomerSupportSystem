namespace CustomerSupport.Domain.Enums;

/// <summary>
/// Represents the strongly typed lifecycle states of a Support Request / Complaint.
/// </summary>
public enum RequestStatus
{
    Created = 1,
    Pending = 2,
    Claimed = 3,
    InProgress = 4,
    WaitingForCustomer = 5,
    Resolved = 6,
    Closed = 7,
    Cancelled = 8
}
