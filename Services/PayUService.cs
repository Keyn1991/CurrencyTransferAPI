// Services/PayUService.cs
using CurrencyTransferAPI.DTOs; // <--- UPEWNIJ SIĘ, ŻE TA LINIA JEST OBECNA I POPRAWNA
using CurrencyTransferAPI.Data;
using CurrencyTransferAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq; // Może być potrzebne, jeśli będziesz rozbudowywać logikę
using System.Threading.Tasks;

namespace CurrencyTransferAPI.Services
{
    public class PayUService : IPayUService
    {
        private readonly ILogger<PayUService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IAccountService _accountService;

        private static readonly Dictionary<string, PaymentStatusDto> _mockPaymentOrders = new Dictionary<string, PaymentStatusDto>();

        public PayUService(ILogger<PayUService> logger, ApplicationDbContext context, IAccountService accountService)
        {
            _logger = logger;
            _context = context;
            _accountService = accountService;
        }

        public async Task<CreatePaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto request)
        {
            _logger.LogInformation("PayUService: Creating mock payment for AccountId: {AccountId}, Amount: {Amount} {CurrencyCode}",
                request.AccountId, request.Amount, request.CurrencyCode);

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId);
            if (account == null)
            {
                _logger.LogWarning("PayUService: Account {AccountId} not found for payment creation.", request.AccountId);
                return new CreatePaymentResponseDto { Success = false, ErrorMessage = "Account not found." };
            }

            if (account.CurrencyCode != request.CurrencyCode)
            {
                _logger.LogWarning("PayUService: Payment currency {PaymentCurrency} does not match account currency {AccountCurrency} for AccountId {AccountId}.",
                    request.CurrencyCode, account.CurrencyCode, request.AccountId);
                return new CreatePaymentResponseDto { Success = false, ErrorMessage = "Payment currency must match account currency." };
            }

            var orderId = "mock-payu-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            var redirectUri = $"{request.ContinueUrl}?orderId={orderId}&status=PENDING";

            _mockPaymentOrders[orderId] = new PaymentStatusDto
            {
                AccountId = request.AccountId,
                OrderId = orderId,
                Status = "PENDING",
                Amount = request.Amount,
                CurrencyCode = request.CurrencyCode
            };

            _logger.LogInformation("PayUService: Mock payment created. OrderId: {OrderId}, AccountId: {AccountId}, RedirectUri: {RedirectUri}",
                orderId, request.AccountId, redirectUri);

            return new CreatePaymentResponseDto
            {
                Success = true,
                OrderId = orderId,
                RedirectUri = redirectUri
            };
        }

        public async Task<PaymentStatusDto> GetPaymentStatusAsync(string orderId)
        {
            _logger.LogInformation("PayUService: Getting status for mock OrderId: {OrderId}", orderId);
            if (_mockPaymentOrders.TryGetValue(orderId, out var payment))
            {
                if (payment.Status == "PENDING")
                {
                    payment.Status = "COMPLETED";
                    _logger.LogInformation("PayUService: Mock OrderId {OrderId} status changed to COMPLETED.", orderId);

                    var accountToCredit = await _context.Accounts
                        .FirstOrDefaultAsync(a => a.Id == payment.AccountId); // Używamy payment.AccountId

                    if (accountToCredit != null)
                    {
                        if (accountToCredit.CurrencyCode == payment.CurrencyCode)
                        {
                            var depositResult = await _accountService.DepositAsync(
                                accountToCredit.Id,
                                accountToCredit.UserId,
                                payment.Amount,
                                $"PayU Mock Deposit, OrderId: {orderId}"
                            );

                            if (depositResult.Success)
                            {
                               _logger.LogInformation("PayUService: Account {AccountId} (User: {UserId}) credited with {Amount} {CurrencyCode} for OrderId {OrderId}.",
                                   accountToCredit.Id, accountToCredit.UserId, payment.Amount, payment.CurrencyCode, orderId);
                            }
                            else
                            {
                                _logger.LogError("PayUService: Failed to credit Account {AccountId} for OrderId {OrderId}. Reason: {Reason}",
                                   accountToCredit.Id, orderId, depositResult.ErrorMessage);
                                payment.Status = "FAILED_CREDIT";
                            }
                        }
                        else
                        {
                            _logger.LogError("PayUService: Currency mismatch for crediting Account {AccountId}. Payment currency: {PaymentCurrency}, Account currency: {AccountCurrency}. OrderId: {OrderId}",
                                accountToCredit.Id, payment.CurrencyCode, accountToCredit.CurrencyCode, orderId);
                            payment.Status = "FAILED_CURRENCY_MISMATCH";
                        }
                    }
                    else
                    {
                         _logger.LogError("PayUService: Could not find Account {AccountId} (associated with OrderId {OrderId}) to credit.",
                            payment.AccountId, orderId);
                         payment.Status = "FAILED_NO_ACCOUNT";
                    }
                }
                return payment;
            }

            _logger.LogWarning("PayUService: Mock OrderId {OrderId} not found in _mockPaymentOrders.", orderId);
            return new PaymentStatusDto { OrderId = orderId, Status = "NOT_FOUND" };
        }
    }
}