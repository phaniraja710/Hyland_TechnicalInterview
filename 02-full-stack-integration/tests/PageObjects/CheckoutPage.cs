using Microsoft.Playwright;
using System.Globalization;

namespace EcommerceTests.PageObjects
{
    public class CheckoutPage
    {
        private readonly IPage _page;

        private ILocator PromoCodeInput   => _page.Locator("#promo-code");
        private ILocator ApplyPromoButton => _page.Locator("#apply-promo");
        private ILocator OriginalPrice    => _page.Locator(".original-price");
        private ILocator DiscountAmount   => _page.Locator(".discount-amount");
        private ILocator FinalPrice       => _page.Locator(".final-price");
        private ILocator PlaceOrderButton => _page.Locator("#place-order");
        private ILocator OrderNumber      => _page.Locator(".order-number");
        private ILocator SuccessMessage   => _page.Locator(".success-message");
        private ILocator ErrorMessage     => _page.Locator(".error-message");

        public CheckoutPage(IPage page)
        {
            _page = page;
        }

        public async Task NavigateAsync()
        {
            await _page.GotoAsync("http://localhost:8080");
            await OriginalPrice.WaitForAsync(new LocatorWaitForOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 15_000
            });
        }

        public async Task ApplyPromoCodeAsync(string code)
        {
            await PromoCodeInput.ClearAsync();
            await PromoCodeInput.FillAsync(code);
            await ApplyPromoButton.ClickAsync();

            // Wait until either success (discount shown) or error message appears
            await _page.WaitForFunctionAsync(@"() => {
                const err  = document.querySelector('.error-message');
                const disc = document.querySelector('.discount-amount');
                return (err  && !err.classList.contains('hidden'))
                    || (disc && !disc.classList.contains('hidden'));
            }", null, new PageWaitForFunctionOptions { Timeout = 15_000 });
        }

        public async Task<decimal> GetOriginalPriceAsync()
        {
            return await ParsePriceAsync(OriginalPrice);
        }

        public async Task<decimal> GetDiscountAmountAsync()
        {
            return await ParsePriceAsync(DiscountAmount);
        }

        public async Task<decimal> GetFinalPriceAsync()
        {
            return await ParsePriceAsync(FinalPrice);
        }

        public async Task VerifyDiscountApplied(decimal expectedDiscount)
        {
            await DiscountAmount.WaitForAsync(new LocatorWaitForOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 15_000
            });

            var displayedDiscount = await GetDiscountAmountAsync();
            var originalPrice     = await GetOriginalPriceAsync();
            var finalPrice        = await GetFinalPriceAsync();

            NUnit.Framework.Assert.That(displayedDiscount,
                NUnit.Framework.Is.EqualTo(expectedDiscount).Within(0.01m),
                $"Discount shown on UI {displayedDiscount} does not match expected {expectedDiscount}");

            var computedFinal = originalPrice - displayedDiscount;
            NUnit.Framework.Assert.That(finalPrice,
                NUnit.Framework.Is.EqualTo(computedFinal).Within(0.01m),
                $"Final price {finalPrice} should be {originalPrice} - {displayedDiscount}");
        }

        public async Task<string> PlaceOrderAsync()
        {
            await PlaceOrderButton.ClickAsync();
            await OrderNumber.WaitForAsync(new LocatorWaitForOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 30_000
            });
            return (await OrderNumber.TextContentAsync())?.Trim()
                   ?? throw new InvalidOperationException("Order number was empty.");
        }

        public async Task<bool> IsErrorDisplayedAsync()
        {
            try
            {
                await ErrorMessage.WaitForAsync(new LocatorWaitForOptions
                {
                    State   = WaitForSelectorState.Visible,
                    Timeout = 5_000
                });
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        public async Task<string> GetErrorMessageAsync()
        {
            await ErrorMessage.WaitForAsync(new LocatorWaitForOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 5_000
            });
            return (await ErrorMessage.TextContentAsync())?.Trim() ?? string.Empty;
        }

        
        private static async Task<decimal> ParsePriceAsync(ILocator locator)
        {
            var text = await locator.TextContentAsync() ?? string.Empty;
            var cleaned = text.Replace("$", "")
                              .Replace(",", "")
                              .Replace("−", "")
                              .Replace("-", "")
                              .Trim();
            return decimal.Parse(cleaned, CultureInfo.InvariantCulture);
        }
    }
}