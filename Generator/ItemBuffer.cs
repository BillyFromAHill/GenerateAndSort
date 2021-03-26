using System;
using System.Collections.Generic;
using System.Text;

namespace Generator
{
    public class ItemBuffer
    {
        private readonly StringBuilder stringBuilder;

        public ItemBuffer()
        {
            stringBuilder = new StringBuilder();
        }

        public int AddNext(Item item)
        {
            stringBuilder.AppendLine($"{item.Number} - {item.Value} ");

            return stringBuilder.Length;
        }

        public string CreateString()
        {
            return stringBuilder.ToString();
        }
    }
}
