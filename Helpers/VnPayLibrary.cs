using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace TechShop1.Helpers
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData
            = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
        {
            var data = new StringBuilder();
            foreach (var kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "="
                              + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            string queryString = data.ToString().TrimEnd('&');
            string signData = queryString;

            string secureHash = HmacSHA512(vnpHashSecret, signData);

            return baseUrl + "?" + queryString
                   + "&vnp_SecureHash=" + secureHash;
        }

        private static string HmacSHA512(string key, string inputData)
        {
            var hash = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            byte[] bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(inputData));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
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
