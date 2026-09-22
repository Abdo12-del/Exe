namespace SalesManagement.Domain.Enums;

public enum MovementType
{
    Purchase = 1,
    Sale = 2,
    SaleReturn = 3,
    PurchaseReturn = 4,
    TransferIn = 5,
    TransferOut = 6,
    InventoryAdjustment = 7,
    DamageWaste = 8,
    InitialBalance = 9
}

public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    BankTransfer = 3,
    Cheque = 4,
    Credit = 5,
    Mixed = 6
}

public enum PaymentStatus
{
    Paid = 1,
    Partial = 2,
    Unpaid = 3
}

public enum RefundMethod
{
    Cash = 1,
    CustomerCreditDeduction = 2,
    SupplierDebtDeduction = 3,
    BankTransfer = 4
}

public enum CashTransactionType
{
    OpeningFloat = 1,
    SaleReceipt = 2,
    SaleRefund = 3,
    CustomerDebtPayment = 4,
    SupplierDebtPayment = 5,
    ExpensePayment = 6,
    CashInDeposit = 7,
    CashOutWithdrawal = 8,
    ClosingBalance = 9
}

public enum InventoryCountStatus
{
    Draft = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}
