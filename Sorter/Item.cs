using System;
using System.Collections.Generic;
using System.Text;

namespace Sorter
{
    public class Item
    {
        // TODO: possibly replace by Parse.
        public Item(string serialized)
        {
            var values = serialized.Split("-");
            Number = int.Parse(values[0]);
            Value = values[1];
        }

        public Item(int number, string value)
        {
            Number = number;
            Value = value;
        }

        public int Number { get; }

        public string Value { get; }

        public override string ToString()
        {
            return $"{Number}-{Value}";
        }
    }
}
