using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using webbanxe.Payments;

namespace doanwebnangcao
{
    public class VnPay
    {
        public const string VERSION = "2.0.0"; // Thay đổi phiên bản thành 2.0.0
        private SortedList<string, string> requestData = new SortedList<string, string>(new VnPayCompare());
        private SortedList<string, string> responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            return responseData.ContainsKey(key) ? responseData[key] : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
        {
            // Chuỗi để tạo chữ ký (không URL-encode)
            StringBuilder signDataBuilder = new StringBuilder();
            // Chuỗi cho URL (có URL-encode)
            StringBuilder queryStringBuilder = new StringBuilder();

            foreach (KeyValuePair<string, string> kv in requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    // Chuỗi cho chữ ký: không mã hóa
                    signDataBuilder.Append(kv.Key + "=" + kv.Value + "&");
                    // Chuỗi cho URL: có mã hóa
                    queryStringBuilder.Append(HttpUtility.UrlEncode(kv.Key) + "=" + HttpUtility.UrlEncode(kv.Value) + "&");
                }
            }

            // Xóa ký tự "&" cuối cùng
            string signData = signDataBuilder.ToString();
            if (signData.Length > 0)
            {
                signData = signData.Remove(signData.Length - 1, 1);
            }

            // Tạo chữ ký từ chuỗi không mã hóa
            string vnp_SecureHash = HashAndGetIP.HmacSHA512(vnp_HashSecret, signData);

            // Tạo URL với tham số đã mã hóa
            string queryString = queryStringBuilder.ToString();
            if (queryString.Length > 0)
            {
                queryString = queryString.Remove(queryString.Length - 1, 1);
            }
            baseUrl += "?" + queryString + "&vnp_SecureHash=" + vnp_SecureHash;

            return baseUrl;
        }

        public bool ValidateSignature(string vnp_SecureHash, string vnp_HashSecret)
        {
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in responseData)
            {
                if (!string.IsNullOrEmpty(kv.Value) && kv.Key != "vnp_SecureHash")
                {
                    data.Append(HttpUtility.UrlEncode(kv.Key) + "=" + HttpUtility.UrlEncode(kv.Value) + "&");
                }
            }

            string signData = data.ToString();
            if (signData.Length > 0)
            {
                signData = signData.Remove(data.Length - 1, 1);
            }
            string checkSum = HashAndGetIP.HmacSHA512(vnp_HashSecret, signData);
            return checkSum.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return string.CompareOrdinal(x, y);
        }
    }
}