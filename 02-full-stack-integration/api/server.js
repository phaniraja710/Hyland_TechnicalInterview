const express = require('express');
const cors = require('cors');
const bodyParser = require('body-parser');
const { Pool } = require('pg');

const app = express();
const PORT = process.env.PORT || 3000;

// Database connection
const pool = new Pool({
  host: process.env.DB_HOST || 'postgres',
  port: process.env.DB_PORT || 5432,
  database: process.env.DB_NAME || 'testshop',
  user: process.env.DB_USER || 'testuser',
  password: process.env.DB_PASSWORD || 'testpass',
});

// Middleware
app.use(cors());
app.use(bodyParser.json());

// Health check
app.get('/health', async (req, res) => {
  try {
    await pool.query('SELECT 1');
    res.json({ status: 'healthy', database: 'connected' });
  } catch (error) {
    res.status(503).json({ status: 'unhealthy', error: error.message });
  }
});

// ============================================
// PROMOTIONS ENDPOINTS
// ============================================

// Create promotion
app.post('/admin/promotions', async (req, res) => {
  const {
    code,
    discountType,
    discountValue,
    category,
    maxUses = 100,
    validFrom,
    validUntil
  } = req.body;

  // Validation
  if (!code || !discountType || !discountValue || !category || !validFrom || !validUntil) {
    return res.status(400).json({
      error: 'Missing required fields',
      required: ['code', 'discountType', 'discountValue', 'category', 'validFrom', 'validUntil']
    });
  }

  try {
    // Check if code already exists
    const existing = await pool.query('SELECT code FROM promotions WHERE code = $1', [code]);
    if (existing.rows.length > 0) {
      return res.status(409).json({ error: 'Promotion code already exists' });
    }

    const promotionId = `promo_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

    const result = await pool.query(
      `INSERT INTO promotions
       (promotion_id, code, discount_type, discount_value, category, max_uses, valid_from, valid_until, status)
       VALUES ($1, $2, $3, $4, $5, $6, $7, $8, 'ACTIVE')
       RETURNING *`,
      [promotionId, code, discountType, discountValue, category, maxUses, validFrom, validUntil]
    );

    res.status(201).json({
      promotionId: result.rows[0].promotion_id,
      code: result.rows[0].code,
      status: result.rows[0].status
    });
  } catch (error) {
    console.error('Error creating promotion:', error);
    res.status(500).json({ error: 'Failed to create promotion', details: error.message });
  }
});

// Get promotion by ID
app.get('/admin/promotions/:id', async (req, res) => {
  const { id } = req.params;

  try {
    const result = await pool.query('SELECT * FROM promotions WHERE promotion_id = $1', [id]);

    if (result.rows.length === 0) {
      return res.status(404).json({ error: 'Promotion not found' });
    }

    res.json(result.rows[0]);
  } catch (error) {
    console.error('Error fetching promotion:', error);
    res.status(500).json({ error: 'Failed to fetch promotion', details: error.message });
  }
});

// Get promotion by code
app.get('/promotions/code/:code', async (req, res) => {
  const { code } = req.params;

  try {
    const result = await pool.query('SELECT * FROM promotions WHERE code = $1', [code]);

    if (result.rows.length === 0) {
      return res.status(404).json({ error: 'Promotion not found' });
    }

    const promo = result.rows[0];
    const now = new Date();
    const validFrom = new Date(promo.valid_from);
    const validUntil = new Date(promo.valid_until);

    // Check if expired
    if (now < validFrom || now > validUntil) {
      return res.status(400).json({
        error: 'Promotion code is expired or not yet valid',
        validFrom: promo.valid_from,
        validUntil: promo.valid_until
      });
    }

    // Check if max uses exceeded
    if (promo.used_count >= promo.max_uses) {
      return res.status(400).json({ error: 'Promotion code has reached maximum uses' });
    }

    res.json(promo);
  } catch (error) {
    console.error('Error fetching promotion by code:', error);
    res.status(500).json({ error: 'Failed to fetch promotion', details: error.message });
  }
});

// Delete promotion
app.delete('/admin/promotions/:id', async (req, res) => {
  const { id } = req.params;

  try {
    const result = await pool.query('DELETE FROM promotions WHERE promotion_id = $1 RETURNING *', [id]);

    if (result.rows.length === 0) {
      return res.status(404).json({ error: 'Promotion not found' });
    }

    res.json({ message: 'Promotion deleted successfully', promotionId: id });
  } catch (error) {
    console.error('Error deleting promotion:', error);
    res.status(500).json({ error: 'Failed to delete promotion', details: error.message });
  }
});

// ============================================
// PRODUCTS ENDPOINTS
// ============================================

app.get('/products', async (req, res) => {
  try {
    const result = await pool.query('SELECT * FROM products ORDER BY product_id');
    res.json(result.rows);
  } catch (error) {
    console.error('Error fetching products:', error);
    res.status(500).json({ error: 'Failed to fetch products', details: error.message });
  }
});

app.get('/products/:id', async (req, res) => {
  const { id } = req.params;

  try {
    const result = await pool.query('SELECT * FROM products WHERE product_id = $1', [id]);

    if (result.rows.length === 0) {
      return res.status(404).json({ error: 'Product not found' });
    }

    res.json(result.rows[0]);
  } catch (error) {
    console.error('Error fetching product:', error);
    res.status(500).json({ error: 'Failed to fetch product', details: error.message });
  }
});

// ============================================
// ORDERS ENDPOINTS
// ============================================

app.post('/orders', async (req, res) => {
  const {
    customerEmail,
    productId,
    promotionCode
  } = req.body;

  if (!customerEmail || !productId) {
    return res.status(400).json({
      error: 'Missing required fields',
      required: ['customerEmail', 'productId']
    });
  }

  const client = await pool.connect();

  try {
    await client.query('BEGIN');

    // Get product
    const productResult = await client.query('SELECT * FROM products WHERE product_id = $1', [productId]);
    if (productResult.rows.length === 0) {
      await client.query('ROLLBACK');
      return res.status(404).json({ error: 'Product not found' });
    }

    const product = productResult.rows[0];
    let originalAmount = parseFloat(product.price);
    let discountAmount = 0;
    let promotionId = null;

    // Apply promotion if provided
    if (promotionCode) {
      const promoResult = await client.query('SELECT * FROM promotions WHERE code = $1', [promotionCode]);

      if (promoResult.rows.length === 0) {
        await client.query('ROLLBACK');
        return res.status(400).json({ error: 'Invalid promotion code' });
      }

      const promo = promoResult.rows[0];
      const now = new Date();
      const validFrom = new Date(promo.valid_from);
      const validUntil = new Date(promo.valid_until);

      // Validate promotion
      if (now < validFrom || now > validUntil) {
        await client.query('ROLLBACK');
        return res.status(400).json({ error: 'Promotion code is expired or not yet valid' });
      }

      if (promo.used_count >= promo.max_uses) {
        await client.query('ROLLBACK');
        return res.status(400).json({ error: 'Promotion code has reached maximum uses' });
      }

      if (promo.category !== product.category) {
        await client.query('ROLLBACK');
        return res.status(400).json({
          error: 'Promotion code is not valid for this product category',
          promotionCategory: promo.category,
          productCategory: product.category
        });
      }

      // Calculate discount
      if (promo.discount_type === 'PERCENTAGE') {
        discountAmount = (originalAmount * promo.discount_value) / 100;
      } else {
        discountAmount = promo.discount_value;
      }

      promotionId = promo.promotion_id;

      // Update promotion used count
      await client.query(
        'UPDATE promotions SET used_count = used_count + 1 WHERE promotion_id = $1',
        [promotionId]
      );
    }

    const finalAmount = originalAmount - discountAmount;
    const orderId = `ORD-${Date.now()}-${Math.random().toString(36).substr(2, 9).toUpperCase()}`;

    // Create order
    const orderResult = await client.query(
      `INSERT INTO orders
       (order_id, customer_email, original_amount, discount_amount, final_amount, promotion_code, status)
       VALUES ($1, $2, $3, $4, $5, $6, 'COMPLETED')
       RETURNING *`,
      [orderId, customerEmail, originalAmount, discountAmount, finalAmount, promotionCode]
    );

    // Create audit log if promotion was used
    if (promotionId) {
      await client.query(
        `INSERT INTO promotion_audit_log (promotion_id, order_id, discount_applied)
         VALUES ($1, $2, $3)`,
        [promotionId, orderId, discountAmount]
      );
    }

    await client.query('COMMIT');

    res.status(201).json({
      orderId: orderResult.rows[0].order_id,
      originalAmount,
      discountAmount,
      finalAmount,
      status: 'COMPLETED'
    });

  } catch (error) {
    await client.query('ROLLBACK');
    console.error('Error creating order:', error);
    res.status(500).json({ error: 'Failed to create order', details: error.message });
  } finally {
    client.release();
  }
});

app.get('/orders/:orderId', async (req, res) => {
  const { orderId } = req.params;

  try {
    const result = await pool.query('SELECT * FROM orders WHERE order_id = $1', [orderId]);

    if (result.rows.length === 0) {
      return res.status(404).json({ error: 'Order not found' });
    }

    res.json(result.rows[0]);
  } catch (error) {
    console.error('Error fetching order:', error);
    res.status(500).json({ error: 'Failed to fetch order', details: error.message });
  }
});

// ============================================
// ADMIN/TESTING ENDPOINTS
// ============================================

// Reset database (for testing)
app.post('/admin/reset', async (req, res) => {
  try {
    await pool.query('DELETE FROM promotion_audit_log');
    await pool.query('DELETE FROM orders');
    await pool.query('DELETE FROM promotions');

    res.json({ message: 'Database reset successfully' });
  } catch (error) {
    console.error('Error resetting database:', error);
    res.status(500).json({ error: 'Failed to reset database', details: error.message });
  }
});

// Start server
app.listen(PORT, '0.0.0.0', () => {
  console.log(`API Server running on port ${PORT}`);
  console.log(`Health check: http://localhost:${PORT}/health`);
});

// Graceful shutdown
process.on('SIGTERM', async () => {
  console.log('SIGTERM received, closing database pool...');
  await pool.end();
  process.exit(0);
});
