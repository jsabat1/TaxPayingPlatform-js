1. Value objects catalog

|ubiquitous language|component attributes and types|self validation rules|example domain operation|
|:---|:---|:---|:---|
|EncodedBankingQrCode|MicroAccountNumber (string), Amount(decimal), TaxAuthorityCode(string), GeneratedAt(DateTime)| Amount must be > 0, MicroAccountNumber must be 26 digits, TaxAuthorityCode cannot be empty.| public EncodedBankingQrCode WithUpdatedTimestamp(DateTimeOffset newTime) => new(MicroAccountNumber, Amount, TaxAuthorityCode, newTime)|
|TaxAmount|Value (decimal), Currency (string)| Value must be >=0, Currency must be a valid ISO code| public TaxAmount Add(TaxAmount other) => new(Value + other.Value, Currency)|
|MicroAccountNumber| Value (string)| Cannot be empty, must be exactly 26 digits, modulo 97 checksum validation| public string FormatWithSpaces() => //returning a new string formatted with spaces// |
|NotificationPreferences| ChannelType Primary, ChannelType Fallback| Primary cant be the same as Fallback (cannot fallback from email to email)| public NotificationPreferences UpdateFallback(ChannelType newFallback)|

2. Strongly typed indentifiers

* 'TaxPaymentInstructionID' instead of 'Guid InstructionId" and 'Guid ID' in the TaxPaymentInstruction aggregate root
* 'ClientId' replacing the raw Guid ClientId representing the taxpayer
* 'BankTransferAttemptId' instead of Guid AttemptId in the BankTransferAttempt child entity
* 'NotificationId' replacing the raw Guid for tracking dispatches in the Multichannel Notification context
* 'MonthlyTaxSettlementId' replaces the Guid MonthlyTaxSettlementId, linking the payment to the settlement

