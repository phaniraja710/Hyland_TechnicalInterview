# Promotion Management API

Node.js/Express API with PostgreSQL for integration testing.

## Endpoints

### Promotions

#### Create Promotion
```
POST /admin/promotions
Content-Type: application/json

{
  "code": "SPRING25",
  "discountType": "PERCENTAGE",
  "discountValue": 25,
  "category": "ELECTRONICS",
  "maxUses": 100,
  "validFrom": "2026-05-01T00:00:00Z",
  "validUntil": "2026-05-31T23:59:59Z"
}

Response 201:
{
  "promotionId": "promo_1234567890_abc123",
  "code": "SPRING25",
  "status": "ACTIVE"
}
```

#### Get Promotion by ID
```
GET /admin/promotions/{promotionId}

Response 200:
{
  "promotion_id": "promo_1234567890_abc123",
  "code": "SPRING25",
  "discount_type": "PERCENTAGE",
  "discount_value": 25,
  "category": "ELECTRONICS",
  "max_uses": 100,
  "used_count": 0,
  "valid_from": "2026-05-01T00:00:00Z",
  "valid_until": "2026-05-31T23:59:59Z",
  "status": "ACTIVE",
  "created_at": "2026-05-20T10:00:00Z"
}
```

#### Get Promotion by Code (for validation)
```
GET /promotions/code/{code}

Response 200: (same as above)
Response 400: Expired or invalid
Response 404: Not found
```

#### Delete Promotion
```
DELETE /admin/promotions/{promotionId}

Response 200:
{
  "message": "Promotion deleted successfully",
  "promotionId": "promo_1234567890_abc123"
}
```

### Products

#### Get All Products
```
GET /products

Response 200:
[
  {
    "product_id": "prod_laptop_001",
    "name": "Professional Laptop",
    "price": 1000.00,
    "category": "ELECTRONICS",
    "stock": 50
  }
]
```

#### Get Product by ID
```
GET /products/{productId}
```

### Orders

#### Create Order
```
POST /orders
Content-Type: application/json

{
  "customerEmail": "customer@example.com",
  "productId": "prod_laptop_001",
  "promotionCode": "SPRING25"  // optional
}

Response 201:
{
  "orderId": "ORD-1234567890-ABC123",
  "originalAmount": 1000.00,
  "discountAmount": 250.00,
  "finalAmount": 750.00,
  "status": "COMPLETED"
}
```

#### Get Order by ID
```
GET /orders/{orderId}

Response 200:
{
  "order_id": "ORD-1234567890-ABC123",
  "customer_email": "customer@example.com",
  "original_amount": 1000.00,
  "discount_amount": 250.00,
  "final_amount": 750.00,
  "promotion_code": "SPRING25",
  "status": "COMPLETED",
  "created_at": "2026-05-20T10:00:00Z"
}
```

### Admin/Testing

#### Reset Database
```
POST /admin/reset

Response 200:
{
  "message": "Database reset successfully"
}
```

Deletes all orders, audit logs, and promotions. Use in test teardown for idempotent tests.

#### Health Check
```
GET /health

Response 200:
{
  "status": "healthy",
  "database": "connected"
}
```

## Error Responses

### 400 Bad Request
```json
{
  "error": "Promotion code is expired or not yet valid"
}
```

### 404 Not Found
```json
{
  "error": "Promotion not found"
}
```

### 409 Conflict
```json
{
  "error": "Promotion code already exists"
}
```

### 500 Internal Server Error
```json
{
  "error": "Failed to create promotion",
  "details": "..."
}
```

## Validation Rules

### Promotions
- Code must be unique
- Expired promotions return 400 error
- Used count cannot exceed max_uses
- Category must match product category

### Orders
- Promotion code validated at order time
- Discount calculated based on type (PERCENTAGE or FIXED)
- Audit log created automatically
- Transaction ensures data consistency
