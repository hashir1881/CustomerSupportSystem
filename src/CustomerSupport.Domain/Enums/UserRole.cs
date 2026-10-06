namespace CustomerSupport.Domain.Enums;

/// <summary>
/// Represents the role of a user in the customer support system.
/// Server-enforced; never client-assigned.
/// </summary>
public enum UserRole
{
    Customer = 1,
    CustomerService = 2,
    Administrator = 3
}
