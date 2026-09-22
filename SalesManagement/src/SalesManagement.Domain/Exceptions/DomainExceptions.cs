namespace SalesManagement.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class InsufficientStockException : DomainException
{
    public int ProductId { get; }
    public string ProductName { get; }
    public decimal RequestedQuantity { get; }
    public decimal AvailableQuantity { get; }

    public InsufficientStockException(int productId, string productName, decimal requested, decimal available)
        : base($"المخزون غير كافٍ للمنتج '{productName}'. المطلوب: {requested}، المتاح حالياً: {available}.")
    {
        ProductId = productId;
        ProductName = productName;
        RequestedQuantity = requested;
        AvailableQuantity = available;
    }
}

public class CreditLimitExceededException : DomainException
{
    public int CustomerId { get; }
    public string CustomerName { get; }
    public decimal CurrentDebt { get; }
    public decimal CreditLimit { get; }
    public decimal AdditionalDebt { get; }

    public CreditLimitExceededException(int customerId, string customerName, decimal currentDebt, decimal creditLimit, decimal additionalDebt)
        : base($"تجاوز سقف الائتمان للزبون '{customerName}'. الدين الحالي: {currentDebt:N2} دج، سقف الائتمان: {creditLimit:N2} دج، الدين الجديد المطلوب: {additionalDebt:N2} دج.")
    {
        CustomerId = customerId;
        CustomerName = customerName;
        CurrentDebt = currentDebt;
        CreditLimit = creditLimit;
        AdditionalDebt = additionalDebt;
    }
}

public class ClosedCashRegisterException : DomainException
{
    public int CashRegisterId { get; }

    public ClosedCashRegisterException(int cashRegisterId)
        : base($"صندوق الكاشير رقم {cashRegisterId} مغلق حالياً. يرجى فتح الصندوق وتحديد رصيد البداية أولاً.")
    {
        CashRegisterId = cashRegisterId;
    }
}
