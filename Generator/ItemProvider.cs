using System;
using System.Collections.Generic;
using System.Text;

namespace Generator
{
    public class ItemProvider
    {
        private Random random = new Random();

        public ItemProvider()
        {

        }

        public Item CreateNew()
        {
            return new Item(random.Next(), Guid.NewGuid().ToString());
        }
    }
}
