# Task: Full-Stack Integration Testing (API → UI → Database)

## Scenario

E-commerce platform: Marketing creates discount codes via API → Customers apply them at checkout → Transactions stored in PostgreSQL.

**Test flow:** API creates "SPRING25" (25% off Electronics) → UI applies discount → Database verifies order and audit log.

## What You Need to Implement

The project provides skeleton classes with `NotImplementedException()`. Complete the following:

### 1. API Client (`tests/helpers/ApiClient.cs`)
- `CreatePromotionAsync()` - POST /promotions
- `GetPromotionAsync()` - GET /promotions/{id}
- `DeletePromotionAsync()` - DELETE /promotions/{id}

### 2. Database Helper (`tests/helpers/DatabaseHelper.cs`)
- `Connect()` - PostgreSQL connection
- `GetOrderById()` - Query orders table
- `GetAuditLogByOrderId()` - Query audit log
- `DeleteOrder()` - Cleanup
- `VerifyOrderTotals()` - Verify amounts

### 3. Page Object (`tests/PageObjects/CheckoutPage.cs`)
- `NavigateAsync()` - Go to checkout
- `ApplyPromoCodeAsync()` - Apply discount code
- `GetOriginalPriceAsync()` - Read original price
- `GetDiscountAmountAsync()` - Read discount
- `GetFinalPriceAsync()` - Read final price
- `VerifyDiscountApplied()` - Assert discount correct
- `PlaceOrderAsync()` - Complete order
- `IsErrorDisplayedAsync()` - Check errors
- `GetErrorMessageAsync()` - Read error text

### 4. Tests (`tests/PromotionFlowTests.cs`)
- `TestFullPromotionFlowHappyPath()` - Complete API → UI → DB flow
- `TestInvalidPromoCode()` - Invalid code rejection
- `TestExpiredPromoCode()` - Expired code rejection
- `TestWrongCategoryPromo()` - Category mismatch rejection

## API Endpoints

**POST /admin/promotions** - Create promo
```json
{
  "code": string,
  "discountType": "PERCENTAGE" | "FIXED",
  "discountValue": number,
  "category": "ELECTRONICS" | "BOOKS" | ...,
  "maxUses": number,
  "validFrom": ISO8601 datetime,
  "validUntil": ISO8601 datetime
}
→ {"promotionId": string, "code": string, "status": string}
```

**POST /orders** - Create order
```json
{
  "customerEmail": string,
  "productId": string,
  "promotionCode": string
}
→ {"orderId": string, "originalAmount": decimal, "discountAmount": decimal, "finalAmount": decimal}
```

**Other:**
- GET /admin/promotions/{id}, GET /promotions/code/{code}, DELETE /admin/promotions/{id}
- POST /admin/reset (cleanup)

## Services

- API: http://localhost:3000 (health: `/health`)
- UI: http://localhost:8080
- PostgreSQL: localhost:5432 (db: `testshop`, user: `testuser`, pass: `testpass`)

## Requirements

- Complete all methods marked with `NotImplementedException`
- All 4 tests must pass
- **Tests must be idempotent** (can run multiple times without conflicts)
  - Use `POST /admin/reset` in test setup/teardown
  - Or use unique promotion codes per test run
  - Or delete created promotions in cleanup
- Proper cleanup in TearDown
- No hardcoded values (use configuration)
- Explicit waits (no Thread.Sleep)
