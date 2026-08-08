using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using Npgsql;
using System.Net.Http;
using System.Text.Json;
using EcommerceTests.Helpers;
using EcommerceTests.PageObjects;

namespace EcommerceTests.Integration
{
    [TestFixture]
    public class PromotionFlowTests : PageTest
    {
        private ApiClient _apiClient;
        private DatabaseHelper _dbHelper;
        private string _testPromotionId;
        private string _testOrderId;

        [SetUp]
        public void Setup()
        {
            _apiClient = new ApiClient("http://localhost:3000");
            _dbHelper = new DatabaseHelper("localhost", 5432, "testshop", "testuser", "testpass");
            _dbHelper.Connect();
            _testPromotionId = null;
            _testOrderId     = null;
        }

        [TearDown]
        public async Task Cleanup()
        {
            if (_testOrderId != null)
            {
                try { _dbHelper.DeleteOrder(_testOrderId); } catch { }
            }
            if (_testPromotionId != null)
            {
                try { await _apiClient.DeletePromotionAsync(_testPromotionId); } catch { }
            }
            _dbHelper.Disconnect();
        }

        [Test]
        public async Task TestFullPromotionFlowHappyPath()
        {
            
            var promo = await _apiClient.CreatePromotionAsync(new
         {
             code          = "TESTDEAL25",
             discountType  = "PERCENTAGE",
             discountValue = 25,
             category    = "ELECTRONICS",
             validFrom     = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
             validUntil    = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd")
         });
            _testPromotionId = promo.PromotionId;

            Assert.That(promo.Code,          Is.EqualTo("TESTDEAL25"), "API: code mismatch");
           
            Assert.That(promo.Status,        Is.EqualTo("ACTIVE"),     "API: status mismatch");

            
            var fetched = await _apiClient.GetPromotionAsync(_testPromotionId);
           
            var checkoutPage = new CheckoutPage(Page);
            await checkoutPage.NavigateAsync();
            await checkoutPage.ApplyPromoCodeAsync("TESTDEAL25");
 
            var originalPrice = await checkoutPage.GetOriginalPriceAsync();
            var discountAmt   = originalPrice * 0.25m;
            var finalPrice    = originalPrice - discountAmt;

            await checkoutPage.VerifyDiscountApplied(discountAmt);

            
            _testOrderId = await checkoutPage.PlaceOrderAsync();
            Assert.That(_testOrderId, Is.Not.Null.And.Not.Empty, "No order ID returned");

            
            var order = _dbHelper.GetOrderById(_testOrderId);
            Assert.That(order.PromotionCode, Is.EqualTo("TESTDEAL25"), "DB: promo code mismatch");
            Assert.That(order.Status,        Is.EqualTo("COMPLETED"),  "DB: status mismatch");
            Assert.That(
                _dbHelper.VerifyOrderTotals(_testOrderId, originalPrice, discountAmt, finalPrice),
                Is.True, "DB: order totals mismatch");

            
            var audit = _dbHelper.GetAuditLogByOrderId(_testOrderId);
            Assert.That(audit.PromotionId,     Is.EqualTo(_testPromotionId),       "Audit: promo ID mismatch");
            Assert.That(audit.DiscountApplied, Is.EqualTo(discountAmt).Within(0.01m), "Audit: discount mismatch");
        }

        [Test]
        public async Task TestInvalidPromoCode()
        {
            var checkoutPage = new CheckoutPage(Page);
            await checkoutPage.NavigateAsync();
            await checkoutPage.ApplyPromoCodeAsync("DOESNOTEXIST999");

            Assert.That(await checkoutPage.IsErrorDisplayedAsync(), Is.True,
                "Expected error message for invalid code");

            var originalPrice = await checkoutPage.GetOriginalPriceAsync();
            var finalPrice    = await checkoutPage.GetFinalPriceAsync();
            Assert.That(finalPrice, Is.EqualTo(originalPrice).Within(0.01m),
                "Price should not change for invalid code");
        }

        [Test]
        public async Task TestExpiredPromoCode()
        {
            
            var promo = await _apiClient.CreatePromotionAsync(new
         {
          code          = "EXPIREDCODE",
          discountType  = "PERCENTAGE",
          discountValue = 20,
         category      = "ELECTRONICS",
         validFrom     = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"),
          validUntil    = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")
         });
            _testPromotionId = promo.PromotionId;

            var checkoutPage = new CheckoutPage(Page);
            await checkoutPage.NavigateAsync();
            await checkoutPage.ApplyPromoCodeAsync("EXPIREDCODE");

            Assert.That(await checkoutPage.IsErrorDisplayedAsync(), Is.True,
                "Expected error message for expired code");

            var errorText = await checkoutPage.GetErrorMessageAsync();
            Assert.That(errorText.ToLower(), Does.Contain("expired"),
                "Error should mention code is expired");

            var originalPrice = await checkoutPage.GetOriginalPriceAsync();
            var finalPrice    = await checkoutPage.GetFinalPriceAsync();
            Assert.That(finalPrice, Is.EqualTo(originalPrice).Within(0.01m),
                "Price should not change for expired code");
        }

        [Test]
        public async Task TestWrongCategoryPromo()
        {
            
          var promo = await _apiClient.CreatePromotionAsync(new
         {
          code          = "BOOKDEAL10",
          discountType  = "PERCENTAGE",
          discountValue = 10,
          category      = "BOOKS",
          validFrom     = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
          validUntil    = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd")
         });
            _testPromotionId = promo.PromotionId;

            var checkoutPage = new CheckoutPage(Page);
            await checkoutPage.NavigateAsync();
            await checkoutPage.ApplyPromoCodeAsync("BOOKDEAL10");

            Assert.That(await checkoutPage.IsErrorDisplayedAsync(), Is.True,
                "Expected error for wrong category promo");

            var originalPrice = await checkoutPage.GetOriginalPriceAsync();
            var finalPrice    = await checkoutPage.GetFinalPriceAsync();
            Assert.That(finalPrice, Is.EqualTo(originalPrice).Within(0.01m),
                "Price should not change for wrong category code");
        }
    }
}