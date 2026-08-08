-- Initialize test database schema

-- Promotions table
CREATE TABLE IF NOT EXISTS promotions (
    promotion_id VARCHAR(50) PRIMARY KEY,
    code VARCHAR(50) UNIQUE NOT NULL,
    discount_type VARCHAR(20) NOT NULL CHECK (discount_type IN ('PERCENTAGE', 'FIXED')),
    discount_value DECIMAL(10,2) NOT NULL,
    category VARCHAR(50) NOT NULL,
    max_uses INTEGER DEFAULT 100,
    used_count INTEGER DEFAULT 0,
    valid_from TIMESTAMP NOT NULL,
    valid_until TIMESTAMP NOT NULL,
    status VARCHAR(20) DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE', 'INACTIVE', 'EXPIRED')),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_promotions_code ON promotions(code);
CREATE INDEX idx_promotions_status ON promotions(status);

-- Products table
CREATE TABLE IF NOT EXISTS products (
    product_id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    category VARCHAR(50) NOT NULL,
    stock INTEGER DEFAULT 0
);

-- Insert sample products
INSERT INTO products (product_id, name, price, category, stock)
VALUES
    ('prod_laptop_001', 'Professional Laptop', 1000.00, 'ELECTRONICS', 50),
    ('prod_book_001', 'Test Automation Book', 45.00, 'BOOKS', 100);

-- Orders table
CREATE TABLE IF NOT EXISTS orders (
    order_id VARCHAR(50) PRIMARY KEY,
    customer_email VARCHAR(255) NOT NULL,
    original_amount DECIMAL(10,2) NOT NULL,
    discount_amount DECIMAL(10,2) DEFAULT 0.00,
    final_amount DECIMAL(10,2) NOT NULL,
    promotion_code VARCHAR(50),
    status VARCHAR(20) DEFAULT 'PENDING',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS promotion_audit_log (
    audit_id SERIAL PRIMARY KEY,
    promotion_id VARCHAR(50) NOT NULL,
    order_id VARCHAR(50) NOT NULL,
    discount_applied DECIMAL(10,2) NOT NULL,
    used_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (order_id) REFERENCES orders(order_id) ON DELETE CASCADE
);

CREATE INDEX idx_orders_promotion_code ON orders(promotion_code);
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_audit_promotion_id ON promotion_audit_log(promotion_id);
CREATE INDEX idx_audit_order_id ON promotion_audit_log(order_id);

-- Create a view for easy querying
CREATE VIEW order_summary AS
SELECT
    o.order_id,
    o.customer_email,
    o.original_amount,
    o.discount_amount,
    o.final_amount,
    o.promotion_code,
    o.status,
    o.created_at,
    pal.audit_id,
    pal.promotion_id
FROM orders o
LEFT JOIN promotion_audit_log pal ON o.order_id = pal.order_id;
