// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace.Sbase;
using Newtonsoft.Json.Linq;
using railyWebIndex.Controller;

namespace TestWorkflows
{
//     public class UnitTest1
//     {
//         [Fact]
//         public void TestPaymentCpaturePendingParsing()
//         {
//             var jsonInput = @"
// {
//     ""id"": ""WH-0K680662TW794710M-45G79326RT2032937"",
//     ""event_version"": ""1.0"",
//     ""create_time"": ""2025-03-30T19:30:11.125Z"",
//     ""resource_type"": ""capture"",
//     ""resource_version"": ""2.0"",
//     ""event_type"": ""PAYMENT.CAPTURE.PENDING"",
//     ""summary"": ""Payment pending for € 0.49 EUR"",
//     ""resource"": {
//         ""id"": ""97158969C4116803J"",
//         ""amount"": {
//             ""currency_code"": ""EUR"",
//             ""value"": ""0.49""
//         },
//         ""final_capture"": true,
//         ""seller_protection"": {
//             ""status"": ""ELIGIBLE"",
//             ""dispute_categories"": [
//                 ""ITEM_NOT_RECEIVED"",
//                 ""UNAUTHORIZED_TRANSACTION""
//             ]
//         },
//         ""seller_receivable_breakdown"": {
//             ""gross_amount"": {
//                 ""currency_code"": ""EUR"",
//                 ""value"": ""0.49""
//             },
//             ""paypal_fee"": {
//                 ""currency_code"": ""EUR"",
//                 ""value"": ""0.40""
//             },
//             ""net_amount"": {
//                 ""currency_code"": ""EUR"",
//                 ""value"": ""0.09""
//             }
//         },
//         ""custom_id"": ""66b886cc-222b-4446-b1ce-39ca0c8615a4;one_time;7d"",
//         ""status"": ""COMPLETED"",
//         ""supplementary_data"": {
//             ""related_ids"": {
//                 ""order_id"": ""0CJ8208487401531V""
//             }
//         },
//         ""payee"": {
//             ""email_address"": ""mail@riesolution.de"",
//             ""merchant_id"": ""CUZ2684DC3ZGG""
//         },
//         ""create_time"": ""2025-03-30T19:30:06Z"",
//         ""update_time"": ""2025-03-30T19:30:06Z"",
//         ""links"": [
//             {
//                 ""href"": ""https://api.paypal.com/v2/payments/captures/97158969C4116803J"",
//                 ""rel"": ""self"",
//                 ""method"": ""GET""
//             },
//             {
//                 ""href"": ""https://api.paypal.com/v2/payments/captures/97158969C4116803J/refund"",
//                 ""rel"": ""refund"",
//                 ""method"": ""POST""
//             },
//             {
//                 ""href"": ""https://api.paypal.com/v2/checkout/orders/0CJ8208487401531V"",
//                 ""rel"": ""up"",
//                 ""method"": ""GET""
//             }
//         ]
//     },
//     ""links"": [
//         {
//             ""href"": ""https://api.paypal.com/v1/notifications/webhooks-events/WH-0K680662TW794710M-45G79326RT2032937"",
//             ""rel"": ""self"",
//             ""method"": ""GET""
//         },
//         {
//             ""href"": ""https://api.paypal.com/v1/notifications/webhooks-events/WH-0K680662TW794710M-45G79326RT2032937/resend"",
//             ""rel"": ""resend"",
//             ""method"": ""POST""
//         }
//     ]
// }
// ";

//             var rootObj = JObject.Parse(jsonInput);
//             var data = PayPalWebhookData.Parse(rootObj);

//             var payInstance = new Payment
//             {
//                 UserId = data.Booking.CustomerUserId,
//                 PaymentType = data.Booking.Kind,
//                 PayedMonth = data.Booking.Month,
//                 PayedDays = data.Booking.Days,

//                 PayPalTransactionId = data.TransactionId,
//                 Amount = data.AmountTotal,
//                 Currency = data.AmountCurrency,
//                 CreatedAt = data.CreateTime,
//                 Status = data.State,
//                 BillingAgreementId = data.BillingAgreementId
//             };

//             // Überprüfungen
//             Assert.Equal("66b886cc-222b-4446-b1ce-39ca0c8615a4", payInstance.UserId.ToString("D"));
//             Assert.Equal("one_time", payInstance.PaymentType);
//             Assert.Equal(7, payInstance.PayedDays);
//             Assert.Equal(-1, payInstance.PayedMonth);
//             Assert.Equal(string.Empty, payInstance.BillingAgreementId);
//             Assert.Equal("97158969C4116803J", payInstance.PayPalTransactionId);
//             Assert.Equal(0.49m, payInstance.Amount);
//             Assert.Equal("EUR", payInstance.Currency);
//             Assert.Equal("COMPLETED", payInstance.Status);
//             Assert.Equal(new DateTime(2025, 3, 30, 19, 30, 11, DateTimeKind.Utc), payInstance.CreatedAt);

//         }
//     }
}