using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace webbanxe.Payments
{
    public static class HashAndGetIP
    {
        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }

        public static string GetIpAddress(HttpContextBase context)
        {
            string ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ipAddress) || ipAddress.ToLower() == "unknown" || ipAddress.Length > 45)
            {
                ipAddress = context.Request.ServerVariables["REMOTE_ADDR"];
            }

            // Nếu là IPv6, thử chuyển sang IPv4 nếu có
            if (!string.IsNullOrEmpty(ipAddress) && ipAddress.Contains(":"))
            {
                string[] ipParts = ipAddress.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in ipParts)
                {
                    string trimmedIp = part.Trim();
                    if (!trimmedIp.Contains(":") && IsValidIPv4(trimmedIp))
                    {
                        return trimmedIp;
                    }
                }
            }

            return ipAddress ?? "127.0.0.1";
        }

        private static bool IsValidIPv4(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString))
                return false;

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
                return false;

            return splitValues.All(r => byte.TryParse(r, out byte temp) && temp >= 0 && temp <= 255);
        }
    }
}