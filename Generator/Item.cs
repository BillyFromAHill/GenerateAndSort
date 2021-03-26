using System;
using System.Collections.Generic;
using System.Text;

namespace Generator
{
    public class Item
    {

        public Item(int number, string value)
        {
            Number = number;
            Value = value;
        }

        public int Number { get; }

        public string Value { get; }
    }
}
