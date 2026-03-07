# Attribute-Based Validation Rules

Standard validation rules that are candidates to be implemented as attributes in Neatoo.

## System.ComponentModel.DataAnnotations Attributes

These leverage existing .NET attributes that developers already know:

### String Validation
- [x] **StringLength** - Validates string length within min/max bounds
  - `[StringLength(100, MinimumLength = 5)]`
  - Error: "Name must be between 5 and 100 characters."

- [x] **MinLength** - Validates minimum length of strings or collections
  - `[MinLength(3)]`
  - Error: "Tags must have at least 3 items."

- [x] **MaxLength** - Validates maximum length of strings or collections
  - `[MaxLength(50)]`
  - Error: "Description cannot exceed 50 characters."

- [x] **RegularExpression** - Validates against a regex pattern
  - `[RegularExpression(@"^[A-Z]{2}\d{4}$")]`
  - Error: "Code must be 2 uppercase letters followed by 4 digits."

### Numeric Validation
- [x] **Range** - Validates numeric values fall within a range
  - `[Range(1, 100)]` or `[Range(typeof(decimal), "0.01", "999.99")]`
  - Error: "Quantity must be between 1 and 100."

### Format Validation
- [x] **EmailAddress** - Validates email format
  - `[EmailAddress]`
  - Error: "Invalid email address."

- [ ] **Phone** - Validates phone number format
  - `[Phone]`
  - Error: "Invalid phone number."

- [ ] **Url** - Validates URL format
  - `[Url]`
  - Error: "Invalid URL."

- [ ] **CreditCard** - Validates credit card number format (Luhn algorithm)
  - `[CreditCard]`
  - Error: "Invalid credit card number."

### Type Validation
- [ ] **EnumDataType** - Validates value is a valid enum member
  - `[EnumDataType(typeof(OrderStatus))]`
  - Error: "Invalid order status."

## Custom Neatoo Attributes (New)

These would be Neatoo-specific attributes for common validation patterns:

### Numeric Constraints
- [ ] **Positive** - Value must be greater than zero
  - `[Positive]`
  - Error: "Amount must be positive."

- [ ] **NonNegative** - Value must be zero or greater
  - `[NonNegative]`
  - Error: "Quantity cannot be negative."

- [ ] **Negative** - Value must be less than zero
  - `[Negative]`
  - Error: "Adjustment must be negative."

### Date/Time Constraints
- [ ] **FutureDate** - Date must be in the future
  - `[FutureDate]`
  - Error: "Appointment date must be in the future."

- [ ] **PastDate** - Date must be in the past
  - `[PastDate]`
  - Error: "Birth date must be in the past."

- [ ] **DateRange** - Date must fall within a range
  - `[DateRange("2020-01-01", "2030-12-31")]`
  - Error: "Date must be between 2020 and 2030."

### Collection Constraints
- [ ] **NotEmpty** - Collection must have at least one item
  - `[NotEmpty]`
  - Error: "Order must have at least one line item."

- [ ] **UniqueItems** - Collection items must be unique
  - `[UniqueItems]`
  - Error: "Tags must be unique."

- [ ] **CountRange** - Collection count within range
  - `[CountRange(1, 10)]`
  - Error: "Must have between 1 and 10 items."

### String Format Constraints
- [ ] **AlphaNumeric** - Only letters and numbers
  - `[AlphaNumeric]`
  - Error: "Username must be alphanumeric."

- [ ] **NoWhitespace** - No whitespace allowed
  - `[NoWhitespace]`
  - Error: "Code cannot contain spaces."

- [ ] **Uppercase** / **Lowercase** - Enforce case
  - `[Uppercase]`
  - Error: "Country code must be uppercase."

### Domain-Specific
- [ ] **Guid** - Must be a valid non-empty GUID
  - `[Guid]`
  - Error: "Invalid identifier."

- [ ] **Currency** - Valid currency code (ISO 4217)
  - `[Currency]`
  - Error: "Invalid currency code."

- [ ] **CountryCode** - Valid ISO country code
  - `[CountryCode]`
  - Error: "Invalid country code."

## Implementation Priority

**High Priority** (most commonly used):
1. ~~StringLength~~ (Done)
2. ~~Range~~ (Done)
3. ~~EmailAddress~~ (Done)
4. ~~RegularExpression~~ (Done)

**Medium Priority** (useful additions):
1. ~~MinLength / MaxLength~~ (Done)
2. Positive / NonNegative
3. FutureDate / PastDate
4. NotEmpty

**Lower Priority** (specialized use cases):
1. Phone / Url / CreditCard
2. UniqueItems / CountRange
3. AlphaNumeric / NoWhitespace
4. Currency / CountryCode
