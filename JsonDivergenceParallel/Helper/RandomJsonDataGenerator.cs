using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace JsonDivergenceParallel.Helper
{
    public static class RandomJsonDataGenerator
    {
        private static readonly Random random = new Random();

        public static string GenerateRandomJson()
        {
            MockData mockData = new MockData
            {
                Id = random.Next(1, 1000),
                Name = GenerateRandomString(10),
                Description = GenerateRandomString(50),
                Date = DateTime.Now.ToString("o"),
                Details = GenerateRandomDetails()
            };

            return mockData.ToJson();
        }

        private static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];

            for (int i = 0; i < length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new string(stringChars);
        }

        private static List<Detail> GenerateRandomDetails()
        {
            var detailsList = new List<Detail>();
            int detailsCount = random.Next(1, 5);

            for (int i = 0; i < detailsCount; i++)
            {
                detailsList.Add(new Detail
                {
                    DetailId = random.Next(1, 100),
                    DetailName = GenerateRandomString(15),
                    Value = random.NextDouble() * 100
                });
            }

            return detailsList;
        }

        private class MockData
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Date { get; set; }
            public List<Detail> Details { get; set; }

            public string ToJson()
            {
                var sb = new StringBuilder();
                sb.Append("{");
                sb.AppendFormat("\"Id\": {0},", Id);
                sb.AppendFormat("\"Name\": \"{0}\",", Name);
                sb.AppendFormat("\"Description\": \"{0}\",", Description);
                sb.AppendFormat("\"Date\": \"{0}\",", Date);
                sb.Append("\"Details\": [");

                for (int i = 0; i < Details.Count; i++)
                {
                    sb.Append(Details[i].ToJson());
                    if (i < Details.Count - 1)
                    {
                        sb.Append(",");
                    }
                }

                sb.Append("]");
                sb.Append("}");
                return sb.ToString();
            }
        }

        private class Detail
        {
            public int DetailId { get; set; }
            public string DetailName { get; set; }
            public double Value { get; set; }

            public string ToJson()
            {
                return $"{{\"DetailId\": {DetailId}, \"DetailName\": \"{DetailName}\", \"Value\": {Value}}}";
            }
        }
    }
}
