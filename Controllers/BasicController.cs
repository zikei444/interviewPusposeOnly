using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Task1.Controllers
{

    [ApiController]
    [Route("api/submittrxmessage")]
    public class SubmitTrxMessageController : ControllerBase
    {
        // Model
        public class SubmitTrxMessage
        {
            public string PartnerKey { get; set; }
            public string PartnerRefNo { get; set; }
            public string PartnerPassword { get; set; }
            public long TotalAmount { get; set; }
            public List<Item> Items { get; set; }
            public string Timestamp { get; set; }
            public string Sig { get; set; }

        }

        public class Item
        {
            public string PartnerItemRef { get; set; }
            public string Name { get; set; }
            public int Qty { get; set; }
            public long UnitPrice { get; set; }
        }


        [HttpPost]
        public IActionResult SubmitTrxRequest([FromBody] SubmitTrxMessage message)
        {
            // Missing validation
            if (message == null)
                return Fail("Message is Required.");  // Question 2

            if (string.IsNullOrEmpty(message.PartnerKey))
                return Fail("partnerkey is Required.");  // Question 2

            if (string.IsNullOrEmpty(message.PartnerRefNo))
                return Fail("partnerrefno is Required.");  // Question 2

            if (string.IsNullOrEmpty(message.PartnerPassword))
                return Fail("partnerpassword is Required.");  // Question 2

            if (string.IsNullOrEmpty(message.Timestamp))
                return Fail("timestamp is Required.");  // Question 2

            // Total Amount >0
            if (message.TotalAmount <= 0)
            {
                return Ok(Fail("Invalid total amount"));  // Question 2
            }

            // Check Items
            if (message.Items == null || message.Items.Count == 0)
            {
                return Ok(Fail("Items list is empty."));  // Question 2
            }

            // Decode the password
            string password = "";

            try
            {
                password = Encoding.UTF8.GetString(Convert.FromBase64String(message.PartnerPassword));
            }
            catch
            {
                return Fail("Access Denied!");  // Question 2
            } 

            // PartnerKey and PartnerPassword
            if (!(message.PartnerKey == "FAKEGOOGLE" && password == "FAKEPASSWORD1234")
                &&
                !(message.PartnerKey == "FAKEPEOPLE" && password == "FAKEPASSWORD4578"))
            {
                return Fail("Access Denied!");  // Question 2
            }


            // Validate each item and calculate total
            long calculatedTotal = 0;

            foreach (var item in message.Items)
            {
                // Missing item
                if (string.IsNullOrEmpty(item.PartnerItemRef))
                    return Fail("partneritemref is Required.");  // Question 2

                if (string.IsNullOrEmpty(item.Name))
                    return Fail("name is Required.");  // Question 2

                // Quantity validation
                if (item.Qty <= 0 || item.Qty > 5)
                    return Fail("Invalid quantity.");  // Question 2

                // Unit price validation
                if (item.UnitPrice <= 0)
                    return Fail("Invalid unit price.");  // Question 2

                // Calculate the total amount
                calculatedTotal += item.Qty * item.UnitPrice;

            }

            // Total amount validation
            if (calculatedTotal != message.TotalAmount)
                return Fail("Invalid Total Amount."); // Question 2

            // Parse timestamp as UTC
            DateTime requestTime = DateTime.Parse(
                message.Timestamp,
                null,
                System.Globalization.DateTimeStyles.AssumeUniversal |
                System.Globalization.DateTimeStyles.AdjustToUniversal
            );

            // server time (UTC)
            DateTime serverTime = DateTime.UtcNow;

            // 5 min check
            double diffMinutes = Math.Abs((serverTime - requestTime).TotalMinutes);

            if (diffMinutes > 5)
            {
                return Fail("Expired."); // Question 2
            }

            // format ONLY for signature
            string formattedTimestamp = requestTime.ToString("yyyyMMddHHmmss");

            // Combine input
            string inputSig = formattedTimestamp + message.PartnerKey + message.PartnerRefNo + calculatedTotal + message.PartnerPassword;

            // Generate SHA256 hash
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputSig));

            // Convert to hexa
            string hex = BitConverter.ToString(hash).Replace("-", "").ToLower();

            // Convert to base64
            string generatedSig = Convert.ToBase64String(Encoding.UTF8.GetBytes(hex));

            // Debug Purpose: 
            Console.WriteLine(inputSig);
            Console.WriteLine(generatedSig);

            // Validate sig
            if (string.IsNullOrEmpty(message.Sig) || generatedSig != message.Sig)
            {
                return Ok(Fail("Invalid signature"));  // Question 2
            }


            // Question 3
            long totalAmountinMYR = calculatedTotal / 100;
            double discountPercent = 0;

            // Base discount
            if (totalAmountinMYR < 200)
            {
                discountPercent = 0;
            }
            else if (totalAmountinMYR <= 500)
            {
                discountPercent = 0.05;
            }
            else if (totalAmountinMYR <= 800)
            {
                discountPercent = 0.07;
            }
            else if (totalAmountinMYR <= 1200)
            {
                discountPercent = 0.10;
            }
            else
            {
                discountPercent = 0.15;
            }

            // Condition discount
            if (totalAmountinMYR > 500 && IsPrime(totalAmountinMYR))
            {
                discountPercent += 0.08;
            }

            if (totalAmountinMYR > 500 && totalAmountinMYR % 10 ==5)
            {
                discountPercent += 0.10;
            }

            // Cap 20%
            if (discountPercent > 0.20)
            {
                discountPercent = 0.20;
            }

            // prime number calculation
            bool IsPrime(long n)
            {
                if (n <= 1) return false;

                for (long i = 2; i * i <= n; i++)
                {
                    if (n % i == 0)
                        return false;
                }

                return true;
            }

            long totalDiscount = (long)(calculatedTotal * discountPercent);
            long finalAmount = calculatedTotal - totalDiscount;

            return Ok(new
            {
                result = 1,
                totalAmount = calculatedTotal,
                totalDiscount,
                finalAmount
            }
            );

        }

        // Failure Response
        private IActionResult Fail(string msg)
        {
            return Ok(new
            {
                result = 0,
                resultmessage = msg
            });
        }
    }
}
